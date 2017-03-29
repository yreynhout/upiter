namespace Upiter.Model
    open System
    open System.Security.Claims

    type Envelope<'TMessage> =
        {
            RequestId       : Guid
            PlatformMember  : ClaimsPrincipal
            Message         : 'TMessage
        }