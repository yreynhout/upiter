namespace Upiter.Messages
    open System

    module MessageContracts =
        // type AutoArchiveMessage =
        // | Never
        // | AfterInactivityOf of Int64
        // | AsOf of Int64
        
        // type AutoCloseComments =
        // | Never
        // | After of Int64
        // | AsOf of Int64

        [<CLIMutable>]
        type PostMessage =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
                AllowComments: Boolean
                AllowSharing: Boolean
            }

        [<CLIMutable>]
        type MessageWasPosted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
                AllowComments: Boolean
                AllowSharing: Boolean
                When: Int64
            }

        [<CLIMutable>]
        type UpvoteMessage = 
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasUpvoted = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type DownvoteMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }
        
        [<CLIMutable>]
        type MessageWasDownvoted = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }
                
        [<CLIMutable>]
        type ExpressReactionToMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                Emoji: String
            }

        [<CLIMutable>]
        type MessageReactionWasExpressed =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Emoji: String
                When: Int64
            }

        [<CLIMutable>]
        type BookmarkMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasBookmarked =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type EnableCommentsOnMessage = 
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type CommentsWereEnabledOnMessage = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64

            }

        [<CLIMutable>]
        type DisableCommentsOnMessage = 
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type CommentsWereDisabledOnMessage = 
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                When: Int64
            }
        //AllowCommentingOnMessage, DisableCommentingOnMessage
        //AllowMessageToBeShared, DisableSharingOfMessage
        //ShareMessageWithGroup, ShareMessageWithPlatformGuest, ShareMessageOnTwitter, Share

        type PinDuration =
        | Forever
        | Until of Int64
        | For of Int64

        [<CLIMutable>]
        type PinMessage =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Duration: PinDuration
            }

        [<CLIMutable>]
        type MessageWasPinned =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Duration: PinDuration
            }

        [<CLIMutable>]
        type UnpinMessage =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasUnpinned =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
            }
        
        [<CLIMutable>]
        type EditMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
            }

        [<CLIMutable>]
        type MessageWasEdited =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
            }
        
        [<CLIMutable>]
        type DeleteMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
            }

        [<CLIMutable>]
        type MessageWasDeleted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
            }