module IdentityServerFs.AuthorizationRequest

open System
open IdentityServer4.Models

let inline getIdp (ctx: AuthorizationRequest) = Option.ofObj ctx.IdP

let inline isNativeClient (ctx: AuthorizationRequest) =
    not <| ctx.RedirectUri.StartsWith("https", StringComparison.Ordinal) &&
    not <| ctx.RedirectUri.StartsWith("http", StringComparison.Ordinal)
