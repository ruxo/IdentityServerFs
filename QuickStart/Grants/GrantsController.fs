namespace IdentityServerFs.QuickStart

open System.Collections.Generic
open FSharp.Control
open IdentityServer4.Events
open IdentityServer4.Services
open IdentityServer4.Stores
open Microsoft.AspNetCore.Mvc
open IdentityServer4.Extensions
open TiraxTech.Foundation

module private Helper =
    let inline (%?) (lhr: string) (rhs: string) = if lhr = null then rhs else lhr
open Helper

type GrantsController(interaction: IIdentityServerInteractionService,
                      clients: IClientStore,
                      resourceStore: IResourceStore,
                      events: IEventService) =
    inherit Controller()
    
    let buildViewModelAsync() =
        task {
            let! grants = interaction.GetAllUserGrantsAsync()
            let! list =
                asyncSeq {
                    for grant in grants do
                        let! client = clients.FindClientByIdAsync(grant.ClientId) |> Async.AwaitTask
                        if client <> null then
                            let! resources = resourceStore.FindResourcesByScopeAsync(grant.Scopes) |> Async.AwaitTask
                            yield { ClientId = client.ClientId
                                    ClientName = client.ClientName %? client.ClientId
                                    ClientLogoUrl = Some client.LogoUri
                                    ClientUrl = client.ClientUri
                                    Description = Some grant.Description
                                    Created = grant.CreationTime
                                    Expires = Option.ofNullable grant.Expiration
                                    IdentityGrantNames = resources.IdentityResources |> Seq.map (fun x -> x.DisplayName %? x.Name)
                                    ApiGrantNames = resources.ApiScopes |> Seq.map (fun x -> x.DisplayName %? x.Name)
                                  }
                } |> AsyncSeq.toListAsync
            return { Grants = list }
        }
        
    member my.Index() = task {
        let! vm = buildViewModelAsync()
        return my.View("Index", vm)
    }

    [<HttpPost>]
    [<ValidateAntiForgeryToken>]
    member my.Revoke(clientId) =
        task {
            do! interaction.RevokeUserConsentAsync(clientId)
            do! events.RaiseAsync <| GrantsRevokedEvent(my.User.GetSubjectId(), clientId)
            return my.RedirectToAction("Index")
        }