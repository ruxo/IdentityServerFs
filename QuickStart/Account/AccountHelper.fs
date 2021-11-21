module IdentityServerFs.QuickStart.AccountHelper

open Microsoft.AspNetCore.Mvc

let loadingPage(viewName, redirectUri) (controller: Controller) =
    controller.HttpContext.Response.StatusCode <- 200
    controller.HttpContext.Response.Headers["Location"] <- ""
    
    controller.View(viewName, RedirectViewModel(RedirectUrl = redirectUri))
