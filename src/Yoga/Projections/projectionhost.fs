namespace Yoga.Projections
    open System
    open FSharp.Control
    open Microsoft.FSharp.Reflection

    open Serilog
    open Serilog.Core
    open NodaTime
    
    open System.Runtime.Caching
    open SqlStreamStore
    open Newtonsoft.Json

    open Upiter.Messages
    
    module ProjectionHost =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "ProjectionHost")
        type Projection = MemoryCache -> Envelope -> unit
        type Scheduler = Action -> int64 -> unit

        type States =
        | Initial
        | Started
        | Stopped
        | Suspended
        | CatchingUp
        | Final

        type Status = 
            { 
                CurrentState: States; 
                ProjectionPosition: int64; 
                SubscriptionPosition: int64; 
            }

        type PrivateCommands =
            private 
            | CatchUpWithAllStream of int64
            | ProjectCatchUpMessage of Envelope

        type Commands =
            | Start
            | Stop
            | Suspend
            | Resume
            | Restart
            | ProjectSubscriptionMessage of Envelope * AsyncReplyChannel<unit>
            | Shutdown
            | CheckStatus of AsyncReplyChannel<Status>
            | PrivateCommand of PrivateCommands * int64 //private command * logical version clock
        
        [<Literal>]
        let private FromStartOfAllStream = 0L

        type private HostState =
            {
                CurrentState : States
                ProjectionPosition: int64
                SubscriptionPosition: int64
                VClock: int64 //Logical version clock
            }
            with 
                static member Initial = 
                    { 
                        CurrentState = Initial;
                        ProjectionPosition = FromStartOfAllStream;
                        SubscriptionPosition = FromStartOfAllStream;
                        VClock = 0L;
                    }
                member this.IsInState state = this.CurrentState = state
                member this.IsInAnyOfStates states = 
                    Array.Exists(states, (fun state -> this.CurrentState = state))
                member this.MatchesVClock clock = 
                    let result = this.VClock = clock
                    log.Debug("Matches vclock? {answer}.", (if result then "Yes" else "No"))
                    result
                    
                member this.NextVClock() = this.VClock + 1L
                member this.MoveVClock() = 
                    log.Debug("Moving vclock to {vclock}.", (this.NextVClock()))
                    { this with VClock = this.NextVClock(); }

                member this.MoveToState (state: States) = 
                    let nameOf value = 
                        match FSharpValue.GetUnionFields(value, typeof<States>) with
                        | case, _ -> case.Name 
                    log.Debug("Moving to state {state}.", nameOf(state))
                    { this with CurrentState = state; }
                member this.RecordProjectionPosition (position: int64) = 
                    log.Debug("Recording projection position {position}.", position)
                    { this with ProjectionPosition = position; }
                member this.RecordSubscriptionPosition (position: int64) = 
                    log.Debug("Recording subscription position {position}.", position)
                    { this with SubscriptionPosition = position; }
                member this.RecordPosition (position: int64) = 
                    log.Debug("Recording position {position}.", position)
                    { this with ProjectionPosition = position; SubscriptionPosition = position; }

        type HostSettings =
            {
                Identity: string
                //Scheduler
                Scheduler: Scheduler
                //EventStore
                StreamStore: IStreamStore
                BatchReadSize: int32
                ContractTypeResolver: string -> Type option
                ContractSerializerSettings: JsonSerializerSettings
                //Projection
                Cache: MemoryCache
                Projection: Projection
            }
        let createHost (settings: HostSettings) =                                             
            MailboxProcessor.Start 
                <| fun inbox ->
                    let schedule command due =
                        let action = new Action(fun () -> inbox.Post(command))
                        settings.Scheduler action due
                        
                    let rec loop (state: HostState) = async {
                        let! message = inbox.Receive()
                        match message with
                        | Start -> 
                            log.Debug("Starting projection host")
                            if state.IsInAnyOfStates([| Initial; Stopped |]) then
                                inbox.Post (PrivateCommand (CatchUpWithAllStream state.ProjectionPosition, (state.NextVClock())))
                                return! loop (state.MoveVClock().MoveToState(CatchingUp))
                            else
                                return! loop state
                        | Stop -> 
                            log.Debug("Stopping projection host")
                            if state.IsInAnyOfStates([| Started; CatchingUp; Suspended |]) then
                                return! loop (state.MoveVClock().MoveToState(Stopped))
                            else
                                return! loop state
                        | Suspend -> 
                            log.Debug("Suspending projection host")
                            if state.IsInAnyOfStates([| Started; CatchingUp; |]) then
                                return! loop (state.MoveVClock().MoveToState(Suspended))
                            else
                                return! loop state
                        | Resume ->
                            log.Debug("Resuming projection host")
                            if state.IsInState(Suspended) then
                                inbox.Post (PrivateCommand (CatchUpWithAllStream (state.ProjectionPosition + 1L), (state.NextVClock())))
                                return! loop (state.MoveVClock().MoveToState(CatchingUp))
                            else
                                return! loop state
                        | Restart -> 
                            log.Debug("Restarting projection host")
                            if state.IsInAnyOfStates([| Started; CatchingUp; Suspended; Stopped |]) then
                                //Clear cache
                                settings.Cache
                                |> Seq.iter (fun pair -> settings.Cache.Remove(pair.Key) |> ignore)
                                inbox.Post (PrivateCommand (CatchUpWithAllStream FromStartOfAllStream, (state.NextVClock())))
                                return! loop (state.MoveVClock().RecordProjectionPosition(FromStartOfAllStream).MoveToState(CatchingUp))
                            else
                                return! loop state
                        | ProjectSubscriptionMessage (envelope, channel) -> 
                            log.Debug("Project subscription message")
                            if state.IsInState(Started) then
                                if state.ProjectionPosition < envelope.AllStreamPosition then    
                                    //Project and record both projection and subscription position
                                    settings.Projection settings.Cache envelope
                                    channel.Reply()
                                    return! loop (state.RecordPosition(envelope.AllStreamPosition))
                                else
                                    //Record subscription position only
                                    channel.Reply()                                
                                    return! loop (state.RecordSubscriptionPosition(envelope.AllStreamPosition))
                            elif state.IsInAnyOfStates([| CatchingUp; Suspended; Stopped |]) then
                                //Record subscription position only
                                channel.Reply()                                
                                return! loop (state.RecordSubscriptionPosition(envelope.AllStreamPosition))
                            else
                                channel.Reply()                                
                                return! loop state
                        | Shutdown -> 
                            log.Debug("Shutting down projection host")
                            if not(state.IsInState(Final)) then
                                return! loop (state.MoveVClock().MoveToState(Final))
                            else
                                return! loop state
                        | CheckStatus channel -> 
                            log.Debug("Checking status of projection host")
                            let reply = 
                                { 
                                    Status.CurrentState = state.CurrentState;
                                    ProjectionPosition = state.ProjectionPosition;
                                    SubscriptionPosition = state.SubscriptionPosition;
                                }
                            channel.Reply(reply)
                            return! loop state
                        | PrivateCommand (command, vclock) -> 
                            // This check makes sure, we don't execute any work we've moved on from.
                            if state.MatchesVClock(vclock) then
                                match command with
                                | CatchUpWithAllStream next -> 
                                    log.Debug("Catching up with all stream as of {position}", next)
                                    if state.IsInState(States.CatchingUp) then
                                        let! page = 
                                            settings.StreamStore.ReadAllForwards(next, settings.BatchReadSize, true)
                                            |> Async.AwaitTask
                                        
                                        if page.Messages.Length = 0 && page.IsEnd then
                                            log.Debug("No messages and at the end of all stream. Probing again soon ...")
                                            schedule message 100L
                                            return! loop state //(state.MoveToState(Started))
                                        else
                                            //Post each message to project in the mailbox
                                            for msg in page.Messages do
                                                match settings.ContractTypeResolver(msg.Type) with
                                                | Some contract ->
                                                    let! data = msg.GetJsonData() |> Async.AwaitTask
                                                    let deserialized = JsonConvert.DeserializeObject(data, contract, settings.ContractSerializerSettings)
                                                    let envelope = { AllStreamPosition = msg.Position; Message = deserialized; }
                                                    inbox.Post(PrivateCommand(ProjectCatchUpMessage envelope, state.VClock))
                                                | None -> ()

                                            //Once the current batch is posted, tell our future self to continue as of the next position
                                            inbox.Post(PrivateCommand (CatchUpWithAllStream page.NextPosition, state.VClock))
                                            return! loop state
                                    else
                                        return! loop state
                                | ProjectCatchUpMessage envelope -> 
                                    log.Debug("Project catch up message of type {type} at {position}", (envelope.Message.GetType()), envelope.AllStreamPosition)
                                    if state.IsInState(CatchingUp) then
                                        if envelope.AllStreamPosition <= state.SubscriptionPosition then
                                            //Project and record projection position
                                            settings.Projection settings.Cache envelope
                                            return! loop (state.RecordProjectionPosition(envelope.AllStreamPosition))
                                        else
                                            //As soon as the subscription position is lower than the projection position
                                            //we can switch to push based
                                            return! loop (state.MoveVClock().RecordProjectionPosition(envelope.AllStreamPosition).MoveToState(Started))
                                    else
                                        return! loop state
                            else
                                match command with
                                | CatchUpWithAllStream next -> 
                                    log.Debug("Not catching up with all stream as of {position} due to clock mismatch.", next)
                                | ProjectCatchUpMessage envelope -> 
                                    log.Debug("Not processing catch up message of type {type} at {position} due to clock mismatch.", (envelope.Message.GetType()), envelope.AllStreamPosition)
                                return! loop state
                    }

                    loop HostState.Initial