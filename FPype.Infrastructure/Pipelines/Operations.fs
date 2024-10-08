﻿namespace FPype.Infrastructure.Pipelines

open System
open FPype.Infrastructure.Pipelines.Models
open FsToolbox.Core.Results
open Microsoft.Extensions.Logging

module Operations =


    open Freql.MySql
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

    /// <summary>
    /// A function to return a pipeline run item.
    /// This is meant for internal use and doesn't require a user reference like getPipelineRunItem.
    /// Also less verification checks are carried out.
    /// </summary>
    /// <param name="ctx">The MySql database context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="runId">The run id.</param>
    let getPipelineRunItemInternal (ctx: MySqlContext) (logger: ILogger) (runId: string) =
        Internal.fetchPipelineRunItem ctx runId
        |> FetchResult.merge (fun pir sr -> sr, pir) (fun pir -> Fetch.subscriptionById ctx pir.SubscriptionId)
        |> FetchResult.merge (fun (sr, pri) pvr -> sr, pvr, pri) (fun (_, pvr) ->
            Fetch.pipelineVersionById ctx pvr.PipelineVersionId)
        |> FetchResult.merge (fun (sr, pvr, pri) pr -> sr, pr, pvr, pri) (fun (_, pvr, _) ->
            Fetch.pipelineById ctx pvr.PipelineId)
        |> FetchResult.merge (fun (sr, pr, pvr, pir) rur -> sr, pr, pvr, pir, rur) (fun (_, _, _, pir) ->
            Fetch.userById ctx pir.RunBy)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (sr, pr, pvr, pri, rur) ->
            let verifiers =
                [ // Verification.subscriptionIsActive sr
                  Verification.subscriptionMatches sr pr.SubscriptionId ]

            VerificationResult.verify verifiers (sr, pr, pvr, pri, rur))
        |> Result.map (fun (sr, pr, pvr, pri, rur) ->
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

    let getPipelineRunItemsForUser (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> sr, ur) (fun ur -> Fetch.subscriptionById ctx ur.SubscriptionId)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (sr, ur) ->
            let verifiers =
                [ Verification.subscriptionIsActive sr
                  Verification.userIsActive ur
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (sr, ur))
        |> Result.map (fun (sr, ur) ->
            let sql =
                """
            SELECT 
                pr.reference AS run_id,
                s.reference AS subscription_reference,
                cp.reference AS pipeline_reference,
                cp.name AS pipeline_name,
                cpv.reference AS pipeline_version_reference,
                cpv.version AS pipeline_version,
                pr.queued_on AS queued_on,
                pr.started_on AS started_on,
                pr.completed_on AS completed_on,
                pr.was_successful AS was_successful,
                pr.base_path AS base_path,
                u.reference AS run_by_reference,
                u.username AS run_by_name
            FROM pipeline_runs pr 
            JOIN subscriptions s ON pr.subscription_id = s.id
            JOIN cfg_pipeline_versions cpv ON pr.pipeline_version_id = cpv.id 
            JOIN cfg_pipelines cp ON cpv.pipeline_id = cp.id
            JOIN users u ON pr.run_by = u.id
            WHERE s.reference = @0;
            """

            ctx.SelectAnon<PipelineRunDetails>(sql, [ sr.Reference ]))
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
                      Verification.subscriptionMatches sr pr.SubscriptionId
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

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

    let startPipelineRun (ctx: MySqlContext) (logger: ILogger) (runId: string) =
        try
            ctx.ExecuteAnonNonQuery(
                "UPDATE pipeline_runs SET started_on = @0 WHERE reference = @1",
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

    let completePipelineRun (ctx: MySqlContext) (logger: ILogger) (runId: string) (wasSuccess: bool) =
        try
            ctx.ExecuteAnonNonQuery(
                "UPDATE pipeline_runs SET completed_on = @0, was_successful = @1 WHERE reference = @2",
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
