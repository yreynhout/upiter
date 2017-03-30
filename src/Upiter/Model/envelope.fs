namespace Upiter.Model
    open System
    open System.Security.Claims
    open Upiter.Security

    type Envelope<'TCommand> =
        {
            CommandId   : Guid
            Visitor     : PlatformVisitor
            Command     : 'TCommand
        }