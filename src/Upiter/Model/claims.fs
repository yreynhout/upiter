namespace Upiter.Model
    open System
    open System.Security.Claims

    module Claims =
        [<Literal>]
        let PlatformRole = "platform-role"
        [<Literal>]
        let PlatformAdministrator = "administrator"
        [<Literal>]
        let PlatformMember = "member"
        [<Literal>]
        let PlatformGuest = "guest"

        type ClaimsPrincipal with
            member this.IsPlatformAdministrator =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = PlatformRole && claim.Value = PlatformAdministrator)