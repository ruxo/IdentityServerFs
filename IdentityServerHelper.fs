module TiraxTech.IdentityServerHelper

open IdentityServer4.Services

let getErrorContextAsync errorId (interaction: IIdentityServerInteractionService) =
    task {
        let! result = interaction.GetErrorContextAsync(errorId)
        return Option.ofObj(result)
    }
    
type IIdentityServerInteractionService with
    member my.getErrorContextAsync errorId = getErrorContextAsync errorId my