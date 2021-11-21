namespace IdentityServerFs.QuickStart

open System

type GrantViewModel = {
    ClientId: string
    ClientName: string
    ClientUrl: string
    ClientLogoUrl: string option
    Description: string option
    Created: DateTime
    Expires: DateTime option
    IdentityGrantNames: string seq
    ApiGrantNames: string seq
}

type GrantsViewModel = {
    Grants: GrantViewModel seq
}
