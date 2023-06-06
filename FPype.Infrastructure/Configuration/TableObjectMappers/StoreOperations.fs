namespace FPype.Infrastructure.Configuration.TableObjectMappers

[<RequireQualifiedAccess>]
module StoreOperations =

    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Configuration
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core

    let addTableObjectMapper
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (mapperReference: string)
        =
        Fetch.tableObjectMapper ctx mapperReference
        |> FetchResult.toResult
        |> Result.bind (fun m ->
            let verifiers =
                [ Verification.subscriptionMatches subscription m.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers m)
        |> Result.bind (fun m ->

            match store.AddTableObjectMapper(m.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add table object mapper `{m.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addTableObjectMapperVersion
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.tableObjectMapperVersionByReference ctx versionReference
        |> FetchResult.merge (fun mv m -> m, mv) (fun mv -> Fetch.tableObjectMapperById ctx mv.TableObjectMapperId)
        |> FetchResult.toResult
        |> Result.bind (fun (m, mv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription m.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (m, mv))
        |> Result.bind (fun (m, mv) ->

            match
                store.AddTableObjectMapperVersion(
                    IdType.Specific mv.Reference,
                    m.Name,
                    mv.MapperJson,
                    ItemVersion.Specific mv.Version
                )
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage =
                     $"Failed to add table object mapper version `{mv.Reference}` ({m.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addAllTableObjectMapperVersions
        (ctx: MySqlContext)
        (failOnError: bool)
        (subscription: Records.Subscription)
        (store: ConfigurationStore)
        =
        let result =
            Fetch.tableObjectMappersBySubscriptionId ctx subscription.Id
            |> FetchResult.toResult
            |> Result.map (fun ms ->
                ms
                |> List.collect (fun m ->
                    // NOTE - expandResults will "gloss over" an error in getting the versions. This may or may not be desirable.
                    Fetch.tableObjectMapperVersionByMapperId ctx m.Id
                    |> expandResult
                    |> List.map (fun mv ->

                        store.AddTableObjectMapperVersion(
                            IdType.Specific mv.Reference,
                            m.Name,
                            mv.MapperJson,
                            ItemVersion.Specific mv.Version
                        ))))

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
