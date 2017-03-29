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
    //     static member MissingContentTypeHeader = { StatusCode=400; Type="MissingContentTypeHeader"; Title="The content type header was not specified." }
    //     static member TooManyContentTypeHeaders = { StatusCode=400; Type="TooManyContentTypeHeaders"; Title="The content type header appears too many times." }
    //     static member ContentTypeHeaderMismatch = { StatusCode=400; Type="ContentTypeHeaderMismatch"; Title="The content type header does not match the expected content type." }
    //     // static member FileExtensionNotSupported = { StatusCode=400; Type="FileExtensionNotSupported"; Title="The file extension specified is not supported." }
    //     // static member FileMimeTypeNotSupported = { StatusCode=400; Type="FileMimeTypeNotSupported"; Title="The file mime type specified is not supported." }
    //     // static member FileSizeNotSupported = { StatusCode=400; Type="FileSizeNotSupported"; Title="The file size is not supported." }
    //     static member JsonDeserializeError = { StatusCode=400; Type="JsonDeserializeError"; Title="There was a problem deserializing the json data you've sent us." }
    //     static member JsonParseError = { StatusCode=400; Type="JsonParseError"; Title="There was a problem parsing the json data you've sent us." }
    
    //     static member BearerTokenMismatch = { StatusCode=401; Type="BearerTokenMismatch"; Title="" }
    //     static member MissingRequiredClaim = { StatusCode=403; Type="MissingRequiredClaim"; Title="" }