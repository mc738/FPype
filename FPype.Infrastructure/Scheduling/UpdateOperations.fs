namespace FPype.Infrastructure.Scheduling

open System.Security.Cryptography.Xml

[<RequireQualifiedAccess>]
module UpdateOperations =

    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence

    let schedule
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (scheduleReference: string)
        (update: UpdateSchedule)
        =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) psr -> ur, sr, psr) (Fetch.scheduleByReference t scheduleReference)
            |> FetchResult.merge (fun (ur, sr, psr) pvr -> ur, sr, pvr, psr) (fun (_, _, psr) ->
                Fetch.pipelineVersionById t psr.PipelineVersionId)
            |> FetchResult.merge (fun (ur, sr, pvr, psr) pr -> ur, sr, pr, pvr, psr) (fun (_, _, pvr, _) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, pr, pvr, psr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur pr.SubscriptionId
                      Verification.scheduleIsActive psr ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr, psr))
            |> Result.map (fun (ur, sr, pr, pvr, psr) ->
                t.ExecuteAnonNonQuery(
                    "UPDATE pipeline_schedules SET schedule_cron = @0 WHERE id = @1",
                    [ update.NewScheduleCron, psr.Id ]
                )
                |> ignore

                [ Events.ScheduleEvent.ScheduleUpdated
                      { Reference = psr.Reference
                        NewScheduleCron = update.NewScheduleCron } ]
                |> FPype.Infrastructure.Scheduling.Events.addEvents t logger sr.Id ur.Id (getTimestamp ())
                |> ignore))
        |> toActionResult "Update schedule"


    let activateSchedule (ctx: MySqlContext) (logger: ILogger) (userReference: string) (scheduleReference: string) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) psr -> ur, sr, psr) (Fetch.scheduleByReference t scheduleReference)
            |> FetchResult.merge (fun (ur, sr, psr) pvr -> ur, sr, pvr, psr) (fun (_, _, psr) ->
                Fetch.pipelineVersionById t psr.PipelineVersionId)
            |> FetchResult.merge (fun (ur, sr, pvr, psr) pr -> ur, sr, pr, pvr, psr) (fun (_, _, pvr, _) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, pr, pvr, psr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur pr.SubscriptionId
                      Verification.scheduleIsActive psr ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr, psr))
            |> Result.map (fun (ur, sr, pr, pvr, psr) ->
                match psr.Active |> not with
                | true ->

                    t.ExecuteAnonNonQuery("UPDATE pipeline_schedules SET active = TRUE WHERE id = @1", [ psr.Id ])
                    |> ignore

                    [ Events.ScheduleEvent.ScheduleActivated { Reference = psr.Reference } ]
                    |> FPype.Infrastructure.Scheduling.Events.addEvents t logger sr.Id ur.Id (getTimestamp ())
                    |> ignore
                | false -> logger.LogInformation($"Schedule `{psr.Reference}` is already active, skipping.")))
        |> toActionResult "Active schedule"
