namespace FPype.Infrastructure.Configuration.ObjectTableMappers

[<RequireQualifiedAccess>]
module StoreOperations =

    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Configuration
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core

    let addObjectTableMapper
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (mapperReference: string)
        =
        Fetch.objectTableMapper ctx mapperReference
        |> FetchResult.toResult
        |> Result.bind (fun m ->
            let verifiers =
                [ Verification.subscriptionMatches subscription m.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers m)
        |> Result.bind (fun m ->

            match store.AddObjectTableMapper(m.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add object table mapper `{m.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addObjectTableMapperVersion
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.objectTableMapperVersionByReference ctx versionReference
        |> FetchResult.merge (fun mv m -> m, mv) (fun mv -> Fetch.tableObjectMapperById ctx mv.ObjectTableMapperId)
        |> FetchResult.merge (fun (m, mv) tv -> m, mv, tv) (fun (_, mv) ->
            Fetch.tableVersionById ctx mv.TableModelVersionId)
        |> FetchResult.merge (fun (m, mv, tv) t -> m, mv, t, tv) (fun (_, _, tv) -> Fetch.tableById ctx tv.TableModelId)
        |> FetchResult.toResult
        |> Result.bind (fun (m, mv, t, tv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription m.SubscriptionId
                  Verification.subscriptionMatches subscription t.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (m, mv, t, tv))
        |> Result.bind (fun (m, mv, t, tv) ->

            match
                store.AddObjectTableMapperVersion(
                    IdType.Specific mv.Reference,
                    m.Name,
                    tv.Reference,
                    mv.MapperJson,
                    ItemVersion.Specific mv.Version
                )
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage =
                     $"Failed to add object table mapper version `{mv.Reference}` ({m.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addAllObjectTableMapperVersions
        (ctx: MySqlContext)
        (failOnError: bool)
        (subscription: Records.Subscription)
        (store: ConfigurationStore)
        =
        let result =
            Fetch.objectTableMappersBySubscriptionId ctx subscription.Id
            |> FetchResult.toResult
            |> Result.map (fun ms ->
                ms
                |> List.collect (fun m ->
                    // NOTE - expandResults will "gloss over" an error in getting the versions. This may or may not be desirable.
                    Fetch.objectTableMapperVersionByMapperId ctx m.Id
                    |> expandResult
                    |> List.map (fun mv ->
                        match Fetch.tableVersionById ctx mv.TableModelVersionId with
                        | FetchResult.Success tv ->
                            store.AddObjectTableMapperVersion(
                                IdType.Specific mv.Reference,
                                m.Name,
                                tv.Reference,
                                mv.MapperJson,
                                ItemVersion.Specific mv.Version
                            )
                        | FetchResult.Failure f -> Error f.Message)))

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
