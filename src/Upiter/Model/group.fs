namespace Upiter.Model
    open System
    open FSharp.Control
    
    open NodaTime

    open Serilog
    open Serilog.Core

    open Upiter.Messages.GroupContracts
    open Upiter.Model.Claims

    module Group =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "Group")

        type Events =
        | PrivateGroupWasStarted of PrivateGroupWasStarted
        | PublicGroupWasStarted of PublicGroupWasStarted
        | GroupWasRenamed of GroupWasRenamed
        | GroupInformationWasChanged of GroupInformationWasChanged
        | GroupMembershipInvitationPolicyWasSet of GroupMembershipInvitationPolicyWasSet
        | GroupModerationPolicyWasSet of GroupModerationPolicyWasSet
        | GroupWasDeleted of GroupWasDeleted
        with
            member this.ToContractMessage() =
                match this with
                | PrivateGroupWasStarted event                -> ("PrivateGroupWasStarted", event :> obj)
                | PublicGroupWasStarted event                 -> ("PublicGroupWasStarted", event :> obj)
                | GroupWasRenamed event                       -> ("GroupWasRenamed", event :> obj)
                | GroupInformationWasChanged event            -> ("GroupInformationWasChanged", event :> obj)
                | GroupMembershipInvitationPolicyWasSet event -> ("GroupMembershipInvitationPolicyWasSet", event :> obj)
                | GroupModerationPolicyWasSet event           -> ("GroupModerationPolicyWasSet", event :> obj)
                | GroupWasDeleted event                       -> ("GroupWasDeleted", event :> obj)

        type GroupIdentity =
            struct
                val TenantId: Int32
                val GroupId: Guid
                new(tenant: Int32, group: Guid) = { TenantId = tenant; GroupId = group }
            end

        //Is the benefit worth it? There's things we don't need handle here which is nice (event translation, raw eventstore message interaction).
        type ReadFromStreamResult = 
            (* expected version *)Int32 * (* events *)AsyncSeq<Events[]>
        type ReadFromStream = 
            (* stream *)GroupIdentity -> (* start *)Int32 -> Async<ReadFromStreamResult>
        type AppendToStreamResult = 
            (* next expected version *)Int32 * (* commit position *)Int64
        type AppendToStream = 
            (* stream *)GroupIdentity -> (* request *) Guid -> (* expected version *)Int32 -> (* events *)Events[] -> (* result *)Async<AppendToStreamResult>
        
        type private States =
        | Initial
        | Started
        | Deleted

        type private Group = 
            {
                CurrentState: States
            }
            with 
                member this.IsInState state = this.CurrentState = state
                static member Initial = { CurrentState = States.Initial }
                static member Fold (initial: Group) (events: Events[]) =
                    let folder (state: Group) (event: Events) =
                        match event with
                        | PrivateGroupWasStarted _ -> { state with CurrentState = States.Started }
                        | PublicGroupWasStarted _ -> { state with CurrentState = States.Started }
                        | GroupWasRenamed _ -> state
                        | GroupInformationWasChanged _ -> state
                        | GroupMembershipInvitationPolicyWasSet _ -> state
                        | GroupModerationPolicyWasSet _ -> state
                        | GroupWasDeleted _ -> { state with CurrentState = States.Deleted }
                    
                    Array.fold folder initial events

        type private Aggregate = 
            { 
                ExpectedVersion: Int32; 
                Data: Group;
            }
            with static member Initial = { ExpectedVersion = 0; Data = Group.Initial }

        type Commands =
        | StartPrivateGroup of StartPrivateGroup
        | StartPublicGroup of StartPublicGroup
        | RenameGroup of RenameGroup
        | ChangeGroupInformation of ChangeGroupInformation
        | SetGroupMembershipInvitationPolicy of SetGroupMembershipInvitationPolicy
        | SetGroupModerationPolicy of SetGroupModerationPolicy
        | DeleteGroup of DeleteGroup
        with
            member this.ToContractMessage() =
                match this with
                | StartPrivateGroup cmd                     -> ("StartPrivateGroup", cmd :> obj)
                | StartPublicGroup cmd                      -> ("StartPublicGroup", cmd :> obj)
                | RenameGroup cmd                           -> ("RenameGroup", cmd :> obj)
                | ChangeGroupInformation cmd                -> ("ChangeGroupInformation", cmd :> obj)
                | SetGroupMembershipInvitationPolicy cmd    -> ("SetGroupMembershipInvitationPolicy", cmd :> obj)
                | SetGroupModerationPolicy cmd              -> ("SetGroupModerationPolicy", cmd :> obj)
                | DeleteGroup cmd                           -> ("DeleteGroup", cmd :> obj)


        type Errors =
        | NotAuthorized of (* reason *)string

        type private GroupActor = MailboxProcessor<Envelope<Commands> * AsyncReplyChannel<Result<Int64, Errors>>>
        let private spawnGroupActor (identity: GroupIdentity) (reader: ReadFromStream) (appender: AppendToStream) (clock: IClock) : GroupActor =
            let load = async {
                log.Debug("Loading group {group} from tenant {tenant}", identity.GroupId, identity.TenantId)
                let! (expected, events) = reader identity 0
                let! state = events |> AsyncSeq.fold Group.Fold Group.Initial
                return { ExpectedVersion = expected; Data = state; }
            }

            let catchup version stale = async { 
                log.Debug("Catching up with group {group} from tenant {tenant}", identity.GroupId, identity.TenantId)
                //not used yet - we need to be able to recover from a concurrent save.
                //are we going to use exceptions for concurrency handling? the underlying libs do
                //so it'd be like putting lipstick on a pig
                let! (expected, events) = reader identity (version + 1)
                let! state = events |> AsyncSeq.fold Group.Fold stale
                return { ExpectedVersion = expected; Data = state; }
            }

            let save request expected events = async {
                log.Debug("Saving group {group} from tenant {tenant}", identity.GroupId, identity.TenantId)                
                return! appender identity request expected events 
            }

            MailboxProcessor.Start <| fun inbox ->
                let rec loop (group: Aggregate) = async {
                    let! (envelope, channel) = inbox.Receive()
                    log.Debug("GroupActor: {message}", envelope.Message)
                    let decision : Result<Events[], Errors> =
                        match envelope.Message with
                        | StartPrivateGroup cmd ->
                            if not(envelope.PlatformMember.IsPlatformAdministrator()) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator.")
                            elif group.Data.IsInState(Initial) then
                                Ok [| 
                                    PrivateGroupWasStarted
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            Name = cmd.Name
                                            Purpose = cmd.Purpose
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | StartPublicGroup cmd ->
                            if not(envelope.PlatformMember.IsPlatformAdministrator()) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator.")
                            elif group.Data.IsInState(Initial) then
                                Ok [| 
                                    PublicGroupWasStarted
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            Name = cmd.Name
                                            Purpose = cmd.Purpose
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | RenameGroup cmd -> 
                            if not(envelope.PlatformMember.IsPlatformAdministrator() || envelope.PlatformMember.IsGroupOwner(cmd.GroupId)) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator nor the role of group owner.")
                            elif group.Data.IsInState(Started) then
                                Ok [| 
                                    GroupWasRenamed
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            Name = cmd.Name
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | ChangeGroupInformation cmd ->
                            if not(envelope.PlatformMember.IsPlatformAdministrator() || envelope.PlatformMember.IsGroupOwner(cmd.GroupId)) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator nor the role of group owner.")
                            elif group.Data.IsInState(Started) then
                                Ok [| 
                                    GroupInformationWasChanged
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            Purpose = cmd.Purpose
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | SetGroupMembershipInvitationPolicy cmd ->
                            if not(envelope.PlatformMember.IsPlatformAdministrator() || envelope.PlatformMember.IsGroupOwner(cmd.GroupId)) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator nor the role of group owner.")
                            elif group.Data.IsInState(Started) then
                                Ok [| 
                                    GroupMembershipInvitationPolicyWasSet
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            AllowMembersToInvite = cmd.AllowMembersToInvite
                                            AllowModeratorsToInvite = cmd.AllowModeratorsToInvite
                                            AllowOwnersToInvite = cmd.AllowOwnersToInvite
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | SetGroupModerationPolicy cmd ->
                            if not(envelope.PlatformMember.IsPlatformAdministrator() || envelope.PlatformMember.IsGroupOwner(cmd.GroupId)) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator nor the role of group owner.")
                            elif group.Data.IsInState(Started) then
                                Ok [| 
                                    GroupModerationPolicyWasSet
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            AllowPlatformGuestsToComment = cmd.AllowPlatformGuestsToComment
                                            AllowPlatformMembersToComment = cmd.AllowPlatformMembersToComment
                                            AllowGroupMembersToComment = cmd.AllowGroupMembersToComment
                                            AllowPlatformMembersToPost = cmd.AllowPlatformMembersToPost
                                            AllowGroupMembersToPost = cmd.AllowGroupMembersToPost
                                            RequireModerationAsOfLinkCount = cmd.RequireModerationAsOfLinkCount
                                            RequireModerationAsOfImageCount = cmd.RequireModerationAsOfImageCount
                                            RequireModerationAsOfMediaCount = cmd.RequireModerationAsOfMediaCount
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                        | DeleteGroup cmd -> 
                            if not(envelope.PlatformMember.IsPlatformAdministrator() || envelope.PlatformMember.IsGroupOwner(cmd.GroupId)) then
                                Error (Errors.NotAuthorized "The platform member does not have the role of platform administrator nor the role of group owner.")
                            elif group.Data.IsInState(Started) then
                                Ok [| 
                                    GroupWasDeleted
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                Ok [||]
                    
                    match decision with
                    | Ok events ->
                        let! (next, position) = save envelope.RequestId group.ExpectedVersion events
                        channel.Reply(Ok(position))
                        return! loop { group with ExpectedVersion = next; Data = Group.Fold group.Data events }
                    | Error error ->
                        channel.Reply(Error(error))
                        return! loop group
                }

                async {
                    let! group = load
                    return! loop group
                }

        type GroupRouter = MailboxProcessor<Envelope<Commands> * AsyncReplyChannel<Result<Int64, Errors>>>
        let spawnGroupRouter (reader: ReadFromStream) (appender: AppendToStream) (clock: IClock) : GroupRouter =
            let identify message =
                match message with
                | StartPrivateGroup cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | StartPublicGroup cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | RenameGroup cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | ChangeGroupInformation cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | SetGroupMembershipInvitationPolicy cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | SetGroupModerationPolicy cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)
                | DeleteGroup cmd -> GroupIdentity(cmd.TenantId, cmd.GroupId)

            MailboxProcessor.Start <| fun inbox ->
                let rec loop (groups: Map<GroupIdentity, GroupActor>) = async {
                    let! (envelope, channel) = inbox.Receive()
                    log.Debug("GroupRouter: {message}", envelope.Message)
                    let identity = identify envelope.Message
                    match Map.tryFind identity groups with
                    | Some group ->
                        log.Debug("GroupRouter: route to cached group {identity}", identity)
                        group.Post((envelope, channel))
                        return! loop groups
                    | None ->
                        log.Debug("GroupRouter: route to uncached group {identity}", identity)
                        let group = spawnGroupActor identity reader appender clock
                        group.Post((envelope, channel))
                        return! loop (Map.add identity group groups)
                }
                loop Map.empty