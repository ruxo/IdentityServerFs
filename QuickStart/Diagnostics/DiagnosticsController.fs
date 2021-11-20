namespace IdentityServerFs.QuickStart

open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Mvc

[<SecurityHeaders>]
[<Authorize>]
type DiagnosticsController() =
    inherit Controller()

    member my.Index() =
        let localAddresses = ["127.0.0.1"; "::1"; my.HttpContext.Connection.LocalIpAddress.ToString() ]
        task {
            if not (localAddresses |> List.contains (my.HttpContext.Connection.RemoteIpAddress.ToString()))
            then return my.NotFound() :> IActionResult
            else let! authenResult = my.HttpContext.AuthenticateAsync()
                 in return my.View(DiagnosticsViewModel.fromResult authenResult) :> IActionResult
        }
