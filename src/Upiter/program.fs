namespace Upiter
    open System
    open System.Text

    open Argu
    open IniParser

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
                store.CreateSchema(true, Async.DefaultCancellationToken)
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
                }

            //Server
            using (createStore) (fun store -> 
                let httpJsonSettings = JsonSerializerSettings()
                httpJsonSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()
                httpJsonSettings.NullValueHandling <- NullValueHandling.Ignore
                let storeJsonSettings = JsonSerializerSettings()    
                storeJsonSettings.NullValueHandling <- NullValueHandling.Ignore
                startWebServer (serverConfig port) (app authenticationOptions httpJsonSettings store storeJsonSettings SystemClock.Instance)
            )
            0 // return an integer exit code
