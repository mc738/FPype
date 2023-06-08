namespace FPype.Infrastructure.Configuration.Common

open System
open System.Text.Json
open FPype.Data.Store
open FsToolbox.Core.Results
open Microsoft.FSharp.Core

[<AutoOpen>]
module Shared =

    let createReference () = Guid.NewGuid().ToString("n")

    let getTimestamp () = DateTime.UtcNow

    let handleFetchResult = ()

    let optionalToFetchResult<'T> (name: string) (result: Result<'T option, FailureResult>) =
        match result with
        | Ok r1 ->
            r1
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                { Message = $"{name} fetched failed"
                  DisplayMessage = $"{name} fetched failed"
                  Exception = None }
                |> FetchResult.Failure)
        | Error fr -> FetchResult.Failure fr

    let toActionResult<'T> (name: string) (result: Result<Result<'T, FailureResult>, string>) =
        match result with
        | Ok r1 -> ActionResult.fromResult r1
        | Error e ->
            ({ Message = $"{name} action failed: {e}"
               DisplayMessage = $"{name} action failed"
               Exception = None }
            : FailureResult)
            |> ActionResult.Failure

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

    let collectFetchResults<'T> (failOnError: bool) (results: FetchResult<'T> list) =
        results
        |> List.fold
            (fun (r: Result<'T list, FailureResult>) (fr) ->

                match r with
                | Ok acc ->
                    match fr with
                    | FetchResult.Success v -> v :: acc |> Ok
                    | FetchResult.Failure f ->
                        match failOnError with
                        | true -> Error f
                        | false -> Ok acc
                | Error f -> Error f)
            (Ok [])

    let expandResult<'T> (results: FetchResult<'T list>) =
        match results with
        | FetchResult.Success v -> v
        | FetchResult.Failure f -> []

    
    

    let chooseFetchResults<'T> (errorFn: FailureResult -> unit) (results: FetchResult<'T> list) =
        results
        |> List.choose (fun r ->
            match r with
            | FetchResult.Success v -> Some v
            | FetchResult.Failure f ->
                errorFn f
                None)
