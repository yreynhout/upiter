namespace Upiter
    open System

    open Argu

    open Serilog
    open Serilog.Configuration
    
    open NodaTime
    
    open Newtonsoft.Json
    open Newtonsoft.Json.Serialization

    open SqlStreamStore

    open Suave
    open Suave.Http
    open Suave.Operators
    open Suave.Writers
    open Suave.Web

    open Upiter.App
    
    module Program =
        type private ProgramArguments = 
        | [<Mandatory>][<Unique>] Port of int
        with
            interface IArgParserTemplate with
                member s.Usage =
                    match s with
                    | Port _ -> "specify a tcp port to listen on for inbound http traffic."

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

            let httpJsonSettings = JsonSerializerSettings()
            httpJsonSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
            let storeJsonSettings = JsonSerializerSettings()

            let storeSettings = MsSqlStreamStoreSettings("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=upiter;Integrated Security=True;")
            

            let parser = ArgumentParser.Create<ProgramArguments>()
            let parsed = parser.Parse(args, ConfigurationReader.FromAppSettings())
            //Config
            let port = parsed.GetResult (<@ Port @>, 8083)
            //Server
            using (new MsSqlStreamStore(storeSettings)) (fun store -> 
                //yuck
                store.CreateSchema(true, Async.DefaultCancellationToken)
                |> Async.AwaitTask
                |> Async.RunSynchronously

                startWebServer (serverConfig port) (app httpJsonSettings store storeJsonSettings SystemClock.Instance)
            )
            0 // return an integer exit code
