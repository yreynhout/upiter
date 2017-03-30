namespace Yoga
    open System
    open System.Text
    open System.Security.Cryptography
    open System.Security.Claims
    open System.Runtime.Caching

    open Serilog
    open Serilog.Core

    open NodaTime

    open Newtonsoft.Json

    open SqlStreamStore

    open Upiter.Security
    
    open Suave
    open Suave.Filters
    open Suave.Writers
    open Suave.Operators
    open Suave.Redirection
    open Suave.RequestErrors
    open Suave.Successful

    open Upiter.AppSecurity

    open Yoga.Projections.Projection

    module App =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "App")

        let private getPlatformVisitor (userState: Map<string, obj>) =
            match Map.tryFind Upiter.AppSecurity.PlatformMemberKey userState with
            | Some principal -> PlatformVisitor(principal :?> ClaimsPrincipal)
            | None -> PlatformVisitor(ClaimsPrincipal()) //TODO: Map subdomain to tenant
        let private listGroups (cache: MemoryCache) (httpJsonSettings: JsonSerializerSettings) =
            fun (context : HttpContext) -> async {
                let visitor = getPlatformVisitor context.userState
                let pattern = sprintf "group~%d~" visitor.Tenant
                let groups =
                    cache
                    |> Seq.filter (fun item -> item.Key.StartsWith(pattern))
                    |> Seq.map (fun item -> item.Value :?> GroupDocument)
                    |> Seq.toArray
                
                use hash = MD5.Create()

                groups
                |> Array.iter (
                    fun group -> 
                        hash.ComputeHash(Array.concat [ BitConverter.GetBytes(group.TenantId); (group.GroupId.ToByteArray()) ]) 
                        |> ignore
                )

                let builder = 
                    hash.Hash
                    |> Array.fold (
                        fun (builder: StringBuilder) (value: Byte) ->
                            builder.Append(sprintf "%02X" value)
                    ) (StringBuilder())

                let etag = sprintf "W/\"%s\"" (builder.ToString())

                let ifNoneMatch = Suave.Headers.getHeader "If-None-Match" context
                if ifNoneMatch |> Seq.exists (fun header -> header = etag) then
                    return! (
                        setMimeType "application/json"
                        >=> setHeader "ETag" etag
                        >=> NOT_MODIFIED)
                        context
                else
                    return! (
                        setMimeType "application/json" 
                        >=> OK (JsonConvert.SerializeObject(groups, httpJsonSettings)))
                        context
            }

        let private getGroup (cache: MemoryCache) (httpJsonSettings: JsonSerializerSettings) (id: string) =
            fun (context : HttpContext) -> async {
                let visitor = getPlatformVisitor context.userState
                let key = sprintf "group~%d~%s" visitor.Tenant id
                match cache |> Seq.tryFind (fun item -> item.Key = key) with
                | Some item ->
                    let document = item.Value :?> GroupDocument
                    let etag = sprintf "W/\"%s\"" (document.Position.ToString())
                    let ifNoneMatch = Suave.Headers.getHeader "If-None-Match" context
                    if ifNoneMatch |> Seq.exists (fun header -> header = etag) then
                        return! (
                            setMimeType "application/json"
                            >=> setHeader "ETag" etag
                            >=> NOT_MODIFIED)
                            context
                    else
                        return! (
                            setMimeType "application/json" 
                            >=> setHeader "ETag" etag
                            >=> OK (JsonConvert.SerializeObject(document, httpJsonSettings)))
                            context
                | None ->
                    let problem : Upiter.HttpProblemDetails = 
                        {
                            Type = new Uri("https://tools.ietf.org/html/rfc7231#section-6.5.4")
                            Title = "The requested group could not be found."
                            Details = null
                            Instance = null
                            Status = 404
                        }
                    return! (
                        setMimeType "application/problem+json" 
                        >=> NOT_FOUND (JsonConvert.SerializeObject(problem, httpJsonSettings)))
                        context
            }
        let app (authenticationOptions: JwtBearerAuthenticationOptions) (httpJsonSettings: JsonSerializerSettings) (cache: MemoryCache) =
            authorizeRequest authenticationOptions httpJsonSettings <|
                choose 
                    [
                        GET >=> choose [
                            path "/api/groups" >=> listGroups cache httpJsonSettings
                            pathScan "/api/groups/%s" (getGroup cache httpJsonSettings)
                        ]
                    ]