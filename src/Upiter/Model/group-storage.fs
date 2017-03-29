namespace Upiter.Model
    open System
    open FSharp.Control

    open Serilog
    open Serilog.Core

    open Upiter.Messages
    open Upiter.Messages.GroupContracts
    open Upiter.Model.Group
    
    open Newtonsoft.Json
    open SqlStreamStore
    open SqlStreamStore.Streams

    module GroupStorage =
        let private log = Log.ForContext(Constants.SourceContextPropertyName, "GroupStorage")

        [<Literal>]
        let private ReadBatchSize = 100
        let reader (store: IStreamStore) (settings: JsonSerializerSettings) (identity: GroupIdentity) (start: Int32) : Async<ReadFromStreamResult> = async {
            let stream = StreamId(sprintf "%d~%s" identity.TenantId (identity.GroupId.ToString("N")))
            log.Debug("Reading from stream {name}", stream.ToString())
            let! initialPage = store.ReadStreamForwards(stream, start, ReadBatchSize, true, Async.DefaultCancellationToken) |> Async.AwaitTask
            if initialPage.Status = PageReadStatus.StreamNotFound then
                return (ExpectedVersion.NoStream, AsyncSeq.empty)
            else
                let generator (page: ReadStreamPage option) : Async<(Events[] * ReadStreamPage option) option> = async {
                    match page with
                    | None -> return None
                    | Some current ->
                        let! events =
                            current.Messages
                            |> Seq.filter (fun message -> message.StreamVersion <= initialPage.LastStreamVersion)
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
                        if current.NextStreamVersion > initialPage.LastStreamVersion || current.IsEnd then
                            return Some((events, None))
                        else
                            let! next = current.ReadNext(Async.DefaultCancellationToken) |> Async.AwaitTask
                            return Some((events, Some(next)))
                }
                return (initialPage.LastStreamVersion, (AsyncSeq.unfoldAsync generator (Some initialPage)))
        }

        let appender (store: IStreamStore) (settings: JsonSerializerSettings) (identity: GroupIdentity) (request: Guid) (expected: Int32) (events: Events[]) : Async<AppendToStreamResult> = async {
            let stream = StreamId(sprintf "%d~%s" identity.TenantId (identity.GroupId.ToString("N")))
            log.Debug("Appending to stream {name}", stream.ToString())
            let messages =
                events
                |> Array.mapi (fun index message -> 
                    let messageId = MessageIdentity.generate request index
                    let (messageType, messageJson, ``when``, who) = 
                        match message with
                        | PrivateGroupWasStarted event -> 
                            ("PrivateGroupWasStarted", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | PublicGroupWasStarted event -> 
                            ("PublicGroupWasStarted", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | GroupWasRenamed event-> 
                            ("GroupWasRenamed", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | GroupInformationWasChanged event -> 
                            ("GroupInformationWasChanged", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | GroupMembershipInvitationPolicyWasSet event -> 
                            ("GroupMembershipInvitationPolicyWasSet", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | GroupModerationPolicyWasSet event -> 
                            ("GroupModerationPolicyWasSet", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                        | GroupWasDeleted event -> 
                            ("GroupWasDeleted", JsonConvert.SerializeObject(event, settings), event.When, event.PlatformMemberId)
                    let metadataJson =
                        JsonConvert.SerializeObject(
                            dict [
                                "Request", (request.ToString("N"))
                                "When", (``when``.ToString())
                                "Who", (who.ToString("N"))
                                "Machine", Environment.MachineName
                            ], settings)
                    NewStreamMessage(messageId, messageType, messageJson, metadataJson)
                )
            let! result = store.AppendToStream(stream, expected, messages, Async.DefaultCancellationToken) |> Async.AwaitTask
            //TODO: once stream store returns current position, we're golden - until then lets return the head
            let! head = store.ReadHeadPosition(Async.DefaultCancellationToken) |> Async.AwaitTask
            return (result.CurrentVersion, head)
        }
