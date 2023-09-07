namespace FPype.Infrastructure.Configuration.Common

open FPype.Infrastructure.Core
open FsToolbox.Core.Results

[<RequireQualifiedAccess>]
module Verification =
        
    open FPype.Infrastructure.Core    
    open FPype.Infrastructure.Core.Persistence
    
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
        
    let isTrue (onFalse: FailureResult) (result: bool) =
        match result with
        | true -> VerificationResult.Success
        | false -> VerificationResult.Failure onFalse
        
    let isFalse (onTrue: FailureResult) (result: bool) =
        match result with
        | true -> VerificationResult.Failure onTrue
        | false ->  VerificationResult.Success