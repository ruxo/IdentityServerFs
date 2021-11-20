namespace IdentityServerFs.QuickStart

open System.Security.Claims
open IdentityModel
open System
open IdentityServer4
open IdentityServer4.Events
open IdentityServer4.Extensions
open IdentityServer4.Services
open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc

[<SecurityHeaders>]
[<AllowAnonymous>]
type AccountController(interaction: IIdentityServerInteractionService,
                       events: IEventService) =
    inherit Controller()
    
    let buildLogoutViewModelAsync(user: ClaimsPrincipal, logoutId) =
        task {
            let vm = LogoutViewModel(LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt)
            
            if not user.Identity.IsAuthenticated then
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt <- false
            else
                let! context = interaction.GetLogoutContextAsync(logoutId)
                if not context.ShowSignoutPrompt then
                    // it's safe to automatically sign-out
                    vm.ShowLogoutPrompt <- false
            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm
        }
        
    let buildLoggedOutViewModelAsync(httpContext: HttpContext, user: ClaimsPrincipal, logoutId) =
        task {
            let! logout = interaction.GetLogoutContextAsync(logoutId)
            let vm = LoggedOutViewModel(AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                                        PostLogoutRedirectUri = logout.PostLogoutRedirectUri,
                                        ClientName = (if String.IsNullOrEmpty(logout.ClientName) then logout.ClientId else logout.ClientName),
                                        SignOutIframeUrl = logout.SignOutIFrameUrl,
                                        LogoutId = Some logoutId)
            if user.Identity.IsAuthenticated then
                let idp = user.FindFirst(JwtClaimTypes.IdentityProvider) |> Option.ofObj |> Option.map (fun i -> i.Value)
                if idp.IsSome && idp <> (Some IdentityServerConstants.LocalIdentityProvider) then
                    let! providerSupportsSignout = httpContext.GetSchemeSupportsSignOutAsync(idp.Value)
                    if providerSupportsSignout then
                        if vm.LogoutId.IsNone then
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            let! lid = interaction.CreateLogoutContextAsync()
                            vm.LogoutId <- Some lid
                        vm.ExternalAuthenticationScheme <- idp
            return vm
        }
        
    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member my.Logout(model: LogoutInputModel) =
        task {
            let! vm = buildLoggedOutViewModelAsync(my.HttpContext, my.User, model.LogoutId)
            
            if my.User.Identity.IsAuthenticated then
                do! my.HttpContext.SignOutAsync()
                do! events.RaiseAsync <| UserLogoutSuccessEvent(my.User.GetSubjectId(), my.User.GetDisplayName())
                
            if vm.TriggerExternalSignout then
                let url = my.Url.Action("Logout", {| logoutId = vm.LogoutId |})
                return my.SignOut(AuthenticationProperties(RedirectUri = url), vm.ExternalAuthenticationScheme |> Option.defaultValue null) :> IActionResult
            else
                return my.View("LoggedOut", vm)
        }

    [<HttpGet>]
    member my.Logout(logoutId) =
        task {
            let! vm = buildLogoutViewModelAsync(my.User, logoutId)
            
            if not vm.ShowLogoutPrompt
            then return! my.Logout(vm)
            else return my.View(vm)
        }