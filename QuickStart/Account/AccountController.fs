namespace IdentityServerFs.QuickStart

open System.Runtime.InteropServices
open System.Security.Claims
open IdentityModel
open IdentityServer4
open IdentityServer4.Events
open IdentityServer4.Models
open IdentityServer4.Extensions
open IdentityServer4.Services
open IdentityServer4.Stores
open IdentityServer4.Test
open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open System
open TiraxTech.Foundation
open TiraxTech.IdentityServerHelper
open IdentityServerFs

[<SecurityHeaders>]
[<AllowAnonymous>]
type AccountController(interaction: IIdentityServerInteractionService,
                       clientStore: IClientStore,
                       schemeProvider: IAuthenticationSchemeProvider,
                       events: IEventService,
                       [<Optional; DefaultParameterValue(null:TestUserStore)>] users: TestUserStore) =
    inherit Controller()
    
    let users = if users = null then TestUserStore(TestUsers.defaultUser()) else users
    
    let getUsableIdp (context: AuthorizationRequest) =
        task {
            let idp = context |> AuthorizationRequest.getIdp
            let! scheme = idp |> Option.bindTask schemeProvider.getSchemeAsync
            return scheme |> Option.bind (constant idp)
        }
        
    let viewModelFromIdp returnUrl loginHint idp =
        let local = idp = IdentityServerConstants.LocalIdentityProvider
        LoginViewModel(
            EnableLocalLogin = local,
            ReturnUrl = returnUrl,
            Username = loginHint,
            ExternalProviders = if local
                                then seq { ExternalProvider(AuthenticationScheme = idp) }
                                else Seq.empty
        )
    
    let buildLoginViewModelAsync(context: AuthorizationRequest option, returnUrl) =
        task {
            let! idp = context |> Option.bindTask getUsableIdp
            let loginHint = context |> Option.map (fun ctx -> ctx.LoginHint) |> Option.getOrDefault null
            let vmFromIdp = idp |> Option.map (viewModelFromIdp returnUrl loginHint)
            
            if vmFromIdp.IsSome then
                return vmFromIdp.Value
            else
                let! schemes = schemeProvider.GetAllSchemesAsync()
                
                // All IdentityServer providers have DisplayName = null, basically this we select only external providers.
                let providers = query {
                    for x in schemes do
                    where (x.DisplayName <> null)
                    select (ExternalProvider(DisplayName = x.DisplayName, AuthenticationScheme=x.Name))
                }
                
                let clientId = context.map (fun ctx -> ctx.Client.ClientId)
                let! client = clientId.bindTask clientStore.findEnabledClientByIdAsync
                let allowLocal = client.map (fun c -> c.EnableLocalLogin)
                let restrictions = client.map (fun c -> c.IdentityProviderRestrictions)
                let anyRestrictions = restrictions.map (not << Seq.isEmpty) |> Option.defaultValue false
                let finalProviders = (if anyRestrictions
                                      then providers |> Seq.filter (fun p -> restrictions.Value.Contains(p.AuthenticationScheme))
                                      else providers)
                                     |> Seq.toArray
                
                return LoginViewModel(
                    AllowRememberLogin = AccountOptions.AllowRememberLogin,
                    EnableLocalLogin = (AccountOptions.AllowLocalLogin && (allowLocal |> Option.defaultValue true)),
                    ReturnUrl = returnUrl,
                    Username = loginHint,
                    ExternalProviders = finalProviders
                )
        }
        
    let buildLoginViewModelFromInputModelAsync(context, model: LoginInputModel) = task {
        let! vm = buildLoginViewModelAsync(context, model.ReturnUrl)
        vm.Username <- model.Username
        vm.RememberLogin <- model.RememberLogin
        return vm
    }
    
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
        
    static let loadingPage(viewName, redirectUri) (controller: Controller) =
        controller.HttpContext.Response.StatusCode <- 200
        controller.HttpContext.Response.Headers["Location"] <- ""
        
        controller.View(viewName, RedirectViewModel(RedirectUrl = redirectUri))
    
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
        
    member private my.RedirectToUrl(context: AuthorizationRequest option, returnUrl) =
        let isNativeClient = context.map(AuthorizationRequest.isNativeClient).getOrDefault(false)
        if context.IsSome then
            if isNativeClient
            then (my |> loadingPage("Redirect", returnUrl)) :> IActionResult
            else my.Redirect(returnUrl)
        elif my.Url.IsLocalUrl(returnUrl) then
            my.Redirect(returnUrl)
        elif String.IsNullOrEmpty(returnUrl) then
            my.Redirect("~/")
        else
            raise <| Exception $"Invalid return URL {returnUrl}"
        
    [<HttpGet>]
    member my.Login(returnUrl) = task {
        let! context = interaction.getAuthorizationContextAsync(returnUrl)
        if my.User.IsAuthenticated() then
            return my.RedirectToUrl(context, returnUrl)
        else
            let! vm = buildLoginViewModelAsync(context, returnUrl)
            if vm.IsExternalLoginOnly
            then return my.RedirectToAction("Challenge", "External", {| provider = vm.ExternalLoginScheme |}) :> IActionResult
            else return my.View(vm)
    }
        
    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member my.Login(model: LoginInputModel, button: string) = task {
        let viewFromModel(context) = task {
            let! vm = buildLoginViewModelFromInputModelAsync(context, model)
            return my.View(vm) :> IActionResult
        }
        
        let! context = interaction.getAuthorizationContextAsync(model.ReturnUrl)
        let clientId = context.map(fun ctx -> ctx.Client.ClientId).getOrDefault(null)
        
        if button <> "login" then
            match context with
            | Some ctx -> do! interaction.DenyAuthorizationAsync(ctx, AuthorizationError.AccessDenied)
                          if ctx.isNativeClient()
                          then return my |> loadingPage ("Redirect", model.ReturnUrl) :> IActionResult
                          else return my.Redirect(model.ReturnUrl)
            | None -> return my.Redirect("~/")
            
        elif my.ModelState.IsValid then
            if users.ValidateCredentials(model.Username, model.Password) then
                let user = users.FindByUsername(model.Username)
                do! events.RaiseAsync <| UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username, clientId = clientId)
                
                let shouldRememberLogin = AccountOptions.AllowRememberLogin && model.RememberLogin
                let props = AuthenticationProperties(
                             IsPersistent = shouldRememberLogin,
                             ExpiresUtc = if shouldRememberLogin
                                          then DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration) |> Nullable
                                          else Nullable()
                            )
                let isuser = IdentityServerUser(user.SubjectId, DisplayName = user.Username)
                do! my.HttpContext.SignInAsync(isuser, props)
                
                return my.RedirectToUrl(context, model.ReturnUrl)
            else
                do! events.RaiseAsync <| UserLoginFailureEvent(model.Username, "invalid credentials", clientId=clientId)
                my.ModelState.AddModelError(String.Empty, AccountOptions.InvalidCredentialsErrorMessage)
                return! viewFromModel(context)
                
        else  
            return! viewFromModel(context)
    }