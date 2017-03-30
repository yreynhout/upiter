namespace Upiter.Security
    open System
    open System.Security.Claims

    type PlatformVisitor(principal: ClaimsPrincipal) =
        let tenant =
            let claim = 
                principal.Claims
                |> Seq.find (fun claim -> claim.Type = Claims.Tenant)
            Int32.Parse(claim.Value)
        member this.Tenant = tenant
        member this.IsPlatformAdministrator() =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.PlatformRole && claim.Value = Claims.PlatformAdministrator)
        member this.IsPlatformMember() =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.PlatformRole && claim.Value = Claims.PlatformMember)
        member this.IsPlatformGuest() =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.PlatformRole && claim.Value = Claims.PlatformGuest)
        member this.IsGroupOwner (group: Guid) =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) Claims.GroupOwner))
        member this.IsGroupMember (group: Guid) =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) Claims.GroupMember))
        member this.IsGroupModerator (group: Guid) =
            principal.Claims
            |> Seq.exists (fun claim -> claim.Type = Claims.GroupRole && claim.Value = (sprintf "%s#%s" (group.ToString("N")) Claims.GroupModerator))