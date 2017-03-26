namespace Upiter
    open System
    open System.Runtime.Caching

    open Serilog
    open Serilog.Configuration
    
    open NodaTime

    open Upiter.Messages
    open Upiter.Messages.GroupContracts
    open Upiter.Scheduler
    
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

            0 // return an integer exit code
