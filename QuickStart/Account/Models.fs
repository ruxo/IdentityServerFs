namespace IdentityServerFs.QuickStart

open System

type LogoutInputModel() =
    member val LogoutId = String.Empty with get, set
    
type LogoutViewModel() =
    inherit LogoutInputModel()
    
    member val ShowLogoutPrompt = true with get, set
    
type LoggedOutViewModel() =
    member val PostLogoutRedirectUri = String.Empty with get, set
    member val ClientName = String.Empty with get, set
    member val SignOutIframeUrl = String.Empty with get, set
    member val AutomaticRedirectAfterSignOut = false with get, set
    member val LogoutId: string option = None with get, set
    member val ExternalAuthenticationScheme: string option = None with get, set
    member my.TriggerExternalSignout = my.ExternalAuthenticationScheme.IsSome

type RedirectViewModel() =
    member val RedirectUrl = String.Empty with get, set
    