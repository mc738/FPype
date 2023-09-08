namespace FPype.Infrastructure.Core

open FPype.Configuration.Persistence


[<RequireQualifiedAccess>]
module Verification =
    
    let isActive<'T> (name: string) (testFn: 'T -> bool) (value: 'T) =
        match testFn value with
        | true -> VerificationResult.Success
        | false -> VerificationResult.ItemInactive name
