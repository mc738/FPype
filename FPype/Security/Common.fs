namespace FPype.Security

open System
open System.Text.Json
open FsToolbox.Core

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    type SecureValue =
        | EnvironmentVariable of Variable: string

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "type" json with
            | Some "environment-variable"
            | Some "environment_variable" ->
                match Json.tryGetStringProperty "value" json with
                | Some v -> EnvironmentVariable v |> Ok
                | None -> Error "Missing `value` property"
            | Some t -> Error $"Unknown secure value type: `{t}`"
            | None -> Error $"Missing `type` property"

        member sv.Resolve() =
            match sv with
            | EnvironmentVariable variable -> Environment.GetEnvironmentVariable variable



    ()
