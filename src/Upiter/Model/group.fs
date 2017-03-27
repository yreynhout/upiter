namespace Upiter.Model
    open System
    open FSharp.Control
    
    open NodaTime

    open Upiter.Messages.GroupContracts

    module Group =
        type Events =
        | PrivateGroupWasStarted of PrivateGroupWasStarted
        | PublicGroupWasStarted of PublicGroupWasStarted
        | GroupWasRenamed of GroupWasRenamed
        | GroupInformationWasChanged of GroupInformationWasChanged
        | GroupMembershipInvitationPolicyWasSet of GroupMembershipInvitationPolicyWasSet
        | GroupModerationPolicyWasSet of GroupModerationPolicyWasSet
        | GroupWasDeleted of GroupWasDeleted

        type GroupIdentity =
            struct
                val TenantId: Int32
                val GroupId: Guid
                new(tenant: Int32, group: Guid) = { TenantId = tenant; GroupId = group }
            end

        type ReadResult = (* expected version *)Int32 * (* events *)AsyncSeq<Events[]>
        type ReadFromStream = (* stream *)GroupIdentity -> (* start *)Int32 -> Async<ReadResult>
        type AppendResult = (* next expected version *)Int32 * (* commit position *)Int64
        type AppendToStream = (* stream *)GroupIdentity -> (* request *) Guid -> (* expected version *)Int32 -> (* events *)Events[] -> (* result *)Async<AppendResult>
        
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

        [<Literal>]
        let private NoStream = -1

        type private Aggregate = 
            { 
                ExpectedVersion: Int32; 
                Data: Group;
            }
            with static member Initial = { ExpectedVersion = NoStream; Data = Group.Initial }

        type Commands =
        | StartPrivateGroup of StartPrivateGroup
        | StartPublicGroup of StartPublicGroup
        | RenameGroup of RenameGroup
        | ChangeGroupInformation of ChangeGroupInformation
        | SetGroupMembershipInvitationPolicy of SetGroupMembershipInvitationPolicy
        | SetGroupModerationPolicy of SetGroupModerationPolicy
        | DeleteGroup of DeleteGroup

        [<Literal>]
        let private FromStartOfStream = -1

        type private GroupActor = MailboxProcessor<Envelope<Commands> * AsyncReplyChannel<Int64>>
        let private spawnGroupActor (identity: GroupIdentity) (reader: ReadFromStream) (appender: AppendToStream) (clock: IClock) : GroupActor =
            //let stream = sprintf "%d~%s" identity.TenantId (identity.GroupId.ToString("N"))
            let load = async {
                let! (expected, events) = reader identity FromStartOfStream
                let! state = events |> AsyncSeq.fold Group.Fold Group.Initial
                return { ExpectedVersion = expected; Data = state; }
            }

            let catchup version stale = async {
                let! (expected, events) = reader identity (version + 1)
                let! state = events |> AsyncSeq.fold Group.Fold stale
                return { ExpectedVersion = expected; Data = state; }
            }

            let save request expected events = async {
                return! appender identity request expected events 
            }

            MailboxProcessor.Start <| fun inbox ->
                let rec loop (group: Aggregate) = async {
                    let! (envelope: Envelope<Commands>, channel: AsyncReplyChannel<Int64>) = inbox.Receive()
                    let events : Events[] =
                        match envelope.Message with
                        | StartPrivateGroup cmd -> 
                            if group.Data.IsInState(Initial) then
                                [| 
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
                                [||]
                        | StartPublicGroup cmd ->
                            if group.Data.IsInState(Initial) then
                                [| 
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
                                [||]
                        | RenameGroup cmd -> 
                            if group.Data.IsInState(Started) then
                                [| 
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
                                [||]
                        | ChangeGroupInformation cmd ->
                            if group.Data.IsInState(Started) then
                                [| 
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
                                [||]
                        | SetGroupMembershipInvitationPolicy cmd ->
                           if group.Data.IsInState(Started) then
                                [| 
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
                                [||]
                        | SetGroupModerationPolicy cmd ->
                           if group.Data.IsInState(Started) then
                                [| 
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
                                [||]
                        | DeleteGroup cmd -> 
                            if group.Data.IsInState(Started) then
                                [| 
                                    GroupWasDeleted
                                        { 
                                            TenantId = cmd.TenantId
                                            GroupId = cmd.GroupId
                                            PlatformMemberId = cmd.PlatformMemberId
                                            When = clock.Now.Ticks
                                        }
                                |]
                            else
                                [||]

                    let! (next, position) = save envelope.RequestId group.ExpectedVersion events
                    channel.Reply(position)
                    return! loop { group with ExpectedVersion = next; Data = Group.Fold group.Data events }
                }

                async {
                    let! group = load
                    return! loop group
                }

        type GroupRouter = MailboxProcessor<Envelope<Commands> * AsyncReplyChannel<Int64>>
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
                    let identity = identify envelope.Message
                    match Map.tryFind identity groups with
                    | Some group ->
                        group.Post((envelope, channel))
                        return! loop groups
                    | None ->
                        let group = spawnGroupActor identity reader appender clock
                        group.Post((envelope, channel))
                        return! loop (Map.add identity group groups)
                }

                loop Map.empty