namespace FPype.Infrastructure.Configuration.Queries

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestQueryVersion (ctx: MySqlContext) (logger: ILogger) (userReference: string) (queryReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query ctx queryReference)
        |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
            Fetch.queryLatestVersion ctx qr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qr, qvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur qr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, qr, qvr))
        // Map
        |> Result.map (fun (ur, sr, qr, qvr) ->

            ({ Reference = qr.Reference
               Name = qr.Name
               Version =
                 { Reference = qvr.Reference
                   Version = qvr.Version
                   RawQuery = qvr.RawQuery
                   Hash = qvr.Hash
                   CreatedOn = qvr.CreatedOn } }
            : QueryDetails))
        |> FetchResult.fromResult

    let specificQueryVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (queryReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query ctx queryReference)
        |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
            Fetch.queryVersion ctx qr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qr, qvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur qr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, qr, qvr))
        // Map
        |> Result.map (fun (ur, sr, qr, qvr) ->

            ({ Reference = qr.Reference
               Name = qr.Name
               Version =
                 { Reference = qvr.Reference
                   Version = qvr.Version
                   RawQuery = qvr.RawQuery
                   Hash = qvr.Hash
                   CreatedOn = qvr.CreatedOn } }
            : QueryDetails))
        |> FetchResult.fromResult

    let queryVersions (ctx: MySqlContext) (logger: ILogger) (userReference: string) (queryReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query ctx queryReference)
        |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
            Fetch.queryVersionsByQueryId ctx qr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qr, qvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur qr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, qr, qvrs))
        // Map
        |> Result.map (fun (ur, sr, qr, qvrs) ->
            qvrs
            |> List.map (fun qvr ->
                ({ QueryReference = qr.Reference
                   Reference = qvr.Reference
                   Name = qr.Name
                   Version = qvr.Version }
                : QueryVersionOverview)))
        |> FetchResult.fromResult
    
    let queries (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) trs -> ur, sr, trs) (fun (_, sr) ->
            Fetch.queriesBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qrs) ->
            let verifiers =
                [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

            VerificationResult.verify verifiers (ur, sr, qrs))
        // Map
        |> Result.map (fun (ur, sr, qrs) ->
            qrs
            |> List.map (fun qr ->
                ({ Reference = qr.Reference
                   Name = qr.Name }
                : QueryOverview)))
        |> FetchResult.fromResult