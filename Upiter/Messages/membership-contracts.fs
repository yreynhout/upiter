namespace Upiter.Messages
    open System

    module MembershipContracts =            
        //Commands
        [<CLIMutable>]
        type BecomeMemberOfGroup = 
            { 
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                MemberId: Guid
            }

        [<CLIMutable>]        
        type PlatformMemberBecameMemberOfGroup =
            {
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                MemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type BecomeOwnerOfGroup = 
            { 
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                OwnerId: Guid
            }
        
        [<CLIMutable>]        
        type PlatformMemberBecameOwnerOfGroup =
            {
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                OwnerId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type BecomeModeratorOfGroup = 
            { 
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                ModeratorId: Guid
            }

        [<CLIMutable>]        
        type PlatformMemberBecameModeratorOfGroup =
            {
                MembershipId: Guid
                TenantId: Int32
                GroupId: Guid
                OwnerId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type CancelGroupMembership = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
                Reason: String
            }

        [<CLIMutable>]
        type GroupMembershipWasCancelled = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
                Reason: String
                When: Int64
            }
        
        [<CLIMutable>]
        type RevokeGroupMembership = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
                Reason: String
            }
        
        [<CLIMutable>]
        type GroupMembershipWasRevoked = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
                Reason: String
                When: Int64
            }

        [<CLIMutable>]
        type ArchiveGroupMembership = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
            }

        [<CLIMutable>]
        type GroupMembershipWasArchived = 
            { 
                TenantId: Int32
                GroupId: Guid
                MembershipId: Guid
                PlatformMemberId: Guid
                When: Int64
            }