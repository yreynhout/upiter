namespace Upiter.Projections
    open System

    type Envelope =
        {
            AllStreamPosition : int64
            Message           : obj
        }