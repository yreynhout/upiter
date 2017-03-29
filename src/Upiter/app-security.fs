namespace Upiter

    open System
    open System.Security.Claims
    open System.Text
    open Microsoft.IdentityModel.Tokens
    open System.IdentityModel.Tokens.Jwt

    open Newtonsoft.Json

    open Suave
    open Suave.Headers
    open Suave.Operators
    open Suave.RequestErrors
    open Suave.Successful
    open Suave.Writers

    module AppSecurity =
        type JwtBearerAuthenticationOptions = {
            Audience: string
            IssuerSecret: string
            Issuer: string
        }

        [<Literal>]
        let PlatformMemberKey = "PlatformMember"

        let private verifyAuthorizationToken token authenticationOptions : Result<ClaimsPrincipal, HttpProblemDetails> =
            let toSecurityKey (secret: string) =
                //Secrets are now UTF-8 instead of Base64 over at Auth0.com
                SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)) :> SecurityKey

            let composeTokenValidationParameters =
                let parameters = TokenValidationParameters()
                //Validation parameterization
                parameters.ValidAudience <- authenticationOptions.Audience
                parameters.ValidIssuer <- authenticationOptions.Issuer
                parameters.IssuerSigningKey <- toSecurityKey(authenticationOptions.IssuerSecret)
                //What to validate
                parameters.ValidateLifetime <- true
                parameters.ValidateIssuerSigningKey <- true
                parameters.ValidateAudience <- true
                parameters.ValidateIssuer <- true
                //Return
                parameters
                
            try
                let handler = JwtSecurityTokenHandler()
                let principal =
                    handler.ValidateToken(token, composeTokenValidationParameters, ref null)
                principal |> Ok
            with
                | ex -> Error { HttpProblemDetails.BearerTokenNotValid with Details = Some(ex.Message) }

        let authorize (authenticationOptions: JwtBearerAuthenticationOptions) (httpJsonSettings: JsonSerializerSettings) (next: WebPart) : WebPart =
             fun (context : HttpContext) -> async {
                 let authorizationHeaders = getHeader "Authorization" context |> Seq.toArray
                 let continuation =
                    if Array.length authorizationHeaders <> 1 then
                        (setMimeType "application/problem+json"
                        >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + authenticationOptions.Issuer + "\", scope=\"openid profile\"")
                        >=> UNAUTHORIZED (JsonConvert.SerializeObject(HttpProblemDetails.MissingOrTooManyAuthorizationHeaders, httpJsonSettings))) 
                    else
                        let authorizationHeader = Array.exactlyOne authorizationHeaders
                        if not(authorizationHeader.StartsWith("Bearer ")) then
                            (setMimeType "application/problem+json"
                            >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + authenticationOptions.Issuer + "\", scope=\"openid profile\"")
                            >=> UNAUTHORIZED (JsonConvert.SerializeObject(HttpProblemDetails.AuthorizationHeaderMismatch, httpJsonSettings))) 
                        else 
                            match verifyAuthorizationToken (authorizationHeader.Substring(7)) authenticationOptions with
                            | Ok principal ->
                                setUserData PlatformMemberKey principal >=> next
                            | Error problem ->
                                (setMimeType "application/problem+json"
                                >=> setHeader "WWW-Authenticate" ("Bearer realm=\"" + authenticationOptions.Issuer + "\", scope=\"openid profile\"")
                                >=> UNAUTHORIZED (JsonConvert.SerializeObject(problem, httpJsonSettings))) 
                return! continuation context
             }