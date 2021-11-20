namespace IdentityServerFs.QuickStart

open System

type GrantViewModel() =
    member val ClientId = String.Empty with get, set
    member val ClientName = String.Empty with get, set
    member val ClientUrl = String.Empty with get, set
    member val ClientLogoUrl = String.Empty with get, set
    member val Description = String.Empty with get, set
    member val Created = DateTime.MinValue with get, set
    member val Expires: Nullable<DateTime> = Nullable() with get, set
    member val IdentityGrantNames: string seq = Seq.empty with get, set
    member val ApiGrantNames: string seq = Seq.empty with get, set

type GrantsViewModel() =
    member val Grants: GrantViewModel seq = Seq.empty with get, set
