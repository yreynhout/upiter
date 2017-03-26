namespace Yoga.Projections
    open System
    open System.Threading.Tasks
    open FSharp.Control
    open Microsoft.FSharp.Reflection

    open Serilog
    open Serilog.Core
    open NodaTime
    
    open System.Runtime.Caching
    open SqlStreamStore
    open SqlStreamStore.Streams
    open SqlStreamStore.Subscriptions
    open Newtonsoft.Json

    open Upiter.Messages
    open Yoga.Projections.ProjectionHost
    
    module SubscriptionHost =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "SubscriptionHost")

        type Projector = Envelope -> Async<unit>
        type Scheduler = Action -> int64 -> unit

        type States =
        | Initial
        | Subscribing
        | Subscribed
        | Stopped
        | Final

        type Status = 
            { 
                CurrentState: States; 
                CurrentPosition: int64; 
            }

        type PrivateCommands =
            private 
            | Subscribe
            | ProcessMessage of Envelope * TaskCompletionSource<unit>

        type Commands =
            | Start
            | Stop
            | Restart
            | Shutdown
            | CheckStatus of AsyncReplyChannel<Status>
            | PrivateCommand of PrivateCommands * int64 //vclock

        [<Literal>]
        let private FromStartOfAllStream = 0L

        type private HostState =
            {
                CurrentState: States
                Subscription: IAllStreamSubscription option
                CurrentPosition: int64
                VClock: int64
            }
            with 
                static member Initial = { CurrentState = States.Initial; VClock = 0L; Subscription = None; CurrentPosition = FromStartOfAllStream; }
                member this.IsInState state = this.CurrentState = state
                member this.IsInAnyOfStates states = 
                    Array.Exists(states, (fun state -> this.CurrentState = state))
                member this.MoveToState (state: States) = 
                    let nameOf value = 
                        match FSharpValue.GetUnionFields(value, typeof<States>) with
                        | case, _ -> case.Name 
                    log.Debug("Moving to state {state}.", nameOf(state))
                    { this with CurrentState = state; }
                member this.RecordPosition (position: int64) = 
                    log.Debug("Recording position {position}.", position)
                    { this with CurrentPosition = position; }
                member this.SwitchSubscription next =
                    log.Debug("Switching subscriptions.")
                    match this.Subscription with
                    | Some previous -> previous.Dispose()
                    | None -> ()
                    { this with Subscription = Some next; }
                member this.ClearSubscription() =
                    match this.Subscription with
                    | Some previous -> previous.Dispose()
                    | None -> ()
                    { this with Subscription = None; }
                member this.MatchesVClock clock = 
                    let result = this.VClock = clock
                    log.Debug("Matches vclock? {answer}.", (if result then "Yes" else "No"))
                    result
                member this.NextVClock() = this.VClock + 1L
                member this.MoveVClock() = 
                    log.Debug("Moving vclock to {vclock}.", (this.NextVClock()))
                    { this with VClock = this.NextVClock(); }

        type HostSettings =
            {
                Identity: string
                //Scheduler
                Scheduler: Scheduler
                WaitTimeBetweenSubscriptionAttempts: int64
                //EventStore
                StreamStore: IStreamStore
                ContractTypeResolver: string -> Type option
                ContractSerializerSettings: JsonSerializerSettings
                //Projection
                Projector: Projector
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
                            log.Debug("Starting subscription host")
                            if state.IsInAnyOfStates([| Initial; Stopped |]) then
                                inbox.Post (PrivateCommand (Subscribe, (state.NextVClock())))
                                return! loop (state.MoveVClock().MoveToState(Subscribing))
                            else
                                return! loop state
                        | Stop -> 
                            log.Debug("Stopping subscription host")
                            if state.IsInAnyOfStates([| Subscribing; Subscribed |]) then
                                return! loop (state.ClearSubscription().MoveVClock().MoveToState(Stopped))
                            else
                                return! loop state
                        | Restart -> 
                            log.Debug("Restarting subscription host")
                            if state.IsInAnyOfStates([| Subscribing; Subscribed; Stopped |]) then
                                inbox.Post (PrivateCommand (Subscribe, (state.NextVClock())))
                                return! loop (state.ClearSubscription().MoveVClock().RecordPosition(FromStartOfAllStream).MoveToState(Subscribing))
                            else
                                return! loop state
                        | Shutdown -> 
                            log.Debug("Shutting down subscription host")
                            if not(state.IsInState(Final)) then
                                return! loop (state.ClearSubscription().MoveVClock().MoveToState(Final))
                            else
                                return! loop state
                        | CheckStatus channel -> 
                            log.Debug("Checking status of subscription host")
                            let reply = 
                                { 
                                    Status.CurrentState = state.CurrentState;
                                    CurrentPosition = state.CurrentPosition;
                                }
                            channel.Reply(reply)
                            return! loop state
                        | PrivateCommand (command, vclock) -> 
                            // This check makes sure, we don't execute any work we've moved on from.
                            if state.MatchesVClock(vclock) then
                                match command with
                                | Subscribe -> 
                                    log.Debug("Subscribing to the all stream of {position}.", state.CurrentPosition)
                                    if state.IsInState(States.Subscribing) then
                                        let toNullablePosition position =
                                            if position = FromStartOfAllStream then
                                                new Nullable<int64>()
                                            else
                                                new Nullable<int64>(position)

                                        let onReceived (subscription : IAllStreamSubscription) (message : StreamMessage) : Task = 
                                            log.Debug("Received message of type {type} at {position}.", message.Type, message.Position)
                                            
                                            match settings.ContractTypeResolver(message.Type) with
                                            | Some contract -> 
                                                log.Debug("Received message of type {type} is a known contract.", message.Type)         
                                                let source = new TaskCompletionSource<unit>()
                                                async { 
                                                    let! data = message.GetJsonData() |> Async.AwaitTask
                                                    let deserialized = JsonConvert.DeserializeObject(data, contract, settings.ContractSerializerSettings)
                                                    let envelope = { AllStreamPosition = message.Position; Message = deserialized; }
                                                    inbox.Post(PrivateCommand(ProcessMessage(envelope, source), state.VClock))
                                                } |> Async.Start
                                                source.Task :> Task
                                            | None ->
                                                log.Debug("Received message of type {type} is not a known contract.", message.Type)                                                
                                                Task.CompletedTask
                                            
                                        let onDropped (subscription : IAllStreamSubscription) (reason: SubscriptionDroppedReason) (exn: Exception) = 
                                            schedule (PrivateCommand(Subscribe, state.VClock)) settings.WaitTimeBetweenSubscriptionAttempts
                                            log.Error(exn, "The subscription was dropped because {reason}.", (reason.ToString()))

                                        let subscription = 
                                            settings.StreamStore.SubscribeToAll(
                                                    (toNullablePosition state.CurrentPosition), 
                                                    new AllStreamMessageReceived(onReceived), 
                                                    new AllSubscriptionDropped(onDropped),
                                                    null,
                                                    true,
                                                    "SubscriptionHost")
                                        return! loop (state.SwitchSubscription(subscription).MoveToState(Subscribed))
                                    else
                                        return! loop state
                                | ProcessMessage (envelope, source) -> 
                                    log.Debug("Processing message of type {type} at {position}", (envelope.Message.GetType()), envelope.AllStreamPosition)
                                    if state.IsInState(Subscribed) then
                                        do! settings.Projector envelope
                                        source.TrySetResult() |> ignore
                                        return! loop state
                                    else
                                        return! loop state
                            else
                                match command with
                                | Subscribe -> 
                                    log.Debug("Not catching up with all stream as of {position} due to clock mismatch.", state.CurrentPosition)
                                | ProcessMessage (envelope, source) -> 
                                    log.Debug("Not processing catch up message of type {type} at {position} due to clock mismatch.", (envelope.Message.GetType()), envelope.AllStreamPosition)
                                    source.TrySetResult() |> ignore
                                return! loop state
                    }

                    loop HostState.Initial