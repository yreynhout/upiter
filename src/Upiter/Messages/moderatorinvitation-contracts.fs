namespace Upiter.Messages
    open System

    module ModeratorInvitationContracts =            
        [<CLIMutable>]
        type InvitePlatformMemberToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformMemberWasInvitedToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                InviterId: Guid
                PersonalMessage: String
                When: Int64
            }

        [<CLIMutable>]
        type InvitePlatformGuestToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeEmailAddress: String
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformGuestWasInvitedToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeEmailAddress: String
                InviterId: Guid
                PersonalMessage: String
                When: Int64
            }
        
        [<CLIMutable>]
        type ConfirmPlatformGuestIdentityOfModeratorInvitation = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type PlatformGuestIdentityOfModeratorInvitationWasConfirmed = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type AcceptInvitationToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeModeratorOfGroupWasAccepted = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RejectInvitationToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }
        
        [<CLIMutable>]
        type InvitationToBecomeModeratorOfGroupWasRejected = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RevokeInvitationToBecomeModeratorOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                PlatformMemberId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeModeratorOfGroupWasRevoked = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                PlatformMemberId: Guid
                When: Int64
            }