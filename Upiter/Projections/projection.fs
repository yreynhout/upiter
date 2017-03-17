namespace Upiter.Projections
    open System
    open System.Runtime.Caching

    open Upiter.Messages
    open Upiter.Messages.GroupContracts

    module Projection =

        type GroupAccessibility = Private = 0 | Public = 1
        type MembershipInvitationPolicy =
            {
                AllowMembersToInvite    : Boolean
                AllowModeratorsToInvite : Boolean
                AllowOwnersToInvite     : Boolean
            }
        type GroupDocument = 
            {
                TenantId                   : Int32
                GroupId                    : Guid
                Name                       : String
                Purpose                    : String
                StartedById                : Guid
                Accessibility              : GroupAccessibility
                MembershipInvitationPolicy : MembershipInvitationPolicy
            }
        
        type private ProjectionEvents =
        | PublicGroupWasStarted                 of PublicGroupWasStarted
        | PrivateGroupWasStarted                of PrivateGroupWasStarted
        | GroupWasRenamed                       of GroupWasRenamed
        | GroupInformationWasChanged            of GroupInformationWasChanged
        | GroupMembershipInvitationPolicyWasSet of GroupMembershipInvitationPolicyWasSet
        | GroupWasDeleted                       of GroupWasDeleted
        with 
            static member FromEnvelope (envelope: Envelope) =
                match envelope.Message with
                | :? PublicGroupWasStarted                 as msg -> Some (PublicGroupWasStarted msg)
                | :? PrivateGroupWasStarted                as msg -> Some (PrivateGroupWasStarted msg)
                | :? GroupWasRenamed                       as msg -> Some (GroupWasRenamed msg)
                | :? GroupInformationWasChanged            as msg -> Some (GroupInformationWasChanged msg)
                | :? GroupMembershipInvitationPolicyWasSet as msg -> Some (GroupMembershipInvitationPolicyWasSet msg)
                | :? GroupWasDeleted                       as msg -> Some (GroupWasDeleted msg)
                | _ -> None

        let instance (cache: MemoryCache) (envelope: Envelope) =
            match ProjectionEvents.FromEnvelope envelope with
            | Some message ->
                match message with
                | PublicGroupWasStarted msg -> 
                    let document = 
                        {
                            TenantId = msg.TenantId
                            GroupId = msg.GroupId
                            Name = msg.Name
                            Purpose = msg.Purpose
                            StartedById = msg.PlatformMemberId
                            Accessibility = GroupAccessibility.Public
                            MembershipInvitationPolicy = 
                                {
                                    AllowMembersToInvite = true
                                    AllowModeratorsToInvite = true
                                    AllowOwnersToInvite = true
                                }
                        }
                    let item = CacheItem(msg.GroupId.ToString("N"), document)
                    cache.Add(item, CacheItemPolicy()) |> ignore
                | PrivateGroupWasStarted msg -> 
                    let document = 
                        {
                            TenantId = msg.TenantId
                            GroupId = msg.GroupId
                            Name = msg.Name
                            Purpose = msg.Purpose
                            StartedById = msg.PlatformMemberId
                            Accessibility = GroupAccessibility.Private
                            MembershipInvitationPolicy = 
                                {
                                    AllowMembersToInvite = false
                                    AllowModeratorsToInvite = true
                                    AllowOwnersToInvite = true
                                }
                        }
                    let item = CacheItem(msg.GroupId.ToString("N"), document)
                    cache.Add(item, CacheItemPolicy()) |> ignore
                | GroupWasRenamed msg ->
                    let item = cache.GetCacheItem(msg.GroupId.ToString("N"))
                    let document = item.Value :?> GroupDocument
                    item.Value <- { document with Name = msg.Name }
                | GroupInformationWasChanged msg ->
                    let item = cache.GetCacheItem(msg.GroupId.ToString("N"))
                    let document = item.Value :?> GroupDocument
                    item.Value <- { document with Purpose = msg.Purpose }
                | GroupMembershipInvitationPolicyWasSet msg ->
                    let item = cache.GetCacheItem(msg.GroupId.ToString("N"))
                    let document = item.Value :?> GroupDocument
                    item.Value <- 
                        { document with MembershipInvitationPolicy = 
                                            {
                                                AllowMembersToInvite = msg.AllowMembersToInvite
                                                AllowModeratorsToInvite = msg.AllowModeratorsToInvite
                                                AllowOwnersToInvite = msg.AllowOwnersToInvite
                                            }
                        }
                | GroupWasDeleted msg ->
                    cache.Remove(msg.GroupId.ToString("N")) |> ignore
            | None -> ()
                