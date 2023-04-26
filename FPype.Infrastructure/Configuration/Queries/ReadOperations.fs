namespace FPype.Infrastructure.Configuration.Queries

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestQueryVersion (ctx: MySqlContext) (userReference: string) (queryReference: string) =
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

    let specificQueryVersion (ctx: MySqlContext) (userReference: string) (queryReference: string) (version: int) =
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
