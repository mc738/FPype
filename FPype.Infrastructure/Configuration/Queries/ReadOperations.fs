namespace FPype.Infrastructure.Configuration.Queries

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
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
                  Verification.userSubscriptionMatches ur qr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, qr, qvr))
        // Map
        |> Result.map (fun (ur, sr, qr, qvr) -> QueryVersionDetails.FromEntity(qr, qvr))
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
                  Verification.userSubscriptionMatches ur qr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, qr, qvr))
        // Map
        |> Result.map (fun (ur, sr, qr, qvr) -> QueryVersionDetails.FromEntity(qr, qvr))
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
        |> Result.map (fun (ur, sr, qr, qvrs) -> qvrs |> List.map (fun qvr -> QueryVersionOverview.FromEntity(qr, qvr)))
        |> FetchResult.fromResult

    let queries (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) qrs -> ur, sr, qrs) (fun (_, sr) -> Fetch.queriesBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, qrs))
        // Map
        |> Result.map (fun (ur, sr, qrs) ->
            qrs
            |> List.map (fun qr ->
                ({ Reference = qr.Reference
                   Name = qr.Name }
                : QueryOverview)))
        |> FetchResult.fromResult

    let query (ctx: MySqlContext) (logger: ILogger) (userReference: string) (queryReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) qrs -> ur, sr, qrs) (Fetch.query ctx queryReference)
        |> FetchResult.merge (fun (ur, sr, qr) qvrs -> ur, sr, qr, qvrs) (fun (_, _, qr) ->
            Fetch.queryVersionsByQueryId ctx qr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, qr, qvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.subscriptionMatches sr qr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, qr, qvrs))
        // Map
        |> Result.map (fun (ur, sr, qr, qvrs) ->
            ({ Reference = qr.Reference
               Name = qr.Name
               Versions = qvrs |> List.map (fun qvr -> QueryVersionDetails.FromEntity(qr, qvr)) }
            : QueryDetails))
        |> FetchResult.fromResult
