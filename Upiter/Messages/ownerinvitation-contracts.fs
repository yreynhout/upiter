namespace Upiter.Messages
    open System

    module OwnerInvitationContracts =            
        [<CLIMutable>]
        type InvitePlatformMemberToBecomeOwnerOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformMemberWasInvitedToBecomeOwnerOfGroup = 
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
        type InvitePlatformGuestToBecomeOwnerOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeEmailAddress: String
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformGuestWasInvitedToBecomeOwnerOfGroup = 
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
        type ConfirmPlatformGuestIdentityOfOwnerInvitation = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type PlatformGuestIdentityOfOwnerInvitationWasConfirmed = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type AcceptInvitationToBecomeOwnerOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeOwnerOfGroupWasAccepted = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RejectInvitationToBecomeOwnerOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }
        
        [<CLIMutable>]
        type InvitationToBecomeOwnerOfGroupWasRejected = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RevokeInvitationToBecomeOwnerOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                PlatformMemberId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeOwnerOfGroupWasRevoked = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                PlatformMemberId: Guid
                When: Int64
            }