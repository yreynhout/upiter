namespace Upiter

    open System
    open System.Security.Claims
    open Microsoft.IdentityModel.Tokens
    open System.IdentityModel.Tokens.Jwt

    open Newtonsoft.Json

    open Suave
    open Suave.Headers
    open Suave.Operators
    open Suave.RequestErrors
    open Suave.Successful
    open Suave.Writers

    open Upiter.AppProblems
    
    module AppSecurity = ()
        // type JwtBearerAuthenticationOptions = {
        //     Audience: string
        //     IssuerSecret: string
        //     Issuer: string
        // }

        // let verifyAuthorizationToken token options : Choice<ClaimsPrincipal, HttpProblemDetails> =
        //     let toSecurityKey secret =
        //         let decodeBase64 (secret: string) =
        //             let pad text =
        //                 let padding = 3 - ((String.length text + 3) % 4)
        //                 if padding = 0 then text else (text + String('=', padding))
        //             Convert.FromBase64String(pad(secret.Replace('-', '+').Replace('_', '/')))

        //         SymmetricSecurityKey(decodeBase64(secret)) :> SecurityKey

        //     let composeTokenValidationParameters =
        //         let parameters = TokenValidationParameters()
        //         //Validation parameterization
        //         parameters.ValidAudience <- options.Audience
        //         parameters.ValidIssuer <- options.Issuer
        //         parameters.IssuerSigningKey <- toSecurityKey(options.IssuerSecret)
        //         //What to validate
        //         parameters.ValidateLifetime <- true
        //         parameters.ValidateIssuerSigningKey <- true
        //         parameters.ValidateAudience <- true
        //         parameters.ValidateIssuer <- true
                
        //         parameters
                
        //     try
        //         let handler = JwtSecurityTokenHandler()
        //         let principal =
        //             handler.ValidateToken(token, composeTokenValidationParameters, ref null)
        //         principal |> Choice1Of2
        //     with
        //         | ex -> Choice2Of2 { HttpProblemDetails.BearerTokenMismatch with Title = ex.Message }

        // let authorize options (settings: JsonSerializerSettings) next : WebPart =
        //     fun (context : HttpContext) -> async {
        //         let authorizationHeader = getHeader "Authorization" context
        //         return! 
        //             match (authorizationHeader |> Seq.length) with
        //             | 0 -> 
        //                 (UNAUTHORIZED (JsonConvert.SerializeObject(HttpProblemDetails.MissingAuthorizationHeader, settings)) 
        //                 >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + options.Issuer + "\", scope=\"openid profile\"")) context
        //             | _ ->
        //                 match (authorizationHeader |> Seq.exactlyOne |> fun item -> item.StartsWith("Bearer ")) with
        //                 | false -> (UNAUTHORIZED (HttpProblemDetails.AuthorizationHeaderMismatch |> Json.serialize |> Json.format) >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + options.Issuer + "\", scope=\"openid profile\"")) context
        //                 | true -> 
        //                     match (authorizationHeader |> Seq.exactlyOne |> fun item -> verifyAuthorizationToken (item.Substring(7)) options) with
        //                     | Choice1Of2 principal ->
        //                         next { context with userState = context.userState.Add("vsl.RequestUser", principal) }
        //                     | Choice2Of2 error -> 
        //                         match error.StatusCode with
        //                         | 400 -> BAD_REQUEST (error |> Json.serialize |> Json.format) context
        //                         | 401 -> (UNAUTHORIZED (error |> Json.serialize |> Json.format) >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + options.Issuer + "\", scope=\"openid profile\"")) context  
        //                         | 403 -> FORBIDDEN (error |> Json.serialize |> Json.format) context
        //                         | _ -> BAD_REQUEST (error |> Json.serialize |> Json.format) context
        //     }