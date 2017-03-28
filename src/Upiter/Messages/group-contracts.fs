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
            with override this.ToString() = sprintf "[Tenant:%d]Starting private group %s - %s" this.TenantId (this.GroupId.ToString("N")) this.Name

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
            with override this.ToString() = sprintf "[Tenant:%d]Started private group %s - %s" this.TenantId (this.GroupId.ToString("N")) this.Name

        [<CLIMutable>]
        type StartPublicGroup =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                Purpose: String
            }
            with override this.ToString() = sprintf "[Tenant:%d]Starting public group %s - %s" this.TenantId (this.GroupId.ToString("N")) this.Name

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
            with override this.ToString() = sprintf "[Tenant:%d]Started public group %s - %s" this.TenantId (this.GroupId.ToString("N")) this.Name
            
        [<CLIMutable>]
        type ChangeGroupInformation = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Purpose: string
            }
            with override this.ToString() = sprintf "[Tenant:%d]Changing group %s information: %s" this.TenantId (this.GroupId.ToString("N")) this.Purpose

        [<CLIMutable>]
        type GroupInformationWasChanged =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Purpose: String
                When: Int64
            }
            with override this.ToString() = sprintf "[Tenant:%d]Changed group %s information: %s" this.TenantId (this.GroupId.ToString("N")) this.Purpose

        [<CLIMutable>]
        type RenameGroup =
            {
                TenantId: Int32                
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
            }
            with override this.ToString() = sprintf "[Tenant:%d]Renaming group %s: %s" this.TenantId (this.GroupId.ToString("N")) this.Name

        [<CLIMutable>]
        type GroupWasRenamed =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Name: String
                When: Int64
            }
            with override this.ToString() = sprintf "[Tenant:%d]Renamed group %s: %s" this.TenantId (this.GroupId.ToString("N")) this.Name

        [<CLIMutable>]
        type DeleteGroup =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
            }
            with override this.ToString() = sprintf "[Tenant:%d]Delete group %s" this.TenantId (this.GroupId.ToString("N"))

        [<CLIMutable>]
        type GroupWasDeleted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                When: Int64
            }
            with override this.ToString() = sprintf "[Tenant:%d]Deleted group %s" this.TenantId (this.GroupId.ToString("N"))

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
            with override this.ToString() = sprintf "[Tenant:%d]Set group %s membership invitiation policy" this.TenantId (this.GroupId.ToString("N"))

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
            with override this.ToString() = sprintf "[Tenant:%d]Group %s membership invitiation policy was set" this.TenantId (this.GroupId.ToString("N"))

        type ModerationChoice = Yes = 0 | Moderated = 1 | No = 2

        [<CLIMutable>]
        type SetGroupModerationPolicy =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                AllowPlatformGuestsToComment: ModerationChoice
                AllowPlatformMembersToComment: ModerationChoice
                AllowGroupMembersToComment: ModerationChoice
                AllowPlatformMembersToPost: ModerationChoice
                AllowGroupMembersToPost: ModerationChoice
                RequireModerationAsOfLinkCount: Int32
                RequireModerationAsOfImageCount: Int32
                RequireModerationAsOfMediaCount: Int32
            }
            with override this.ToString() = sprintf "[Tenant:%d]Set group %s moderation policy" this.TenantId (this.GroupId.ToString("N"))            

        [<CLIMutable>]
        type GroupModerationPolicyWasSet =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                AllowPlatformGuestsToComment: ModerationChoice
                AllowPlatformMembersToComment: ModerationChoice
                AllowGroupMembersToComment: ModerationChoice
                AllowPlatformMembersToPost: ModerationChoice
                AllowGroupMembersToPost: ModerationChoice
                RequireModerationAsOfLinkCount: Int32
                RequireModerationAsOfImageCount: Int32
                RequireModerationAsOfMediaCount: Int32
                When: Int64
            }
            with override this.ToString() = sprintf "[Tenant:%d]Group %s moderation policy was set" this.TenantId (this.GroupId.ToString("N"))