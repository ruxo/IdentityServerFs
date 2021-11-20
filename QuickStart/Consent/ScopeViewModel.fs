namespace IdentityServerFs.QuickStart

open System

type ScopeViewModel() =
    member val Value = String.Empty with get, set
    member val DisplayName = String.Empty with get, set
    member val Description = String.Empty with get, set
    member val Emphasize = false with get, set
    member val Required = false with get, set
    member val Checked = false with get, set

