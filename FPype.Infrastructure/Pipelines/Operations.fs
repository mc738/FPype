namespace FPype.Infrastructure.Pipelines

open System
open FsToolbox.Core.Results
open Microsoft.Extensions.Logging

module Operations =


    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common

    module private Internal =

        let fetchPipelineRunItem (ctx: MySqlContext) (reference: string) =
            try
                Operations.selectPipelineRunItemRecord ctx [ "WHERE reference = @0" ] [ reference ]
                |> Option.map FetchResult.Success
                |> Option.defaultWith (fun _ ->
                    ({ Message = $"Pipeline run item (ref: {reference}) not found"
                       DisplayMessage = "Pipeline run item not found"
                       Exception = None }
                    : FailureResult)
                    |> FetchResult.Failure)
            with ex ->
                { Message = "Unhandled exception while fetching pipeline run item"
                  DisplayMessage = "Error fetching pipeline run item"
                  Exception = Some ex }
                |> FetchResult.Failure

    let getPipelineRunItem
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (runId: string)

        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.SubscriptionId)
        |> FetchResult.chain (fun (ur, sr) pri -> ur, sr, pri) (Internal.fetchPipelineRunItem ctx runId)
        |> FetchResult.merge (fun (ur, sr, pri) pvr -> ur, sr, pvr, pri) (fun (_, _, pvr) ->
            Fetch.pipelineVersionById ctx pvr.PipelineVersionId)
        |> FetchResult.merge (fun (ur, sr, pvr, pri) pr -> ur, sr, pr, pvr, pri) (fun (_, _, pvr, _) ->
            Fetch.pipelineById ctx pvr.PipelineId)
        |> FetchResult.merge (fun (ur, sr, pr, pvr, pir) rur -> ur, sr, pr, pvr, pir, rur) (fun (_, _, _, _, pir) ->
            Fetch.userById ctx pir.RunBy)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, pr, pvr, pri, rur) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.subscriptionMatches sr pr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, pr, pvr, pri, rur))
        |> Result.map (fun (ur, sr, pr, pvr, pri, rur) ->
            ({ RunId = runId
               SubscriptionReference = sr.Reference
               PipelineReference = pr.Reference
               PipelineName = pr.Name
               PipelineVersion = pvr.Version
               PipelineVersionReference = pvr.Reference
               QueuedOn = pri.QueuedOn
               StartedOn = pri.StartedOn
               CompletedOn = pri.CompletedOn
               WasSuccessful = pri.WasSuccessful
               BasePath = pri.BasePath
               RunByReference = rur.Reference
               RunByName = rur.Username }
            : Models.PipelineRunDetails))
        |> FetchResult.fromResult

    let queuePipelineRun
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (pipelineVersionReference: string)
        (basePath: string)
        (runId: string)
        =
        ctx.ExecuteInTransaction(fun t ->
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.SubscriptionId)
            |> FetchResult.chain
                (fun (ur, sr) pvr -> ur, sr, pvr)
                (Fetch.pipelineVersionByReference t pipelineVersionReference)
            |> FetchResult.merge (fun (ur, sr, pvr) pr -> ur, sr, pr, pvr) (fun (_, _, pvr) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            |> Result.bind (fun (ur, sr, pr, pvr) ->
                let verifiers =
                    [ Verification.subscriptionIsActive sr
                      Verification.userIsActive ur
                      Verification.subscriptionMatches sr pr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr))
            |> Result.map (fun (ur, sr, pr, pvr) ->
                ({ Reference = runId
                   SubscriptionId = sr.Id
                   PipelineVersionId = pvr.Id
                   QueuedOn = DateTime.UtcNow
                   StartedOn = None
                   CompletedOn = None
                   WasSuccessful = None
                   BasePath = basePath
                   RunBy = ur.Id }
                : Parameters.NewPipelineRunItem)
                |> Operations.insertPipelineRunItem t
                |> ignore))
        |> toActionResult "Queue pipeline run"

    let startPipelineRun (ctx: MySqlContext) (runId: string) =
        try
            ctx.ExecuteAnonNonQuery(
                "UPDATE pipeline_runs SET started_on = @0 WHERE id = @1",
                [ DateTime.UtcNow; runId ]
            )
            |> ignore
            |> Ok
        with ex ->
            ({ Message = $"Failed to start pipeline run. Error: {ex.Message}"
               DisplayMessage = "Failed to start pipeline run"
               Exception = Some ex }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult

    let completePipelineRun (ctx: MySqlContext) (runId: string) (wasSuccess: bool) =
        try
            ctx.ExecuteAnonNonQuery(
                "UPDATE pipeline_runs SET completed_on = @0, was_successful = @1 WHERE id = @2",
                [ DateTime.UtcNow; wasSuccess; runId ]
            )
            |> ignore
            |> Ok
        with ex ->
            ({ Message = $"Failed to complete pipeline run. Error: {ex.Message}"
               DisplayMessage = "Failed to complete pipeline run"
               Exception = Some ex }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult
