﻿namespace FPype.Infrastructure.Configuration.Common

open FPype.Infrastructure.Core
open Freql.MySql
open FsToolbox.Core.Results
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core

[<AutoOpen>]
module Shared =

    let handleFetchResult = ()

    type FetchOperation<'T, 'U> =
        { FetchHandler: MySqlContext -> ILogger -> FetchResult<'T>
          Verifiers: 'T -> (unit -> VerificationResult) list
          ResultHandler: 'T -> 'U }

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
