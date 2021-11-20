module IdentityServerFs.Config

open IdentityServer4.Models

let IdentityResources: IdentityResource seq = [
    IdentityResources.OpenId()
    IdentityResources.Profile()
]

let ApiScopes: ApiScope list = [
    ApiScope("scope1")
    ApiScope("scope2")
]

let Clients: Client list = [
    Client(ClientId = "m2m.client",
           ClientName = "Client Credentials Client",
           AllowedGrantTypes = GrantTypes.ClientCredentials,
           ClientSecrets = [| Secret(Value = "secret".Sha256()) |],
           AllowedScopes = [| "scope1" |])
    
    Client(ClientId = "interactive",
           ClientSecrets = [| Secret(Value = "secret".Sha256()) |],
           
           AllowedGrantTypes = GrantTypes.Code,
           RedirectUris = [| "https://localhost:44300/signin-oidc" |],
           FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
           PostLogoutRedirectUris = [| "https://localhost:44300/signout-callback-oidc" |],
           
           AllowOfflineAccess = true,
           AllowedScopes = [| "openid"; "scope1"; "scope2" |])
]