namespace IdentityServerFs.QuickStart

open System
open IdentityServer4.Services
open IdentityServerFs.QuickStart
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting
open TiraxTech.IdentityServerHelper
open TiraxTech.Foundation

[<SecurityHeaders>]
[<AllowAnonymous>]
type HomeController(interaction: IIdentityServerInteractionService,
                    environment: IWebHostEnvironment,
                    logger: ILogger<HomeController>) =
    inherit Controller()

    member my.Index() =
        if environment.IsDevelopment() then
            my.View() :> IActionResult
        else
            logger.LogInformation("Homepage is disabled in production. Returning 404.")
            my.NotFound()
            
    member my.Error errorId =
        task {
            let! message = interaction.getErrorContextAsync errorId
            if environment.IsDevelopment() then
                message |> Option.do' (fun m -> m.ErrorDescription <- String.Empty)
            return my.View("Error", { Error = message })
        }