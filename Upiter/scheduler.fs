namespace Upiter
    open System
    open System.Threading

    open NodaTime

    open Serilog

    module Scheduler =
        let private log = Log.ForContext("SourceContext", "Scheduler")

        type PrivateCommands =
            private
            | TimerElapsed of Instant
            
        type Commands =
        | Start
        | Stop
        | ScheduleTellOnce of Action * int64
        | PrivateCommand of PrivateCommands

        [<CustomEquality;CustomComparison>]
        type private ScheduledAction = { Id: Guid; Tell: Action; Due: Instant } with
            override this.Equals(comparand) = 
                match comparand with
                | :? ScheduledAction as other -> this.Id.Equals(other.Id) && this.Due.Equals(other.Due)
                | _ -> false
            override this.GetHashCode() =
                this.Id.GetHashCode() ^^^ this.Due.GetHashCode()

            interface System.IComparable with
                member this.CompareTo comparand =
                    let other = comparand :?> ScheduledAction
                    let comparison = this.Due.CompareTo(other.Due)
                    if comparison = 0 then
                        this.Id.CompareTo(other.Id)
                    else
                        comparison

        type private States = Initial = 0 | Started = 1 | Stopped = 2
        type private SchedulerState = 
            { 
                State: States;
                Timer: Timer; 
                ScheduledActions: Set<ScheduledAction>;
            }
            with static member Initial = { State = States.Initial; Timer = null; ScheduledActions = Set.empty }

        let createScheduler (clock: IClock) (frequency: int32) =
            MailboxProcessor<Commands>.Start
            <| fun mailbox ->
                    let rec loop (scheduler: SchedulerState) =
                        async {
                            let! message = mailbox.Receive()
                            match message with
                            | Start ->
                                log.Debug("Starting scheduler")
                                if scheduler.State = States.Initial || scheduler.State = States.Stopped then
                                    let timer = 
                                        new Timer(
                                            (fun _ -> mailbox.Post (PrivateCommand(TimerElapsed clock.Now))), 
                                            null, 
                                            frequency, 
                                            frequency)
                                    return! loop { scheduler with State = States.Started; Timer = timer; }
                                else
                                    return! loop scheduler
                            | Stop ->
                                log.Debug("Stopping scheduler.")
                                if scheduler.State = States.Started then
                                    scheduler.Timer.Change(Timeout.Infinite, Timeout.Infinite) |> ignore
                                    scheduler.Timer.Dispose()
                                    return! loop { scheduler with State = States.Stopped; Timer = null; ScheduledActions = Set.empty; }
                                else
                                    return! loop scheduler
                            | ScheduleTellOnce (action, due) ->
                                log.Debug("Schedule a tell once action")
                                if scheduler.State = States.Started then                                                                
                                    let instant = clock.Now.Plus(Duration.FromMilliseconds(due))
                                    return! loop { scheduler with ScheduledActions = Set.add { Id = Guid.NewGuid(); Tell = action; Due = instant; } scheduler.ScheduledActions }
                                else
                                    return! loop scheduler                                
                            | PrivateCommand msg ->
                                match msg with
                                | TimerElapsed instant ->
                                    //log.Debug("Timer elapsed at ticks {Instant}", instant.Ticks)
                                    if scheduler.State = States.Started then                                
                                        let due = Set.filter (fun item -> item.Due <= instant) scheduler.ScheduledActions
                                        due |> Set.iter (fun item -> item.Tell.Invoke())
                                        return! loop { scheduler with ScheduledActions = Set.difference scheduler.ScheduledActions due }
                                    else
                                        return! loop scheduler
                            
                        }
                    loop SchedulerState.Initial