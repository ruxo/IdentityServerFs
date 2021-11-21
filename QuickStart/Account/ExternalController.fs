namespace IdentityServerFs.QuickStart

open System.Collections.Generic
open System.Runtime.InteropServices
open System.Security.Claims
open IdentityModel
open System
open IdentityServer4
open IdentityServer4.Events
open IdentityServer4.Services
open IdentityServer4.Test
open IdentityServerFs
open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open IdentityServerFs.Helpers
open TiraxTech.IdentityServerHelper
open TiraxTech.Foundation

[<SecurityHeaders>]
[<AllowAnonymous>]
type ExternalController(interaction: IIdentityServerInteractionService,
                        events: IEventService,
                        logger: ILogger<ExternalController>,
                        [<Optional; DefaultParameterValue(null:TestUserStore)>] users: TestUserStore) =
    inherit Controller()

    let users = if users = null then TestUserStore(TestUsers.defaultUser()) else users
    
    let findUserFromExternalProvider(result: AuthenticateResult) =
        let externalUser = result.Principal
        let userIdClaim = externalUser.findFirst(JwtClaimTypes.Subject)
                          |> Option.orElse (externalUser.findFirst(ClaimTypes.NameIdentifier))
                          |> Option.orElseWith (fun _ -> failwith "Unknown userid")
                          |> Option.get
        let claims = List(externalUser.Claims |> Seq.filter (fun c -> c <> userIdClaim))
        
        let provider = result.Properties.Items["scheme"]
        let providerUserId = userIdClaim.Value
        
        let user = users.FindByExternalProvider(provider, providerUserId) |> Option.ofObj
        (user, provider, providerUserId, claims)
        
    let processLoginCallback(externalResult: AuthenticateResult, localClaims: List<Claim>, localSignInProps: AuthenticationProperties) =
        let sid = externalResult.Principal.Claims |> Seq.tryFind (fun x -> x.Type = JwtClaimTypes.SessionId)
        if sid.IsSome then
            localClaims.Add(Claim(JwtClaimTypes.SessionId, sid.Value.Value))
        
        let idToken = externalResult.Properties.GetTokenValue("id_token") |> Option.ofObj
        if idToken.IsSome then
            localSignInProps.StoreTokens(seq { AuthenticationToken(Name = "id_token", Value = idToken.Value)  })
    
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
                
            let user, provider, providerUserId, claims = findUserFromExternalProvider(result)
            // this might be where you might initiate a custom workflow for user registration
            // in this sample we don't show how that would be done, as our sample implementation
            // simply auto-provisions new external user
            let user = user |> Option.defaultWith (fun _ -> users.AutoProvisionUser(provider, providerUserId, claims))
            
            // this allows us to collect any additional claims or properties
            // for the specific protocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            let additionalLocalClaims = List<Claim>()
            let localSignInProps = AuthenticationProperties()
            processLoginCallback(result, additionalLocalClaims, localSignInProps)
            
            let isuser = IdentityServerUser(user.SubjectId,
                                            DisplayName = user.Username,
                                            IdentityProvider = provider,
                                            AdditionalClaims = additionalLocalClaims)
            do! my.HttpContext.SignInAsync(isuser, localSignInProps)
            
            // delete temporary cookie used during external authentication
            do! my.HttpContext.SignOutAsync(IdentityServerConstants.ExternalCookieAuthenticationScheme)
            
            let returnUrl = result.Properties.Items["returnUrl"] %? "~/"
            
            let! context = interaction.getAuthorizationContextAsync(returnUrl)
            do! events.RaiseAsync <| UserLoginSuccessEvent(provider, providerUserId, user.SubjectId, user.Username, true,
                                                           context.map(fun c -> c.Client.ClientId).getOrDefault(null))
            
            if context |> Option.map AuthorizationRequest.isNativeClient |> Option.defaultValue false then
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return my |> AccountHelper.loadingPage("Redirect", returnUrl) :> IActionResult
            else
                return my.Redirect(returnUrl)
        }