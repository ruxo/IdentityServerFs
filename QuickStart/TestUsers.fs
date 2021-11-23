module IdentityServerFs.QuickStart.TestUsers

open System.Collections.Generic
open System.Security.Claims
open System.Text.Json
open IdentityModel
open IdentityServer4
open IdentityServer4.Test

let private createTestUser(subjectId, user, password, claims) =
    let user = TestUser(SubjectId=subjectId, Username=user, Password=password)
    claims |> Seq.iter user.Claims.Add
    user

let private address = {|
    street_address = "One Hacker Way"
    locality = "Heidelberg"
    postal_code = 69118
    country = "Germany"
|}
    
let defaultUser() =
    List([
        createTestUser("818727", "alice", "alice", [
            Claim(JwtClaimTypes.Name, "Alice Smith")
            Claim(JwtClaimTypes.GivenName, "Alice")
            Claim(JwtClaimTypes.FamilyName, "Smith")
            Claim(JwtClaimTypes.Email, "AliceSmith@email.com")
            Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
            Claim(JwtClaimTypes.WebSite, "http://alice.com")
            Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
            Claim("partner_name", "celsi")
            Claim("marriage_time", "1900-01-01T00:00:00Z", ClaimValueTypes.DateTime)
        ])
        createTestUser("88421113", "bob", "bob", [
            Claim(JwtClaimTypes.Name, "Bob Smith")
            Claim(JwtClaimTypes.GivenName, "Bob")
            Claim(JwtClaimTypes.FamilyName, "Smith")
            Claim(JwtClaimTypes.Email, "BobSmith@email.com")
            Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
            Claim(JwtClaimTypes.WebSite, "http://bob.com")
            Claim(JwtClaimTypes.Address, JsonSerializer.Serialize(address), IdentityServerConstants.ClaimValueTypes.Json)
            Claim("partner_name", "alice")
            Claim("marriage_time", "1900-01-01T00:00:00Z", ClaimValueTypes.DateTime)
        ])
    ])

