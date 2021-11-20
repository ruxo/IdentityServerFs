namespace IdentityServerFs.QuickStart

open IdentityServer4.Models

type ErrorViewModel = {
    Error: ErrorMessage option
}
with
    static member Empty = { Error=None }