namespace Upiter
    open System
    open Upiter.Messages
    open Upiter.Messages.GroupContracts

    open NodaTime

    open SqlStreamStore
    open SqlStreamStore.Streams

    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    
    module Seeding =
        let data = JArray.Parse("""[
        {
            "GroupAdministratorOrganization": "Lobortis Industries",
            "GroupAdministratorFullName": "Abraham Barrett",
            "Name": "Pellentesque habitant morbi tristique senectus et netus et malesuada",
            "Purpose": "molestie orci tincidunt adipiscing. Mauris molestie pharetra nibh. Aliquam ornare, libero at auctor ullamcorper, nisl arcu iaculis enim, sit amet ornare lectus justo eu arcu. Morbi sit amet massa. Quisque porttitor"
        },
        {
            "GroupAdministratorOrganization": "Eu Elit Limited",
            "GroupAdministratorFullName": "Gil Luna",
            "Name": "augue ut lacus. Nulla tincidunt, neque vitae semper",
            "Purpose": "Mauris magna. Duis dignissim tempor arcu. Vestibulum ut eros non enim commodo hendrerit. Donec porttitor tellus non magna. Nam ligula elit, pretium et, rutrum non, hendrerit id, ante. Nunc mauris sapien, cursus in, hendrerit consectetuer, cursus et, magna. Praesent interdum ligula eu enim. Etiam imperdiet dictum magna. Ut tincidunt"
        },
        {
            "GroupAdministratorOrganization": "Semper Et Lacinia Company",
            "GroupAdministratorFullName": "Gil Ellis",
            "Name": "nec, euismod in, dolor. Fusce feugiat. Lorem ipsum dolor",
            "Purpose": "nibh vulputate mauris sagittis placerat. Cras dictum ultricies ligula. Nullam enim. Sed nulla"
        },
        {
            "GroupAdministratorOrganization": "Sodales Nisi Magna PC",
            "GroupAdministratorFullName": "Ina Wallace",
            "Name": "eu erat semper rutrum.",
            "Purpose": "ornare. In faucibus. Morbi vehicula. Pellentesque tincidunt tempus risus. Donec egestas. Duis ac arcu. Nunc mauris. Morbi non sapien molestie orci"
        },
        {
            "GroupAdministratorOrganization": "Ipsum Corp.",
            "GroupAdministratorFullName": "Prescott Cash",
            "Name": "nulla. In tincidunt congue turpis.",
            "Purpose": "commodo"
        },
        {
            "GroupAdministratorOrganization": "Nulla Facilisi Sed Corporation",
            "GroupAdministratorFullName": "Dacey Le",
            "Name": "Nullam enim. Sed nulla",
            "Purpose": "mi. Aliquam gravida mauris ut mi. Duis risus odio, auctor vitae, aliquet nec, imperdiet nec, leo. Morbi neque tellus,"
        },
        {
            "GroupAdministratorOrganization": "Fermentum Metus Aenean Industries",
            "GroupAdministratorFullName": "Rosalyn Castaneda",
            "Name": "iaculis nec, eleifend",
            "Purpose": "adipiscing elit. Aliquam auctor, velit eget laoreet posuere, enim nisl elementum purus, accumsan interdum"
        },
        {
            "GroupAdministratorOrganization": "Viverra Donec Tempus Corporation",
            "GroupAdministratorFullName": "Mechelle Barnes",
            "Name": "luctus, ipsum leo elementum sem, vitae",
            "Purpose": "ac mattis velit justo nec ante. Maecenas mi felis, adipiscing fringilla, porttitor vulputate, posuere vulputate, lacus. Cras interdum. Nunc sollicitudin commodo ipsum. Suspendisse non leo. Vivamus nibh dolor, nonummy ac, feugiat non, lobortis quis, pede. Suspendisse dui. Fusce diam nunc, ullamcorper eu, euismod ac, fermentum vel, mauris. Integer sem elit,"
        },
        {
            "GroupAdministratorOrganization": "Curabitur Limited",
            "GroupAdministratorFullName": "Rose Burt",
            "Name": "non, feugiat nec, diam. Duis mi enim, condimentum eget, volutpat",
            "Purpose": "lorem eu metus. In lorem. Donec elementum, lorem ut aliquam iaculis, lacus pede sagittis augue, eu tempor erat neque non quam. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Aliquam fringilla cursus purus. Nullam"
        },
        {
            "GroupAdministratorOrganization": "Lectus Justo Foundation",
            "GroupAdministratorFullName": "Vivien Meyer",
            "Name": "Fusce fermentum fermentum arcu. Vestibulum ante",
            "Purpose": "dui, semper et, lacinia vitae, sodales at, velit. Pellentesque ultricies dignissim lacus. Aliquam rutrum lorem ac risus. Morbi metus. Vivamus euismod urna. Nullam lobortis quam a felis ullamcorper viverra. Maecenas iaculis aliquet diam. Sed diam lorem, auctor"
        }
    ]""")
        let seed (store: IStreamStore) (storeJsonSettings: JsonSerializerSettings) (clock: IClock) = async {
            //TODO: Appending should happen with the group's append method.
            for index = 0 to 1000 do
                let groupData = data.Item(index % 9)
                let groupId = Guid.NewGuid()
                if index % 2 = 0 then
                    let message : PrivateGroupWasStarted =
                        {
                            GroupId = groupId
                            Name = groupData.Item("Name").ToString()
                            Purpose = groupData.Item("Purpose").ToString()
                            PlatformMemberId = Guid.NewGuid()
                            TenantId = index % 3 
                            When = clock.Now.Ticks
                        }
                    store.AppendToStream(
                            groupId.ToString("N"),
                            ExpectedVersion.NoStream,
                            NewStreamMessage(
                                groupId,
                                "PrivateGroupStarted",
                                JsonConvert.SerializeObject(message),
                                null)) 
                        |> Async.AwaitTask
                        |> ignore
                else
                    let message : PublicGroupWasStarted =
                        {
                            GroupId = groupId
                            Name = groupData.Item("Name").ToString()
                            Purpose = groupData.Item("Purpose").ToString()
                            PlatformMemberId = Guid.NewGuid()
                            TenantId = index % 5 
                            When = clock.Now.Ticks
                        }
                    store.AppendToStream(
                            groupId.ToString("N"),
                            ExpectedVersion.NoStream,
                            NewStreamMessage(
                                groupId,
                                "PublicGroupStarted",
                                JsonConvert.SerializeObject(message),
                                null)) 
                        |> Async.AwaitTask
                        |> ignore

        }
