namespace FPype.Infrastructure.Configuration.Pipelines

open FPype.Configuration
open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module StoreOperations =

    open FPype.Configuration
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let addPipeline
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (pipelineReference: string)
        =
        Fetch.pipeline ctx pipelineReference
        |> FetchResult.toResult
        |> Result.bind (fun p ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers p)
        |> Result.bind (fun p ->

            match store.AddPipeline(p.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add pipeline `{p.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addPipelineVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.pipelineVersionByReference ctx versionReference
        |> FetchResult.merge (fun pv p -> p, pv) (fun pv -> Fetch.pipelineById ctx pv.PipelineId)
        |> FetchResult.toResult
        |> Result.bind (fun (p, pv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (p, pv))
        |> Result.bind (fun (p, pv) ->

            match
                store.AddPipelineVersion(
                    IdType.Specific pv.Reference,
                    p.Name,
                    pv.Description,
                    ItemVersion.Specific pv.Version
                )
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage =
                     $"Failed to add pipeline version `{pv.Reference}` ({p.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addPipelineResource
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (pipelineResourceReference: string)
        =
        Fetch.pipelineResourceByReference ctx pipelineResourceReference
        |> FetchResult.merge (fun prv pv -> pv, prv) (fun prv -> Fetch.pipelineVersionById ctx prv.PipelineVersionId)
        |> FetchResult.merge (fun (pv, prv) p -> p, pv, prv) (fun (pv, _) -> Fetch.pipelineById ctx pv.PipelineId)
        |> FetchResult.merge (fun (p, pv, prv) rv -> p, pv, prv, rv) (fun (_, _, prv) ->
            Fetch.resourceVersionById ctx prv.ResourceVersionId)
        |> FetchResult.merge (fun (p, pv, prv, rv) r -> p, pv, prv, r, rv) (fun (_, _, _, rv) ->
            Fetch.resourceById ctx rv.ResourceId)
        |> FetchResult.toResult
        |> Result.bind (fun (p, pv, prv, r, rv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  Verification.subscriptionMatches subscription r.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (p, pv, prv, r, rv))
        |> Result.bind (fun (p, pv, prv, r, rv) ->

            match store.AddPipelineResource(IdType.Specific prv.Reference, pv.Reference, rv.Reference) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add pipeline resource `{prv.Reference}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addPipelineArg
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.pipelineArgByReference ctx versionReference
        |> FetchResult.merge (fun pa pv -> pv, pa) (fun pa -> Fetch.pipelineVersionById ctx pa.PipelineVersionId)
        |> FetchResult.merge (fun (pv, pa) p -> p, pv, pa) (fun (pv, _) -> Fetch.pipelineById ctx pv.PipelineId)
        |> FetchResult.toResult
        |> Result.bind (fun (p, pv, pa) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (p, pv, pa))
        |> Result.bind (fun (p, pv, pa) ->
            match
                store.AddPipelineArg(
                    IdType.Specific pa.Reference,
                    p.Name,
                    pa.Name,
                    pa.Required,
                    pa.DefaultValue,
                    ItemVersion.Specific pv.Version
                )
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add pipeline arg `{pa.Reference}` ({p.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addPipelineAction
        (ctx: MySqlContext)
        (logger: ILogger)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.pipelineActionByReference ctx versionReference
        |> FetchResult.merge (fun pa pv -> pv, pa) (fun pa -> Fetch.pipelineVersionById ctx pa.PipelineVersionId)
        |> FetchResult.merge (fun (pv, pa) p -> p, pv, pa) (fun (pv, _) -> Fetch.pipelineById ctx pv.PipelineId)
        |> FetchResult.merge (fun (p, pv, pa) a -> p, pv, pa, a) (fun (_, _, pa) ->
            Fetch.actionTypeById ctx pa.ActionTypeId)
        |> FetchResult.toResult
        |> Result.bind (fun (p, pv, pa, a) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (p, pv, pa, a))
        |> Result.bind (fun (p, pv, pa, a) ->
            match
                store.AddPipelineAction(
                    IdType.Specific pa.Reference,
                    p.Name,
                    pa.Name,
                    a.Name,
                    pa.ActionJson,
                    pa.Step,
                    ItemVersion.Specific pv.Version
                )
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage =
                     $"Failed to add pipeline action `{pa.Reference}` ({pa.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addAllPipelineVersions
        (ctx: MySqlContext)
        (logger: ILogger)
        (failOnError: bool)
        (subscription: Records.Subscription)
        (store: ConfigurationStore)
        =
        let result =
            Fetch.pipelinesBySubscriptionId ctx subscription.Id
            |> FetchResult.toResult
            |> Result.map (fun ps ->
                let actionTypesMap =
                    Operations.selectActionTypeRecords ctx [] []
                    |> List.map (fun at -> at.Id, at.Name)
                    |> Map.ofList

                ps
                |> List.collect (fun p ->
                    // NOTE - expandResults will "gloss over" an error in getting the versions. This may or may not be desirable.
                    Fetch.pipelineVersionsByPipelineId ctx p.Id
                    |> expandResult
                    |> List.map (fun pv ->
                        // Add version

                        store.AddPipelineVersion(
                            IdType.Specific pv.Reference,
                            p.Name,
                            pv.Description,
                            ItemVersion.Specific pv.Version
                        )
                        |> Result.bind (fun _ ->
                            // Add actions

                            match Fetch.pipelineActions ctx pv.Id with
                            | FetchResult.Success pas ->
                                let r =
                                    pas
                                    |> List.map (fun pa ->
                                        match actionTypesMap.TryFind pa.ActionTypeId with
                                        | Some atn ->
                                            store.AddPipelineAction(
                                                IdType.Specific pa.Reference,
                                                p.Name,
                                                pa.Name,
                                                atn,
                                                pa.ActionJson,
                                                pa.Step,
                                                ItemVersion.Specific pv.Version
                                            )
                                        | None -> Error $"Action type (id: {pa.ActionTypeId}) not found")
                                    |> FPype.Core.Common.flattenResultList

                                match r with
                                | Ok _ -> Ok()
                                | Error e ->
                                    match failOnError with
                                    | true -> Error $"Aggregated failure message: {e}"
                                    | false -> Ok()
                            | FetchResult.Failure f ->
                                match failOnError with
                                | true -> Error f.Message
                                | false -> Ok())
                        |> Result.bind (fun _ ->
                            // Add resources
                            match Fetch.pipelineResourcesByPipelineVersionId ctx pv.Id with
                            | FetchResult.Success prs ->
                                let r =
                                    prs
                                    |> List.map (fun pr ->
                                        match Fetch.resourceVersionById ctx pr.ResourceVersionId with
                                        | FetchResult.Success rv ->
                                            store.AddPipelineResource(
                                                IdType.Specific pr.Reference,
                                                pv.Reference,
                                                rv.Reference
                                            )
                                        | FetchResult.Failure f ->
                                            match failOnError with
                                            | true -> Error f.Message
                                            | false -> Ok())
                                    |> FPype.Core.Common.flattenResultList

                                match r with
                                | Ok _ -> Ok()
                                | Error e ->
                                    match failOnError with
                                    | true -> Error $"Aggregated failure message: {e}"
                                    | false -> Ok()
                            | FetchResult.Failure f ->
                                match failOnError with
                                | true -> Error f.Message
                                | false -> Ok())
                        |> Result.bind (fun _ ->
                            // Add args
                            match Fetch.pipelineArgByVersionId ctx pv.Id with
                            | FetchResult.Success pas ->
                                let r =
                                    pas
                                    |> List.map (fun pa ->
                                        store.AddPipelineArg(
                                            IdType.Specific pa.Reference,
                                            p.Name,
                                            pa.Name,
                                            pa.Required,
                                            pa.DefaultValue,
                                            ItemVersion.Specific pv.Version
                                        ))
                                    |> FPype.Core.Common.flattenResultList

                                match r with
                                | Ok _ -> Ok()
                                | Error e ->
                                    match failOnError with
                                    | true -> Error $"Aggregated failure message: {e}"
                                    | false -> Ok()
                            | FetchResult.Failure f ->
                                match failOnError with
                                | true -> Error f.Message
                                | false -> Ok()))))

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
