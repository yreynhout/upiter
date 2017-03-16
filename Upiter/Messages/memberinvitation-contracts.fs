namespace Upiter.Messages
    open System

    module MemberInvitationContracts =            
        [<CLIMutable>]
        type InvitePlatformMemberToBecomeMemberOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformMemberWasInvitedToBecomeMemberOfGroup = 
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
        type InvitePlatformGuestToBecomeMemberOfGroup = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeEmailAddress: String
                InviterId: Guid
                PersonalMessage: String
            }

        [<CLIMutable>]
        type PlatformGuestWasInvitedToBecomeMemberOfGroup = 
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
        type ConfirmPlatformGuestIdentityOfMemberInvitation = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type PlatformGuestIdentityOfMemberInvitationWasConfirmed = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type AcceptInvitationToBecomeMemberOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeMemberOfGroupWasAccepted = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RejectInvitationToBecomeMemberOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                InviteeId: Guid
            }
        
        [<CLIMutable>]
        type InvitationToBecomeMemberOfGroupWasRejected = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                InviteeId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type RevokeInvitationToBecomeMemberOfGroup = 
            { 
                TenantId: Int32
                InvitationId: Guid
                PlatformMemberId: Guid
            }

        [<CLIMutable>]
        type InvitationToBecomeMemberOfGroupWasRevoked = 
            { 
                TenantId: Int32
                GroupId: Guid
                InvitationId: Guid
                PlatformMemberId: Guid
                When: Int64
            }