namespace FPype.Infrastructure.Core

open System


[<RequireQualifiedAccess>]
module Verification =

    open FPype.Configuration.Persistence
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    
     [<RequireQualifiedAccess>]
    type VerificationResult =
        | Success
        | MissingPermission of Name: string
        | WrongSubscription
        | ItemInactive of ItemType: string
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
                ({ Message = "Item's subscription is wrong"
                   DisplayMessage = "Item's subscription is wrong"
                   Exception = None }
                : FailureResult)
                |> Error
            | ItemInactive itemType ->
                ({ Message = $"{itemType} is inactive"
                   DisplayMessage = $"{itemType} is inactive"
                   Exception = None }
                : FailureResult)
                |> Error
            | Failure failure -> Error failure

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

    let isActive<'T> (name: string) (testFn: 'T -> bool) (value: 'T) =
        match testFn value with
        | true -> VerificationResult.Success
        | false -> VerificationResult.ItemInactive name

    let isTrue (onFalse: FailureResult) (result: bool) =
        match result with
        | true -> VerificationResult.Success
        | false -> VerificationResult.Failure onFalse

    let isFalse (onTrue: FailureResult) (result: bool) =
        match result with
        | true -> VerificationResult.Failure onTrue
        | false -> VerificationResult.Success

    let bespoke<'T> (handler: 'T -> VerificationResult) (value: 'T) = handler value

    let subscriptionMatches (subscription: Records.Subscription) (subscriptionId: int) _ =
        match subscription.Id = subscriptionId with
        | true -> VerificationResult.Success
        | false -> VerificationResult.WrongSubscription

    let userSubscriptionMatches (user: Records.User) (subscriptionId: int) _ =
        match user.SubscriptionId = subscriptionId with
        | true -> VerificationResult.Success
        | false -> VerificationResult.WrongSubscription

    let userIsActive (user: Records.User) _ =
        match user.Active = true with
        | true -> VerificationResult.Success
        | false -> VerificationResult.ItemInactive "User"

    let subscriptionIsActive (subscription: Records.Subscription) _ =
        match subscription.Active = true with
        | true -> VerificationResult.Success
        | false -> VerificationResult.ItemInactive "Subscription"

    let isSystemSubscription (subscription: Records.Subscription) _ =
        match Subscriptions.isSystemSubscription subscription with
        | true -> VerificationResult.Success
        | false ->
            ({ Message = "The operation can only be completed by a system subscription"
               DisplayMessage = "Wrong subscription type"
               Exception = None }
            : FailureResult)
            |> VerificationResult.Failure
            
    let isNotSystemSubscription (subscription: Records.Subscription) _ =
        match Subscriptions.isSystemSubscription subscription |> not with
        | true -> VerificationResult.Success
        | false ->
            ({ Message = "The operation can only be completed by a non-system subscription"
               DisplayMessage = "Wrong subscription type"
               Exception = None }
            : FailureResult)
            |> VerificationResult.Failure
    
    let isSystemUser (user: Records.User) _ =
        match Users.isSystemUser user with
        | true -> VerificationResult.Success
        | false ->
            ({ Message = "The operation can only be completed by a system user"
               DisplayMessage = "Wrong user type"
               Exception = None }
            : FailureResult)
            |> VerificationResult.Failure
            
    let isNotSystemUser (user: Records.User) _ =
        match Users.isSystemUser user |> not with
        | true -> VerificationResult.Success
        | false ->
            ({ Message = "The operation can only be completed by a non-system user"
               DisplayMessage = "Wrong user type"
               Exception = None }
            : FailureResult)
            |> VerificationResult.Failure