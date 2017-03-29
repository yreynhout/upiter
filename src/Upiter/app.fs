namespace Upiter
    open System
    open System.Text
    open FSharp.Control

    open Serilog
    open Serilog.Core
    open NodaTime
    open Newtonsoft.Json
    open SqlStreamStore

    open Upiter.Messages.GroupContracts
    open Upiter.Model.Group
    
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
                let mutable parsed = Guid.Empty;  
                if Guid.TryParse(value, &parsed) then 
                    parsed
                else
                    Guid.NewGuid()
            | Choice2Of2 _ -> Guid.NewGuid()
        
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

        let private handleGroupCommand (router: GroupRouter) (httpJsonSettings: JsonSerializerSettings) : WebPart =
            fun (context : HttpContext) -> async {
                log.Debug("Handle command on path {path}", context.request.url.PathAndQuery)
                let command = toGroupCommand context.request.url.PathAndQuery context.request.rawForm httpJsonSettings
                match command with
                | Some message ->
                    log.Debug("Converted request to command on path {path}: {command}", context.request.url.PathAndQuery, message)
                    let! result = 
                        router.PostAndAsyncReply(
                            fun reply -> 
                                let envelope =
                                    { 
                                        Model.Envelope.RequestId = (getOrCreateRequestId context.request)
                                        Model.Envelope.Message = message
                                    }
                                (envelope, reply)
                        )
                    return!
                        (setMimeType "application/json"
                        >=> setHeader "X-Position" (result.ToString()) 
                        >=> OK (JsonConvert.SerializeObject(message)))
                        context
                | None -> 
                    log.Debug("Failed to convert request to command on path {path}", context.request.url.PathAndQuery)
                    return!
                        (setMimeType "application/problem+json"
                        >=> BAD_REQUEST (
                            JsonConvert.SerializeObject(
                                dict [
                                    "type", "urn:upiter:v1:group_command_not_understood"
                                    "title", "GROUP_COMMAND_NOT_UNDERSTOOD"
                                    "details", "The group command you've sent could not be understood."
                                    "status", "400"
                                ]
                            )
                        ))
                        context
            }

        let app (authenticationOptions: JwtBearerAuthenticationOptions) (httpJsonSettings: JsonSerializerSettings) (store: IStreamStore) (storeJsonSettings: JsonSerializerSettings) (clock: IClock) =
            let router = 
                spawnGroupRouter 
                    (Model.GroupStorage.reader store storeJsonSettings) 
                    (Model.GroupStorage.appender store storeJsonSettings) 
                    clock

            authorize authenticationOptions httpJsonSettings <|
                choose 
                    [
                        POST >=> choose [
                            pathStarts "/api/group" >=> handleGroupCommand router httpJsonSettings
                        ]
                    ]