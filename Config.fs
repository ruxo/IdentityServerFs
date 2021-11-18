module IdentityServerFs.Config

open IdentityServer4.Models

let IdentityResources: IdentityResource seq = [
    IdentityResources.OpenId()
]

let ApiScopes: ApiScope list = []

let Clients: Client list = []