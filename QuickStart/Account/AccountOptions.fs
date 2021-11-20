module IdentityServerFs.QuickStart.AccountOptions

open System

[<Literal>]
let AllowLocalLogin = true

[<Literal>]
let AllowRememberLogin = true

let RememberMeLoginDuration = TimeSpan.FromDays 30

[<Literal>]
let ShowLogoutPrompt = true

[<Literal>]
let AutomaticRedirectAfterSignOut = false

[<Literal>]
let InvalidCredentialsErrorMessage = "Invalid username or password";
