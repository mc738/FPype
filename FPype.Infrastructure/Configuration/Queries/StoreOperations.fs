namespace FPype.Infrastructure.Configuration.Queries

open FPype.Data
open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module StoreOperations =

    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Configuration
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core

    let addQuery
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (queryReference: string)
        =
        Fetch.query ctx queryReference
        |> FetchResult.toResult
        |> Result.bind (fun q ->
            let verifiers =
                [ Verification.subscriptionMatches subscription q.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers q)
        |> Result.bind (fun q ->

            match store.AddQuery(q.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add query `{q.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addQueryVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.queryVersionByReference ctx versionReference
        |> FetchResult.merge (fun qv q -> q, qv) (fun qv -> Fetch.queryById ctx qv.QueryId)
        |> FetchResult.toResult
        |> Result.bind (fun (q, qv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription q.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (q, qv))
        |> Result.bind (fun (q, qv) ->

            let result =
                // If the query is a serialized query then deserialize it and convert to sql.
                // If not the query is already sql.
                match qv.IsSerialized with
                | true ->
                    SerializableQueries.Query.Deserialize(qv.RawQuery)
                    |> Result.map (fun q -> q.ToSql())
                | false -> Ok qv.RawQuery
                |> Result.bind (fun rq ->
                    store.AddQueryVersion(
                        IdType.Specific qv.Reference,
                        q.Name,
                        rq,
                        ItemVersion.Specific qv.Version
                    ))

            match result with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add query version `{qv.Reference}` ({q.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addAllQueryVersions
        (ctx: MySqlContext)
        (logger: ILogger)
        (failOnError: bool)
        (subscription: Records.Subscription)
        (store: ConfigurationStore)
        =
        // NOTE
        let result =
            Fetch.queriesBySubscriptionId ctx subscription.Id
            |> FetchResult.toResult
            |> Result.map (fun qs ->
                qs
                |> List.collect (fun q ->
                    // NOTE - expandResults will "gloss over" an error in getting the versions. This may or may not be desirable.
                    Fetch.queryVersionsByQueryId ctx q.Id
                    |> expandResult
                    |> List.map (fun qv ->
                        // If the query is a serialized query then deserialize it and convert to sql.
                        // If not the query is already sql.
                        match qv.IsSerialized with
                        | true ->
                            SerializableQueries.Query.Deserialize(qv.RawQuery)
                            |> Result.map (fun q -> q.ToSql())
                        | false -> Ok qv.RawQuery
                        |> Result.bind (fun rq ->
                            store.AddQueryVersion(
                                IdType.Specific qv.Reference,
                                q.Name,
                                rq,
                                ItemVersion.Specific qv.Version
                            )))))

        match result with
        | Ok rs ->
            match rs |> FPype.Core.Common.flattenResultList with
            | Ok _ -> ActionResult.Success store
            | Error e ->
                match failOnError with
                | true ->
                    ({ Message = $"Aggregated failure message: {e}"
                       DisplayMessage = "Failed to add table versions"
                       Exception = None }
                    : FailureResult)
                    |> ActionResult.Failure
                | false -> ActionResult.Success store
        | Error f ->
            match failOnError with
            | true -> ActionResult.Failure f
            | false -> ActionResult.Success store
