namespace FPype.Infrastructure.Configuration.Resources

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =


    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestResourceVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (resourceReference: string)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource ctx resourceReference)
        |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
            Fetch.resourceLatestVersion ctx rr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rr, rvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur rr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, rr, rvr))
        // Map
        |> Result.map (fun (ur, sr, rr, rvr) ->

            ({ Reference = rr.Reference
               Name = rr.Name
               Version =
                 { Reference = rvr.Reference
                   Version = rvr.Version
                   Type = rvr.ResourceType
                   Path = rvr.ResourcePath
                   Hash = rvr.Hash
                   CreatedOn = rvr.CreatedOn } }
            : ResourceDetails))
        |> FetchResult.fromResult

    let specificResourceVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (resourceReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource ctx resourceReference)
        |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
            Fetch.resourceVersion ctx rr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rr, rvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur rr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, rr, rvr))
        // Map
        |> Result.map (fun (ur, sr, rr, rvr) ->

            ({ Reference = rr.Reference
               Name = rr.Name
               Version =
                 { Reference = rvr.Reference
                   Version = rvr.Version
                   Type = rvr.ResourceType
                   Path = rvr.ResourcePath
                   Hash = rvr.Hash
                   CreatedOn = rvr.CreatedOn } }
            : ResourceDetails))
        |> FetchResult.fromResult
        
    let resourceVersions (ctx: MySqlContext) (logger: ILogger) (userReference: string) (resourceReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource ctx resourceReference)
        |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
            Fetch.resourceVersionsByResourceId ctx rr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rr, rvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur rr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, rr, rvrs))
        // Map
        |> Result.map (fun (ur, sr, rr, rvrs) ->
            rvrs
            |> List.map (fun rvr ->
                ({ ResourceReference = rr.Reference
                   Reference = rvr.Reference
                   Name = rr.Name
                   Version = rvr.Version }
                : ResourceVersionOverview)))
        |> FetchResult.fromResult
    
    let resources (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) rrs -> ur, sr, rrs) (fun (_, sr) ->
            Fetch.resourcesBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rrs) ->
            let verifiers =
                [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

            VerificationResult.verify verifiers (ur, sr, rrs))
        // Map
        |> Result.map (fun (ur, sr, qrs) ->
            qrs
            |> List.map (fun qr ->
                ({ Reference = qr.Reference
                   Name = qr.Name }
                : ResourceOverview)))
        |> FetchResult.fromResult
