namespace Upiter.Messages
    open System

    module PlatformMemberContracts = 
        [<CLIMutable>]
        type WelcomePlatformMember = 
            { 
                TenantId: Int32
                PlatformMemberId: Guid
                EmailAddress: String
            }

        [<CLIMutable>]
        type PlatformGuestBecamePlatformMember = 
            { 
                TenantId: Int32
                PlatformMemberId: Guid
                EmailAddress: String
                When: Int64
            }