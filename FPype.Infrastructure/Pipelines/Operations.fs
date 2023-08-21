namespace FPype.Infrastructure.Pipelines

open System

module Operations =


    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common

    let queuePipelineRun
        (ctx: MySqlContext)
        (userReference: string)
        (pipelineVersionId: string)
        (basePath: string)
        (runId: string)
        =
        ctx.ExecuteInTransaction(fun t ->
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.SubscriptionId)
            |> FetchResult.chain
                (fun (ur, sr) pvr -> ur, sr, pvr)
                (Fetch.pipelineVersionByReference t pipelineVersionId)
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
