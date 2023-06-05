namespace FPype.Infrastructure.Configuration.Pipelines

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

            match store .AddTable(p.Name) with
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
                store.AddPipelineVersion(IdType.Specific pv.Reference, p.Name, pv.Description, ItemVersion.Specific pv.Version)
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add pipeline version `{pv.Reference}` ({p.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult
    
    let addPipelineResource
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (pipelineResourceReference: string)
        =
        Fetch.pipelineResourceByReference ctx pipelineResourceReference
        |> FetchResult.merge (fun prv pv -> pv, prv) (fun prv -> Fetch.pipelineVersionById ctx prv.PipelineVersionId)
        |> FetchResult.merge (fun (pv, prv) p -> p, pv, prv) (fun (pv, _) -> Fetch.pipelineById ctx pv.PipelineId)
        |> FetchResult.merge (fun (p, pv, prv) rv -> p, pv, prv, rv) (fun (_, _, prv) -> Fetch.resourceVersionById ctx prv.ResourceVersionId)
        |> FetchResult.merge (fun (p, pv, prv, rv) r -> p, pv, prv, r, rv) (fun (_, _, _, rv) -> Fetch.resourceById ctx rv.ResourceId)
        |> FetchResult.toResult
        |> Result.bind (fun ( p, pv, prv, r, rv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription p.SubscriptionId
                  Verification.subscriptionMatches subscription r.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (p, pv, prv, r, rv))
        |> Result.bind (fun (p, pv, prv, r, rv) ->

            match
                store.AddPipelineResource(IdType.Specific prv.Reference, pv.Reference, rv.Reference)
            with
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
                store.AddPipelineArg(IdType.Specific pv.Reference, p.Name, pv.Description, ItemVersion.Specific pv.Version)
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add pipeline version `{pv.Reference}` ({p.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult