namespace Upiter.Messages
    open System

    module PollContracts =
        type PollDuration =
        | For of Int64
        | Until of Int64

        // [<CLIMutable>]
        // type ComposePoll =
        //     {
        //         TenantId: Int32
        //         GroupId: Guid
        //         PollId: Guid
        //         PlatformMemberId: Guid
        //         Duration: PollDuration
        //         Choices: String[]
        //     }

        // [<CLIMutable>]
        // type PollWasComposed = 
        //     {
        //         TenantId: Int32
        //         GroupId: Guid
        //         PollId: Guid
        //         PlatformMemberId: Guid
        //         Duration: PollDuration
        //         Choices: String[]
        //         When: Int64
        //     }

        // [<CLIMutable>]
        // type EditPoll = 
        //     {
        //         TenantId: Int32
        //         PollId: Guid
        //         PlatformMemberId: Guid
        //         Duration: PollDuration
        //         Choices: String[]
        //     }

        // [<CLIMutable>]
        // type PollWasEdited = 
        //     {
        //         TenantId: Int32
        //         PollId: Guid
        //         PlatformMemberId: Guid
        //         Duration: PollDuration
        //         Choices: String[]
        //         When: Int64
        //     }

        [<CLIMutable>]
        type StartPoll = 
            {
                TenantId: Int32
                GroupId: Guid
                PollId: Guid
                PlatformMemberId: Guid
                Title: String
                Body: String
                Duration: PollDuration
                Choices: String[]
            }

        [<CLIMutable>]
        type PollWasStarted = 
            {
                TenantId: Int32
                GroupId: Guid
                PollId: Guid
                PlatformMemberId: Guid
                Duration: PollDuration
                Choices: String[]
                When: Int64
            }

        [<CLIMutable>]
        type CancelPoll = 
            {
                TenantId: Int32
                PollId: Guid
                PlatformMemberId: Guid
            }
        
        [<CLIMutable>]
        type PollWasCancelled = 
            {
                TenantId: Int32
                GroupId: Guid
                PollId: Guid
                PlatformMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type ClosePoll = 
            {
                TenantId: Int32
                PollId: Guid
                PlatformMemberId: Guid
            }
        
        [<CLIMutable>]
        type PollWasClosed = 
            {
                TenantId: Int32
                GroupId: Guid
                PollId: Guid
                PlatformMemberId: Guid
                When: Int64
            }

        [<CLIMutable>]
        type VoteOnPoll = 
            {
                TenantId: Int32
                PollId: Guid
                PlatformMemberId: Guid
                Choice: Int32
            }

        [<CLIMutable>]
        type PollWasVotedOn = 
            {
                TenantId: Int32
                GroupId: Guid
                PollId: Guid
                PlatformMemberId: Guid
                Choice: Int32
                When: Int64
            }