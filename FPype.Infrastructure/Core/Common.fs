namespace FPype.Infrastructure.Core

open System.Reflection
open FsToolbox.Core
open FsToolbox.Core.Results

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    type VerificationResult =
        | Success
        | MissingPermission of Name: string
        | WrongSubscription
        | Failure of FailureResult

        member vr.ToResult() =
            match vr with
            | Success -> Ok()
            | MissingPermission name ->
                ({ Message = $"`{name}` permission is missing."
                   DisplayMessage = "A permission is missing."
                   Exception = None }
                : FailureResult)
                |> Error
            | WrongSubscription ->
                ({ Message = "Item's subscription is wrong."
                   DisplayMessage = "Item's subscription is wrong."
                   Exception = None }
                : FailureResult)
                |> Error
            | Failure failure -> Error failure

        member vr.IsSuccess() =
            match vr with
            | Success -> true
            | _ -> false

    module VerificationResult =

        let chain (fn: unit -> VerificationResult) (result: VerificationResult) =
            match result with
            | VerificationResult.Success -> fn ()
            | _ -> result

        let toResult<'T> (value: 'T) (verificationResult: VerificationResult) =
            verificationResult.ToResult() |> Result.map (fun _ -> value)

        let verify<'T> (verifiers: (unit -> VerificationResult) list) (value: 'T) =
            verifiers
            |> List.fold
                (fun r v ->
                    match r with
                    | VerificationResult.Success -> v ()
                    | _ -> r)
                (VerificationResult.Success)
            |> toResult value
