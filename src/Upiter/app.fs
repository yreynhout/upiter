namespace Upiter
    open System
    open System.Security.Claims
    open System.Text
    open FSharp.Control

    open Serilog
    open Serilog.Core
    open NodaTime
    open Newtonsoft.Json
    open SqlStreamStore

    open Upiter.Messages.GroupContracts
    open Upiter.Model.Group
    open Upiter.Security
    
    open Suave
    open Suave.Filters
    open Suave.Writers
    open Suave.Operators
    open Suave.RequestErrors
    open Suave.Successful

    open Upiter.AppSecurity

    module App =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "App")

        let private getOrCreateRequestId (request: HttpRequest) =
            match request.header("X-Request-ID") with
            | Choice1Of2 value -> 
                let (result, parsed) = Guid.TryParse(value)
                if result then parsed else Guid.NewGuid()
            | Choice2Of2 _ -> Guid.NewGuid()
        
        let private getPlatformVisitor (userState: Map<string, obj>) =
            match Map.tryFind AppSecurity.PlatformMemberKey userState with
            | Some principal -> PlatformVisitor(principal :?> ClaimsPrincipal)
            | None -> PlatformVisitor(ClaimsPrincipal()) //TODO: Map subdomain to tenant
        
        let private toGroupCommand path data (settings: JsonSerializerSettings) =
            let json = Encoding.UTF8.GetString(data)
            match path with
            | "/api/group/start_private" -> 
                Some (StartPrivateGroup (JsonConvert.DeserializeObject<StartPrivateGroup>(json, settings)))
            | "/api/group/start_public" -> 
                Some (StartPublicGroup (JsonConvert.DeserializeObject<StartPublicGroup>(json, settings)))
            | "/api/group/rename" -> 
                Some (RenameGroup (JsonConvert.DeserializeObject<RenameGroup>(json, settings)))
            | "/api/group/change_information" -> 
                Some (ChangeGroupInformation (JsonConvert.DeserializeObject<ChangeGroupInformation>(json, settings)))
            | "/api/group/set_membership_invitation_policy" -> 
                Some (SetGroupMembershipInvitationPolicy (JsonConvert.DeserializeObject<SetGroupMembershipInvitationPolicy>(json, settings)))
            | "/api/group/set_moderation_policy" -> 
                Some (SetGroupModerationPolicy (JsonConvert.DeserializeObject<SetGroupModerationPolicy>(json, settings)))
            | "/api/group/delete" -> 
                Some (DeleteGroup (JsonConvert.DeserializeObject<DeleteGroup>(json, settings)))
            | _ -> None

        let private enrichGroupCommandWithTenant tenant command =
            match command with
            | StartPrivateGroup cmd -> StartPrivateGroup { cmd with TenantId = tenant }
            | StartPublicGroup cmd -> StartPublicGroup { cmd with TenantId = tenant }
            | RenameGroup cmd -> RenameGroup { cmd with TenantId = tenant }
            | ChangeGroupInformation cmd -> ChangeGroupInformation { cmd with TenantId = tenant }
            | SetGroupMembershipInvitationPolicy cmd -> SetGroupMembershipInvitationPolicy { cmd with TenantId = tenant }
            | SetGroupModerationPolicy cmd -> SetGroupModerationPolicy { cmd with TenantId = tenant }
            | DeleteGroup cmd -> DeleteGroup { cmd with TenantId = tenant }

        let private handleGroupCommand (router: GroupRouter) (httpJsonSettings: JsonSerializerSettings) : WebPart =
            fun (context : HttpContext) -> async {
                log.Debug("Handle command on path {path}", context.request.url.PathAndQuery)
                match toGroupCommand context.request.url.PathAndQuery context.request.rawForm httpJsonSettings with
                | Some command ->
                    let visitor = getPlatformVisitor context.userState
                    log.Debug("Converted request to command on path {path}: {command}", context.request.url.PathAndQuery, command)
                    let enrichedCommand = enrichGroupCommandWithTenant visitor.Tenant command
                    let! result = 
                        router.PostAndAsyncReply(
                            fun channel -> 
                                let envelope =
                                    {
                                        Model.Envelope.CommandId = (getOrCreateRequestId context.request)
                                        Model.Envelope.Command = enrichedCommand
                                        Model.Envelope.Visitor = visitor
                                    }
                                (envelope, channel)
                        )
                    match result with
                    | Ok position ->
                        log.Debug("Command handling succeeded: {command}", command)
                        let (_, command) = command.ToContractMessage()
                        return!
                            (setMimeType "application/json"
                            >=> setHeader "ES-Position" (position.ToString()) 
                            >=> OK (JsonConvert.SerializeObject(command)))
                            context
                    | Error error ->
                        log.Debug("Command handling failed: {command} - {error}", command, error)
                        match error with
                        | NotAuthorized reason ->
                            return!
                                (setMimeType "application/problem+json"
                                >=> FORBIDDEN (JsonConvert.SerializeObject({ HttpProblemDetails.NotAuthorized with Details = reason })))
                                context
                | None -> 
                    log.Debug("Failed to convert request to command on path {path}", context.request.url.PathAndQuery)
                    return!
                        (setMimeType "application/problem+json"
                        >=> BAD_REQUEST (JsonConvert.SerializeObject(HttpProblemDetails.JsonParseFailure)))
                        context
            }

        let app (authenticationOptions: JwtBearerAuthenticationOptions) (httpJsonSettings: JsonSerializerSettings) (store: IStreamStore) (storeJsonSettings: JsonSerializerSettings) (clock: IClock) =
            let router = 
                spawnGroupRouter 
                    (Model.GroupStorage.reader store storeJsonSettings) 
                    (Model.GroupStorage.appender store storeJsonSettings) 
                    clock

            authorizeRequest authenticationOptions httpJsonSettings <|
                choose 
                    [
                        POST >=> choose [
                            pathStarts "/api/group" >=> handleGroupCommand router httpJsonSettings
                        ]
                    ]