namespace FPype.Infrastructure.Configuration.Queries

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let query (ctx: MySqlContext) (userReference: string) (query: NewQuery) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr) ->
                let verifiers =
                    [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

                VerificationResult.verify verifiers (ur, sr))
            // Create
            |> Result.map (fun (ur, sr) ->

                let queryId =
                    ({ Reference = query.Reference
                       SubscriptionId = sr.Id
                       Name = query.Name }
                    : Parameters.NewQuery)
                    |> Operations.insertQuery t
                    |> int

                ({ Reference = query.Version.Reference
                   QueryId = queryId
                   Version = 1
                   RawQuery = query.Version.RawQuery
                   Hash = query.Version.RawQuery.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewQueryVersion)
                |> Operations.insertQueryVersion t
                |> ignore))
        |> toActionResult "Create query"

    let queryVersion (ctx: MySqlContext) (userReference: string) (queryReference: string) (version: NewQueryVersion) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query t queryReference)
            |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
                Fetch.queryLatestVersion t qr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, qr, qvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur qr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, qr, qvr))
            // Create
            |> Result.map (fun (ur, sr, qr, qvr) ->

                ({ Reference = version.Reference
                   QueryId = qr.Id
                   Version = qvr.Version + 1
                   RawQuery = version.RawQuery
                   Hash = version.RawQuery.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewQueryVersion)
                |> Operations.insertQueryVersion t
                |> ignore))
        |> toActionResult "Create query version"