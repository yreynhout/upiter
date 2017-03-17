namespace Upiter.Model
    open System
    open FSharp.Control
    
    open NodaTime
    open Newtonsoft.Json
    open SqlStreamStore
    open SqlStreamStore.Streams

    open Upiter.Messages.GroupContracts

    module Group = ()
//         type Events =
//         | PrivateGroupWasStarted of PrivateGroupWasStarted
//         | PublicGroupWasStarted of PublicGroupWasStarted
//         | GroupWasRenamed of GroupWasRenamed
//         | GroupInformationWasChanged of GroupInformationWasChanged
//         | GroupMembershipInvitationPolicyWasSet of GroupMembershipInvitationPolicyWasSet
//         | GroupModerationPolicyWasSet of GroupModerationPolicyWasSet
//         | GroupWasDeleted of GroupWasDeleted

//         type ReadFromStream = (* stream *)String -> (* events *)AsyncSeq<StreamMessage[]>
//         type AppendToStream = (* stream *)String -> (* expected version *)Int32 -> (* events *)NewStreamMessage[] -> (* result *)Async<AppendResult>

//         type private States =
//         | Initial
//         | Started
//         | Deleted

//         type private State = 
//             {
//                 CurrentState: States
//             }
//             with 
//                 static member Initial = { CurrentState = States.Initial }
//                 static member Fold (initialState: State) (events: Events[]) =
//                     let folder (state: State) (event: Events) =
//                         match event with
//                         | PrivateGroupWasStarted _ -> { state with CurrentState = States.Started }
//                         | PublicGroupWasStarted _ -> { state with CurrentState = States.Started }
//                         | GroupWasRenamed _ -> state
//                         | GroupInformationWasChanged _ -> state
//                         | GroupMembershipInvitationPolicyWasSet _ -> state
//                         | GroupModerationPolicyWasSet _ -> state
//                         | GroupWasDeleted _ -> { state with CurrentState = States.Deleted }
                    
//                     Array.fold folder initialState events

//         type private Aggregate = { Stream: String; ExpectedVersion: Int32; Group: State; }

//         type Commands =
//         | StartPrivateGroup of StartPrivateGroup
//         | StartPublicGroup of StartPublicGroup
//         | RenameGroup of RenameGroup
//         | ChangeGroupInformation of ChangeGroupInformation
//         | SetGroupMembershipInvitationPolicy of SetGroupMembershipInvitationPolicy
//         | SetGroupModerationPolicy of SetGroupModerationPolicy
//         | DeleteGroup of DeleteGroup

//         type Error =
//         | BecauseGroupWasDeleted

//         type Partition = 
//             { 
//                 TenantId: Int32
//                 GroupId: Guid 
//             }
//             with member this.ToStream() = sprintf "%d-group-%s" this.TenantId (this.GroupId.ToString("N"))

//         let spawnGroupActor (partition: Partition) (store: IStreamStore) (settings: JsonSerializerSettings) = 
//             let stream = partition.ToStream()
//             let reader stream = async {
//                 let! page = store.ReadStreamForwards(stream, StreamVersion.Start, 100, true) |> Async.AwaitTask
//                 if page.Status = PageReadStatus.StreamNotFound then
//                     return { Stream = stream; ExpectedVersion = ExpectedVersion.NoStream; Group = State.Initial }
//                 else
//                     let generator group = async {
//                         page.Messages
//                         |> AsyncSeq.ofSeq
//                         |> AsyncSeq.mapAsync (fun message -> async {
//                             let! jsonData = message.GetJsonData() |> Async.AwaitTask

//                         })
//                         return None
//                     }
//                     AsyncSeq.unfoldAsync generator page
//             }
//             MailboxProcessor.Start
//             <| fun inbox ->
//                 let rec loop (group: Aggregate) = async {
//                     let! (envelope: Envelope<Commands>, channel: AsyncReplyChannel<Choice<Int64, Error>>) = inbox.Receive()
//                     let result : Choice<Events[], Error> =
//                         match envelope.Message with
//                         | StartPrivateGroup cmd -> Choice1Of2 [||]
//                         | StartPublicGroup cmd -> Choice1Of2 [||]
//                         | RenameGroup cmd -> Choice1Of2 [||]
//                         | ChangeGroupInformation cmd -> Choice1Of2 [||]
//                         | SetGroupMembershipInvitationPolicy cmd -> Choice1Of2 [||]
//                         | SetGroupModerationPolicy cmd -> Choice1Of2 [||]
//                         | DeleteGroup cmd -> Choice1Of2 [||]
//                     match result with
//                     | Choice1Of2 events ->
//                         let! appendResult = appender (partition.ToStream()) group.ExpectedVersion events
//                         channel.Reply(Choice1Of2 appendResult.Position)
//                         return! loop { group with ExpectedVersion = appendResult.NextExpectedVersion }
//                     | Choice2Of2 error ->
//                         channel.Reply(Choice2Of2 error)
//                         return! loop group
//                 }

//                 async {
//                     let state =
//                         reader stream
//                         |> AsyncSeq.mapAsync 
//                             (fun batch -> async {
//                                 batch
//                                 |> AsyncSeq.ofSeq
//                                 |> AsyncSeq.map
//                                 let! data = msg.GetJsonData() |> Async.AwaitTask
//                                 let deserialized = JsonConvert.DeserializeObject(data, contract, settings.ContractSerializerSettings)
//                                 let envelope = { AllStreamPosition = msg.Position; Message = deserialized; }
//                             })
//                         |> AsyncSeq.fold State.Fold State.Initial
//                     return! loop { Stream = stream; ExpectedVersion = }
//                 }
