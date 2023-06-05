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
    

