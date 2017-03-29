namespace Upiter
    open System
    open Newtonsoft.Json

    module AppProblems = ()
        // [<CLIMutable>]
        // type HttpProblemDetails = { 
        //     [<JsonIgnore>]
        //     StatusCode: int
        //     Type: string 
        //     Title: string
        // }
        // with 
        //     static member MissingContentTypeHeader = { StatusCode=400; Type="MissingContentTypeHeader"; Title="The content type header was not specified." }
        //     static member TooManyContentTypeHeaders = { StatusCode=400; Type="TooManyContentTypeHeaders"; Title="The content type header appears too many times." }
        //     static member ContentTypeHeaderMismatch = { StatusCode=400; Type="ContentTypeHeaderMismatch"; Title="The content type header does not match the expected content type." }
        //     // static member FileExtensionNotSupported = { StatusCode=400; Type="FileExtensionNotSupported"; Title="The file extension specified is not supported." }
        //     // static member FileMimeTypeNotSupported = { StatusCode=400; Type="FileMimeTypeNotSupported"; Title="The file mime type specified is not supported." }
        //     // static member FileSizeNotSupported = { StatusCode=400; Type="FileSizeNotSupported"; Title="The file size is not supported." }
        //     static member JsonDeserializeError = { StatusCode=400; Type="JsonDeserializeError"; Title="There was a problem deserializing the json data you've sent us." }
        //     static member JsonParseError = { StatusCode=400; Type="JsonParseError"; Title="There was a problem parsing the json data you've sent us." }
        //     static member MissingAuthorizationHeader = { StatusCode=401; Type="MissingAuthorizationHeader"; Title="The authorization header was not specified." }
        //     static member AuthorizationHeaderMismatch = { StatusCode=401; Type="AuthorizationHeaderMismatch"; Title="The authorization header must start with 'Bearer '." }
        //     static member BearerTokenMismatch = { StatusCode=401; Type="BearerTokenMismatch"; Title="" }
        //     static member MissingRequiredClaim = { StatusCode=403; Type="MissingRequiredClaim"; Title="" }