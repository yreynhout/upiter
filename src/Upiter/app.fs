namespace Upiter
    open System
    open System.Security.Cryptography
    open FSharp.Control

    open Serilog
    
    open NodaTime

    open Upiter.Messages
    open Upiter.Messages.GroupContracts
    open Upiter.Model.Group
    
    open Newtonsoft.Json
    open SqlStreamStore
    open SqlStreamStore.Streams
    
    module App =
        let reader (store: IStreamStore) (settings: JsonSerializerSettings) (identity: GroupIdentity) (start: Int32) : Async<ReadResult> = async {
            let stream = StreamId(sprintf "%d~%s" identity.TenantId (identity.GroupId.ToString("N")))
            let! initialPage = store.ReadStreamForwards(stream, start, 100, true, Async.DefaultCancellationToken) |> Async.AwaitTask
            if initialPage.Status = PageReadStatus.StreamNotFound then
                return (-1, AsyncSeq.empty)
            else
                let generator (page: ReadStreamPage) : Async<(Events[] * ReadStreamPage) option> = async {
                    //this is flawed and verbose - but I do want to keep 
                    if page.IsEnd then
                        return None
                    else
                        let! events =
                            page.Messages
                            |> AsyncSeq.ofSeq
                            |> AsyncSeq.chooseAsync (fun message -> async {
                                let! json = message.GetJsonData(Async.DefaultCancellationToken) |> Async.AwaitTask
                                return 
                                    match message.Type with
                                    | "PrivateGroupWasStarted" -> 
                                        Some(PrivateGroupWasStarted(JsonConvert.DeserializeObject(json, typeof<PrivateGroupWasStarted>, settings) :?> PrivateGroupWasStarted))
                                    | "PublicGroupWasStarted" ->
                                        Some(PublicGroupWasStarted(JsonConvert.DeserializeObject(json, typeof<PublicGroupWasStarted>, settings) :?> PublicGroupWasStarted))
                                    | "GroupWasRenamed" ->
                                        Some(GroupWasRenamed(JsonConvert.DeserializeObject(json, typeof<GroupWasRenamed>, settings) :?> GroupWasRenamed))
                                    | "GroupInformationWasChanged" ->
                                        Some(GroupInformationWasChanged(JsonConvert.DeserializeObject(json, typeof<GroupInformationWasChanged>, settings) :?> GroupInformationWasChanged))
                                    | "GroupMembershipInvitationPolicyWasSet" ->
                                        Some(GroupMembershipInvitationPolicyWasSet(JsonConvert.DeserializeObject(json, typeof<GroupMembershipInvitationPolicyWasSet>, settings) :?> GroupMembershipInvitationPolicyWasSet))
                                    | "GroupModerationPolicyWasSet" ->
                                        Some(GroupModerationPolicyWasSet(JsonConvert.DeserializeObject(json, typeof<GroupModerationPolicyWasSet>, settings) :?> GroupModerationPolicyWasSet))
                                    | "GroupWasDeleted" ->
                                        Some(GroupWasDeleted(JsonConvert.DeserializeObject(json, typeof<GroupWasDeleted>, settings) :?> GroupWasDeleted))
                                    | _ -> None
                            })
                            |> AsyncSeq.toArrayAsync
                        let! nextPage = page.ReadNext(Async.DefaultCancellationToken) |> Async.AwaitTask
                        return Some (events, nextPage)
                }
                return (initialPage.LastStreamVersion, (AsyncSeq.unfoldAsync generator initialPage))
        }

        // let appender (store: IStreamStore) (settings: JsonSerializerSettings) (identity: GroupIdentity) (request: Guid) (expected: Int32) (events: Events[]) : Async<AppendResult> = async {
        //     let stream = StreamId(sprintf "%d~%s" identity.TenantId (identity.GroupId.ToString("N")))
        //     using (MD5.Create()) (fun hash ->
        //         let requestBytes = request.ToByteArray()

        //         hash.ComputeHash(
        //     )
        //     let messages =
        //         events
        //         |> Array.mapi (fun index message -> 
        //             new NewStreamMessage(
        //         )

        //     let! result = store.AppendToStream(stream, expected, messages, Async.DefaultCancellationToken) |> Async.AwaitTask
        //     return (-1, -1L)
        // }
            

        //let appender identity expected 
        //let router = spawnGroupRouter reader appender SystemClock.Instance