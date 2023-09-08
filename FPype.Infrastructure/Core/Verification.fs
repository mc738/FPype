namespace FPype.Infrastructure.Core


[<RequireQualifiedAccess>]
module Verification =

    open FPype.Configuration.Persistence
    open FsToolbox.Core.Results
        
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
        | false ->  VerificationResult.Success

    let bespoke<'T> (handler: 'T -> VerificationResult) (value: 'T) = handler value     
    