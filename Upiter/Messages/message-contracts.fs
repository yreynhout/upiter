namespace Upiter.Messages
    open System

    module MessageContracts =            
        [<CLIMutable>]
        type PostMessage =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
                Links: String[]
                Media: String[]
                Locations: String[]
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
                Links: String[]
                Media: String[]
                Locations: String[]
                AllowComments: Boolean
                AllowSharing: Boolean
            }
                
        [<CLIMutable>]
        type ReactToMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                Emoji: String
            }

        [<CLIMutable>]
        type MessageWasReactedTo =
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
        type EditMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                Title: String
                Body: String
                Links: String[]
                Media: String[]
                Locations: String[]
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
                Links: String[]
                Media: String[]
                Locations: String[]
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