module TiraxTech.IdentityServerHelper

open System
open IdentityServer4.Models
open IdentityServer4.Services
open IdentityServer4.Stores
open Microsoft.AspNetCore.Authentication

let getErrorContextAsync errorId (interaction: IIdentityServerInteractionService) =
    task {
        let! result = interaction.GetErrorContextAsync(errorId)
        return Option.ofObj(result)
    }
    
let getAuthorizationContextAsync returnUrl (interaction: IIdentityServerInteractionService) =
    task {
        let! result = interaction.GetAuthorizationContextAsync(returnUrl)
        return Option.ofObj(result)
    }
    
type IIdentityServerInteractionService with
    member inline my.getAuthorizationContextAsync returnUrl = getAuthorizationContextAsync returnUrl my
    member inline my.getErrorContextAsync errorId = getErrorContextAsync errorId my
    
type IAuthenticationSchemeProvider with
    member inline my.getSchemeAsync idp =
        task {
            let! result = my.GetSchemeAsync idp
            return Option.ofObj(result)
        }

type IClientStore with
    member inline my.findEnabledClientByIdAsync clientId =
        task {
            let! result = my.FindEnabledClientByIdAsync clientId
            return Option.ofObj result
        }
        
type AuthorizationRequest with
    member inline my.isNativeClient() = IdentityServerFs.AuthorizationRequest.isNativeClient(my)
