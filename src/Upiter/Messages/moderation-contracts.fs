namespace Upiter.Messages
    open System

    module MessageModerationContracts =
        [<CLIMutable>]
        type FlagMessageAsInappropriate =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasFlaggedAsInappropriate =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type FlagMessageAsSpam =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasFlaggedAsSpam =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type TrustFutureMessagesFromGroupMember =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
            }
        
        [<CLIMutable>]
        type FutureMessagesFromGroupMemberWereTrusted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type BanGroupMember =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
                When: Int64
            }
        
        [<CLIMutable>]
        type GroupMemberWasBanned =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
                When: Int64
            }
        
        [<CLIMutable>]        
        type BlockGroupMember = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
            }
        
        [<CLIMutable>]        
        type GroupMemberWasBlocked = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type ReportGroupMember =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
            }

        [<CLIMutable>]        
        type GroupMemberWasReported = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                GroupMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type TurnGroupMessageModerationOn =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
            }
        
        [<CLIMutable>]        
        type GroupMessageModerationWasTurnedOn =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type TurnGroupMessageModerationOff =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
            }
        
        [<CLIMutable>]        
        type GroupMessageModerationWasTurnedOff =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type ChangeGroupBlacklistWords =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Blacklist: String[]
            }

        [<CLIMutable>]
        type GroupBlacklistWordsWereChanged =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                Blacklist: String[]
                When: Int32
            }

        [<CLIMutable>]
        type ApproveMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }
        
        [<CLIMutable>]
        type MessageWasApproved =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type RejectMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasRejected =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }