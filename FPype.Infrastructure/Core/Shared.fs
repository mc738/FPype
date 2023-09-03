namespace FPype.Infrastructure.Core

[<AutoOpen>]
module Shared =

    open System.Text.Json
    open FsToolbox.Core.Results
        
    let toJson (value: 'T) =
        try
            JsonSerializer.Serialize value |> Ok
        with exn ->
            Error(
                { Message = exn.Message
                  DisplayMessage = "Failed to serialize object."
                  Exception = Some exn }
                : FailureResult
            )

    let fromJson<'T> (json: string) =
        try
            JsonSerializer.Deserialize<'T> json |> Ok
        with exn ->
            Error(
                { Message = exn.Message
                  DisplayMessage = "Failed to deserialize object."
                  Exception = Some exn }
                : FailureResult
            )