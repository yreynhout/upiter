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

        [<Literal>]
        let GroupRole = "group-role"
        [<Literal>]
        let GroupOwner = "owner"
        [<Literal>]
        let GroupMember = "member"
        [<Literal>]
        let GroupModerator = "moderator"

        type ClaimsPrincipal with
            member this.IsPlatformAdministrator() =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = PlatformRole && claim.Value = PlatformAdministrator)
            member this.IsPlatformMember() =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = PlatformRole && claim.Value = PlatformMember)
            member this.IsPlatformGuest() =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = PlatformRole && claim.Value = PlatformGuest)
            member this.IsGroupOwner (group: Guid) =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) GroupOwner))
            member this.IsGroupMember (group: Guid) =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) GroupMember))
            member this.IsGroupModerator (group: Guid) =
                this.Claims
                |> Seq.exists (fun claim -> claim.Type = GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) GroupModerator))
            