namespace FPype.Infrastructure.Core

[<AutoOpen>]
module Shared =

    open System
    open System.Text.Json
    open FsToolbox.Core.Results

    type ResultCollection<'T> =
        { Success: 'T list
          Errors: FailureResult list }

        static member Create(success: 'T list, errors: FailureResult list) = { Success = success; Errors = errors }

        member rc.HasErrors() = rc.Errors.Length

    let createReference () = Guid.NewGuid().ToString("n")

    let getTimestamp () = DateTime.UtcNow

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
