namespace FPype.Infrastructure.Configuration.Queries

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

            match
                store.AddQueryVersion(IdType.Specific qv.Reference, q.Name, qv.RawQuery, ItemVersion.Specific qv.Version)
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add query version `{qv.Reference}` ({q.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult
