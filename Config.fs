module IdentityServerFs.Config

open IdentityServer4
open IdentityServer4.Models

let PartnerScopeName = "partner"
type PartnerScope() as self =
    inherit IdentityResource()
    
    do self.Name <- PartnerScopeName
       self.DisplayName <- "Your partner"
       self.Description <- "Your partner!"
       self.Emphasize <- true
       self.UserClaims <- [| "partner_name"; "marriage_time" |]

let IdentityResources: IdentityResource seq = [
    IdentityResources.OpenId()
    IdentityResources.Profile()
    IdentityResources.Email()
    PartnerScope()
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
           RedirectUris = [| "https://localhost:7295/signin-oidc" |],
           PostLogoutRedirectUris = [| "https://localhost:7295/signout-callback-oidc" |],
           
           AllowOfflineAccess = true,
           AllowedScopes = [| IdentityServerConstants.StandardScopes.OpenId
                              IdentityServerConstants.StandardScopes.Profile
                              IdentityServerConstants.StandardScopes.Email
                              PartnerScopeName
                           |])
]