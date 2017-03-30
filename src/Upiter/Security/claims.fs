namespace Upiter.Security
    open System
    open System.Security.Claims

    module Claims =
        [<Literal>]
        let Tenant = "tenant"
        [<Literal>]
        let PlatformRole = "platform-role"
        [<Literal>]
        let PlatformAdministrator = "administrator"
        [<Literal>]
        let PlatformMember = "member"
        [<Literal>]
        let PlatformGuest = "guest"

        [<Literal>]
        let GroupRole = "group-role"
        [<Literal>]
        let GroupOwner = "owner"
        [<Literal>]
        let GroupMember = "member"
        [<Literal>]
        let GroupModerator = "moderator"