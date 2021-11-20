namespace IdentityServerFs.QuickStart

open System
open System.ComponentModel.DataAnnotations

type ExternalProvider() =
    member val DisplayName = String.Empty with get, set
    member val AuthenticationScheme = String.Empty with get, set

type LoginInputModel() =
    [<Required>]
    member val Username = String.Empty with get, set
    [<Required>]
    member val Password = String.Empty with get, set
    member val RememberLogin = true with get, set
    member val ReturnUrl: string = null with get, set
    
type LoginViewModel() =
    inherit LoginInputModel()
    
    member val AllowRememberLogin  = true with get, set
    member val EnableLocalLogin = true with get, set
    
    member val ExternalProviders: ExternalProvider seq = Seq.empty with get, set
    
    member my.VisibleExternalProviders = my.ExternalProviders |> Seq.filter (fun x -> not <| String.IsNullOrEmpty(x.DisplayName))
    member my.IsExternalLoginOnly = not my.EnableLocalLogin && (my.ExternalProviders |> Seq.length) = 1
    member my.ExternalLoginScheme = if my.IsExternalLoginOnly
                                    then my.ExternalProviders |> Seq.tryHead
                                                              |> Option.map (fun p -> p.AuthenticationScheme)
                                                              |> Option.defaultValue null
                                    else null

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
    