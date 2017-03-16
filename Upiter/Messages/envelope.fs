namespace Upiter.Messages
    open System

    type Envelope =
        {
            AllStreamPosition: int64
            Message: obj
        }