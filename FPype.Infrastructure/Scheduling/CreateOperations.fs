namespace FPype.Infrastructure.Scheduling

open System

[<RequireQualifiedAccess>]
module CreateOperations =

    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence

    let schedule (ctx: MySqlContext) (logger: ILogger) (userReference: string) (schedule: NewSchedule) =
        ctx.ExecuteInTransaction(fun t ->
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain
                (fun (ur, sr) pvr -> ur, sr, pvr)
                (Fetch.pipelineVersionByReference t schedule.PipelineVersionReference)
            |> FetchResult.merge (fun (ur, sr, pvr) pr -> ur, sr, pr, pvr) (fun (ur, sr, pvr) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, pr, pvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur pr.SubscriptionId
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr))
            // Create
            |> Result.map (fun (ur, sr, pr, pvr) ->
                let scheduleId =
                    ({ Reference = schedule.Reference
                       SubscriptionId = sr.Id
                       PipelineVersionId = pvr.Id
                       ScheduleCron = schedule.ScheduleCron
                       Active = schedule.SetAsActive }
                    : Parameters.NewPipelineSchedule)
                    |> Operations.insertPipelineSchedule t
                    |> int

                [ Events.ScheduleEvent.ScheduleCreated
                      { Reference = schedule.Reference
                        ScheduleCron = schedule.ScheduleCron }
                  if schedule.SetAsActive then
                      Events.ScheduleEvent.ScheduleActivated { Reference = schedule.Reference } ]
                |> FPype.Infrastructure.Scheduling.Events.addEvents t logger scheduleId ur.Id (getTimestamp ())
                |> ignore))
        |> toActionResult "Create schedule"

    let scheduleRun
        (ctx: MySqlContext)
        (logger: ILogger)
        (scheduleReference: string)
        (basePath: string)
        (runId: string)
        =
        ctx.ExecuteInTransaction(fun t ->
            Fetch.user t Users.defaultSystemReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.SubscriptionId)
            |> FetchResult.chain (fun (ur, sr) psr -> ur, sr, psr) (Fetch.scheduleByReference t scheduleReference)
            |> FetchResult.merge (fun (ur, sr, psr) pvr -> ur, sr, pvr, psr) (fun (_, _, psr) ->
                Fetch.pipelineVersionById t psr.PipelineVersionId)
            |> FetchResult.merge (fun (ur, sr, pvr, psr) pr -> ur, sr, pr, pvr, psr) (fun (_, _, pvr, _) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            |> Result.bind (fun (ur, sr, pr, pvr, psr) ->
                let verifiers =
                    [ Verification.subscriptionIsActive sr
                      Verification.userIsActive ur
                      Verification.scheduleIsActive psr
                      // These might not technically be needed but they can guard against regressions
                      Verification.isSystemSubscription sr
                      Verification.isSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr, psr))
            |> Result.map (fun (ur, sr, pr, pvr, psr) ->
                let timestamp = DateTime.UtcNow

                // NOTE the subscription id is the same as the pipeline. Not the system one.
                let internalRunId =
                    ({ Reference = runId
                       SubscriptionId = pr.SubscriptionId
                       PipelineVersionId = pvr.Id
                       QueuedOn = timestamp
                       StartedOn = None
                       CompletedOn = None
                       WasSuccessful = None
                       BasePath = basePath
                       RunBy = ur.Id }
                    : Parameters.NewPipelineRunItem)
                    |> Operations.insertPipelineRunItem t
                    |> int

                // NOTE the schedule run has the same reference as the run item. This could change later however.
                ({ Reference = runId
                   ScheduleId = psr.Id
                   PipelineRunId = internalRunId
                   RunOn = timestamp }
                : Parameters.NewPipelineScheduleRun)
                |> Operations.insertPipelineScheduleRun t
                |> ignore))
        |> toActionResult "Queue schedule pipeline run"
