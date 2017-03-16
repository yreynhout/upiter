namespace Upiter
    open System
    open System.Runtime.Caching

    open Serilog
    open Serilog.Configuration
    
    open NodaTime

    open Upiter.Messages
    open Upiter.Messages.GroupContracts
    open Upiter.Scheduler
    open Upiter.Projections
    open Upiter.Seeding
    
    open Newtonsoft.Json
    open SqlStreamStore
    
    module Program =
        [<EntryPoint>]
        let main argv =
            let template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {SourceContext} {Message}{NewLine}{Exception}"
            // Initialize Logging
            let logConfiguration = 
                LoggerConfiguration()
                    .Destructure.FSharpTypes()
                    .WriteTo
                    .Async((fun config -> 
                        config
                            .ColoredConsole(outputTemplate=template) 
                        |> ignore), 100)
                    .WriteTo
                    .Async((fun config -> 
                        config
                            .File("output.log", outputTemplate=template, fileSizeLimitBytes = new Nullable<int64>()) 
                        |> ignore), 100)
            Log.Logger <- logConfiguration
                            .MinimumLevel.Debug()
                            .CreateLogger()

            let contracts = 
                [
                    ("PrivateGroupWasStarted", typeof<PrivateGroupWasStarted>)
                    ("PublicGroupWasStarted", typeof<PublicGroupWasStarted>)
                    ("GroupWasRenamed", typeof<GroupWasRenamed>)
                    ("GroupInformationWasChanged", typeof<GroupInformationWasChanged>)
                    ("GroupWasDeleted", typeof<GroupWasDeleted>)
                    ("GroupMembershipInvitationWasPolicySet", typeof<GroupMembershipInvitationPolicyWasSet>)
                ] |> Map.ofList

            let resolver contract = Map.tryFind contract contracts 

            let rec readlines () = seq {
                let line = Console.ReadLine()
                if (line <> "q") then
                    yield line
                    yield! readlines ()
            }

            using (new InMemoryStreamStore()) (
                fun store -> 
                    Seeding.seed store 
                    |> Async.Start

                    using (new MemoryCache("Groups")) (
                        fun cache ->
                            using (createScheduler SystemClock.Instance 50) (
                                fun scheduler ->
                                    scheduler.Error |> Event.add((fun exn -> Log.Debug(exn, exn.Message)))
                                    scheduler.Post(Scheduler.Commands.Start)

                                    let settings1 : ProjectionHost.HostSettings = 
                                        {
                                            Identity = "GroupProjection"
                                            //EventStore
                                            StreamStore = store
                                            BatchReadSize = 10
                                            ContractTypeResolver = resolver
                                            ContractSerializerSettings = JsonSerializerSettings()
                                            //Projection
                                            Cache = cache
                                            Projection = Projection.instance
                                            Scheduler = (fun action due -> scheduler.Post(Scheduler.Commands.ScheduleTellOnce (action, due)))
                                        }
                                    
                                    using (ProjectionHost.createHost settings1) (
                                        fun host1 ->
                                            host1.Error |> Event.add((fun exn -> Log.Debug(exn, exn.Message)))
                                            host1.Post(ProjectionHost.Commands.Start)

                                            let settings2 : SubscriptionHost.HostSettings =
                                                {
                                                    Identity = "Subscription"
                                                    Scheduler = (fun action due -> scheduler.Post(Scheduler.Commands.ScheduleTellOnce (action, due)))
                                                    WaitTimeBetweenSubscriptionAttempts = 5000L
                                                    //EventStore
                                                    StreamStore = store
                                                    ContractTypeResolver = resolver
                                                    ContractSerializerSettings = JsonSerializerSettings()
                                                    //Projection
                                                    Projector = 
                                                        (fun envelope -> async { 
                                                            do! host1.PostAndAsyncReply(fun reply ->
                                                                ProjectionHost.Commands.ProjectSubscriptionMessage (envelope, reply)
                                                            )
                                                        })
                                                }

                                            using (SubscriptionHost.createHost settings2) (
                                                fun host2 ->
                                                    host2.Error |> Event.add((fun exn -> Log.Debug(exn, exn.Message)))
                                                    host2.Post(SubscriptionHost.Commands.Start)
                                                    
                                                    readlines()
                                                    |> Seq.iter (fun line -> printfn "%A" (host1.PostAndReply ProjectionHost.CheckStatus))

                                                    scheduler.Post(Scheduler.Commands.Stop)
                                                    host1.Post(ProjectionHost.Commands.Stop)
                                                    host2.Post(SubscriptionHost.Commands.Stop)

                                                    Log.CloseAndFlush()
                                            )
                                    )
                            )
                    )
                )
                
            0 // return an integer exit code
