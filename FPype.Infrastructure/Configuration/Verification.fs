namespace FPype.Infrastructure.Configuration

open FPype.Infrastructure.Core

module Verification =

    open FPype.Infrastructure.Configuration.Persistence
    
    let verifySubscription (subscription: Records.Subscription) (subscriptionId: int) _ =
        match subscription.Id = subscriptionId with
        | true -> VerificationResult.Success
        | false -> VerificationResult.WrongSubscription
        
    let verifyUserSubscription (user: Records.User) (subscriptionId: int) _ =
        match user.SubscriptionId = subscriptionId with
        | true -> VerificationResult.Success
        | false -> VerificationResult.WrongSubscription
