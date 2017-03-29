namespace Upiter
    open System
    open Newtonsoft.Json

    [<CLIMutable>]
    type HttpProblemDetails = { 
        Status: int
        Type: Uri
        Title: string
        Details: String option
        Instance: Uri option
    }
    with
        static member MissingOrTooManyAuthorizationHeaders = 
            { 
                Status   = 401
                Type     = new Uri("https://tools.ietf.org/html/rfc7235#section-4.2")
                Title    = "The authorization header was not specified or too many were specified."
                Details  = None
                Instance = None
            }
        static member AuthorizationHeaderMismatch = 
            { 
                Status   = 401
                Type     = new Uri("https://tools.ietf.org/html/rfc6750#section-2.1")
                Title    = "The authorization header must start with 'Bearer '."
                Details  = None
                Instance = None
            }
        static member BearerTokenNotValid = 
            {
                Status   = 401
                Type     = new Uri("https://tools.ietf.org/html/rfc7519#section-7.2")
                Title    = "The authorization header's bearer json web token is not valid."
                Details  = None
                Instance = None
            }
        static member JsonParseError = 
            { 
                Status   = 400
                Type     = new Uri("https://tools.ietf.org/html/rfc7231#section-6.5.1")
                Title    = "There was a problem parsing the provided json data."
                Details  = None
                Instance = None
            }
        static member NotAuthorizedError = 
            { 
                Status   = 403
                Type     = new Uri("https://tools.ietf.org/html/rfc7231#section-6.5.3")
                Title    = "The caller is not authorized to perform the request."
                Details  = None
                Instance = None
            }