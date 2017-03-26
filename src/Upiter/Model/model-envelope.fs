namespace Upiter.Model
    open System

    type Envelope<'TMessage> =
        {
            RequestId : Guid
            Message   : 'TMessage
        }