namespace Upiter.Messages
    open System

    module CommentContracts =
        [<CLIMutable>]
        type CommentOnMessage =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                MessageId: Guid
                CommentId: Guid
                Body: String
            }

        [<CLIMutable>]
        type MessageWasCommentedOn =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                CommentId: Guid
                Body: String
                When: Int64
            }

        [<CLIMutable>]
        type EditComment =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                CommentId: Guid
                Body: String
            }

        [<CLIMutable>]
        type CommentWasEdited =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                CommentId: Guid
                Body: String
                When: Int64
            }

        [<CLIMutable>]
        type DeleteComment =
            {
                TenantId: Int32
                PlatformMemberId: Guid
                CommentId: Guid
            }

        [<CLIMutable>]
        type CommentWasDeleted =
            {
                TenantId: Int32
                GroupId: Guid
                PlatformMemberId: Guid
                MessageId: Guid
                CommentId: Guid
                When: Int64
            }