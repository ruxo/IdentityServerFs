namespace IdentityServerFs.QuickStart

open System.Text
open System.Text.Json
open IdentityModel
open Microsoft.AspNetCore.Authentication
open Microsoft.FSharp.Core

module private DiagnosticsViewModel =
    [<Literal>]
    let ClientListKey = "client_list"

type DiagnosticsViewModel = {
    AuthenticateResult: AuthenticateResult
    Clients: string seq
}
with
    static member fromResult (result: AuthenticateResult) =
        { AuthenticateResult = result
          Clients = if result.Properties.Items.ContainsKey(DiagnosticsViewModel.ClientListKey)
                    then result.Properties.Items[DiagnosticsViewModel.ClientListKey]
                         |> Base64Url.Decode
                         |> Encoding.UTF8.GetString
                         |> JsonSerializer.Deserialize<string[]>
                         :> string seq
                    else Seq.empty }
