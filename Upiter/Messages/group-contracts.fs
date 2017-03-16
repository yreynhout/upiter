namespace Upiter.Messages
    open System

    module GroupContracts =            
        [<CLIMutable>]
        type StartPrivateGroup =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                Purpose: String
            }

        [<CLIMutable>]
        type PrivateGroupWasStarted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                Purpose: String
                When: Int64
            }

        [<CLIMutable>]
        type StartPublicGroup =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                Purpose: String
            }

        [<CLIMutable>]
        type PublicGroupWasStarted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                Purpose: String
                When: Int64
            }
            
        [<CLIMutable>]
        type ChangeGroupInformation = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Purpose: string
            }

        [<CLIMutable>]
        type GroupInformationWasChanged =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Purpose: String
                When: Int64
            }

        [<CLIMutable>]
        type RenameGroup =
            {
                TenantId: Int32                
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
            }

        [<CLIMutable>]
        type GroupWasRenamed =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                When: Int64
            }
        

        [<CLIMutable>]
        type DeleteGroup =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
            }        

        [<CLIMutable>]
        type GroupWasDeleted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                When: Int64
            }

        [<CLIMutable>]
        type SetGroupMembershipInvitationPolicy =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                AllowMembersToInvite: Boolean
                AllowModeratorsToInvite: Boolean
                AllowOwnersToInvite: Boolean
            }

        [<CLIMutable>]
        type GroupMembershipInvitationPolicyWasSet =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                AllowMembersToInvite: Boolean
                AllowModeratorsToInvite: Boolean
                AllowOwnersToInvite: Boolean
                When: Int64
            }