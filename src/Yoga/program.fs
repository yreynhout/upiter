namespace Yoga
    open System
    open System.Text
    open System.Runtime.Caching

    open Argu
    open IniParser

    open Serilog
    open Serilog.Configuration
    
    open NodaTime

    open Upiter.Messages
    open Upiter.Messages.GroupContracts
    open Yoga.Scheduler
    open Yoga.Projections
    
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization
    open SqlStreamStore

    open Suave
    open Suave.Http
    open Suave.Operators
    open Suave.Writers
    open Suave.Web

    open Yoga.App
    open Upiter.AppSecurity
    
    module Program =
        type private ProgramArguments = 
        | [<Mandatory>][<Unique>] Port of int
        | [<Mandatory>][<Unique>] ConnectionString of string
        | [<Mandatory>][<Unique>] AuthenticationIniFile of string
        with
            interface IArgParserTemplate with
                member s.Usage =
                    match s with
                    | Port _ -> "specify a tcp port to listen on for inbound http traffic."
                    | ConnectionString _ -> "specify a Microsoft Sql Server connection string to store events in."
                    | AuthenticationIniFile _ -> "specify the ini file containing the audience, issuer and issuer secret key for OpenID Connect authentication."

        let private serverConfig (port: int) =
            { defaultConfig with
                homeFolder = Some __SOURCE_DIRECTORY__
                bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" port ]
            }
        [<EntryPoint>]
        let main args =
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

            //Config
            let exiter = ProcessExiter()
            let parser = ArgumentParser.Create<ProgramArguments>(errorHandler=exiter)
            let parsed = parser.Parse(args, ConfigurationReader.FromAppSettings())
            let port = parsed.GetResult (<@ Port @>, 8081)
            let connectionString = parsed.GetResult (<@ ConnectionString @>)
            let authenticationIniFile = parsed.GetResult (<@ AuthenticationIniFile @>, "authentication.ini")
            
            let createStore : IStreamStore = 
                let storeSettings = MsSqlStreamStoreSettings(connectionString)
                let store = new MsSqlStreamStore(storeSettings)
                //yuck
                store.CreateSchema(Async.DefaultCancellationToken)
                |> Async.AwaitTask
                |> Async.RunSynchronously
                store :> IStreamStore
            
            let authenticationOptions : JwtBearerAuthenticationOptions =
                let parser = FileIniDataParser()
                let data = parser.ReadFile(authenticationIniFile, Encoding.UTF8)
                let section = data.Item("Authentication")
                {
                    Audience = section.Item("Audience")
                    Issuer = section.Item("Issuer")
                    IssuerSecret = section.Item("IssuerSecret")
                    RequiredClaims = [| Upiter.Security.Claims.Tenant |]
                }

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

            using (createStore) (
                fun store ->

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
                                                    
                                                    let httpJsonSettings = JsonSerializerSettings()
                                                    httpJsonSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
                                                    httpJsonSettings.NullValueHandling <- NullValueHandling.Ignore
                                                    
                                                    startWebServer (serverConfig port) (app authenticationOptions httpJsonSettings cache)

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
