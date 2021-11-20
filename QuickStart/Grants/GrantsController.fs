namespace IdentityServerFs.QuickStart

open System.Collections.Generic
open IdentityServer4.Events
open IdentityServer4.Services
open IdentityServer4.Stores
open Microsoft.AspNetCore.Mvc
open IdentityServer4.Extensions

type GrantsController(interaction: IIdentityServerInteractionService,
                      clients: IClientStore,
                      resourceStore: IResourceStore,
                      events: IEventService) =
    inherit Controller()
    
    let buildViewModelAsync() =
        task {
            let! grants = interaction.GetAllUserGrantsAsync()
            
            let list = List<GrantViewModel>()
            for grant in grants do
                let! client = clients.FindClientByIdAsync(grant.ClientId)
                if client <> null then
                    let! resources = resourceStore.FindResourcesByScopeAsync(grant.Scopes)
                    list.Add(GrantViewModel(ClientId = client.ClientId,
                                            ClientName = client.ClientName))
            return ()
        }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member my.Revoke(clientId) =
        task {
            do! interaction.RevokeUserConsentAsync(clientId)
            do! events.RaiseAsync <| GrantsRevokedEvent(my.User.GetSubjectId(), clientId)
            return my.RedirectToAction("Index")
        }