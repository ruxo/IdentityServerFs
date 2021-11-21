module IdentityServerFs.Helpers

open System.Security.Claims

type ClaimsPrincipal with
    member inline my.findFirst claim =
        my.FindFirst(claim: string) |> Option.ofObj