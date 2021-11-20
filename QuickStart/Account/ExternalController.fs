namespace IdentityServerFs.QuickStart

open System
open System.Runtime.InteropServices
open IdentityServer4
open IdentityServer4.Services
open IdentityServer4.Stores
open IdentityServer4.Test
open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<SecurityHeaders>]
[<AllowAnonymous>]
type ExternalController(interaction: IIdentityServerInteractionService,
                        clientStore: IClientStore,
                        events: IEventService,
                        logger: ILogger<ExternalController>,
                        [<Optional; DefaultParameterValue(null:TestUserStore)>] users: TestUserStore) =
    inherit Controller()

    let users = if users = null then TestUserStore(TestUsers.defaultUser()) else users
    
    member my.Challenge(scheme: string, returnUrl: string) =
        let returnUrl = if String.IsNullOrEmpty(returnUrl) then "~/" else returnUrl
        
        if (not <| my.Url.IsLocalUrl(returnUrl)) && (not <| interaction.IsValidReturnUrl(returnUrl)) then
            failwith "Invalid return URL"
        
        let props = AuthenticationProperties(RedirectUri = my.Url.Action(nameof(my.Callback)))
        props.Items["returnUrl"] <- returnUrl
        props.Items["scheme"] <- scheme
        my.Challenge(props, scheme)
        
    member my.Callback() =
        task {
            let! result = my.HttpContext.AuthenticateAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme)
            if not result.Succeeded then failwith "External authentication error"
            
            if logger.IsEnabled(LogLevel.Debug) then
                let externalClaims = seq { for c in result.Principal.Claims -> $"{c.Type}: {c.Value}" }
                logger.LogDebug("External claims: {@claims}", externalClaims)
                
            return raise <| NotImplementedException()
        }