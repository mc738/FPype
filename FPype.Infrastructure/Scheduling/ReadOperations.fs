namespace FPype.Infrastructure.Scheduling

open System
open Freql.MySql

[<RequireQualifiedAccess>]
module ReadOperations =

    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence

    module private Internal =
        
        // SECURITY Because `additionConditions` is used to make generate SQL make sure this function is need called with untrusted values.
        let getScheduledPipelineRunDetails (ctx: MySqlContext) (additionConditions: string option) (parameters: obj list) =
            let baseSql =
                """
                SELECT 
                    pr.reference AS run_id, 
                    psr.reference AS schedule_reference,
                    s.reference AS subscription_reference,
                    cp.reference AS pipeline_reference,
                    cp.name AS pipeline_name,
                    cpv.reference AS pipeline_version_reference,
                    cpv.`version` AS pipeline_version,
                    psr.run_on AS run_on,
                    pr.queued_on AS queued_on, 
                    pr.started_on AS started_on, 
                    pr.completed_on AS completed_on, 
                    pr.was_successful AS was_successful, 
                    pr.base_path, 
                    u.reference AS run_by_reference,
                    u.username AS run_by_name
                FROM pipeline_schedule_runs psr 
                JOIN pipeline_runs pr ON psr.pipeline_run_id = pr.id
                JOIN cfg_pipeline_versions cpv ON pr.pipeline_version_id = cpv.id
                JOIN cfg_pipelines cp ON cpv.pipeline_id = cp.id
                JOIN users u ON pr.run_by = u.id
                JOIN subscriptions s ON cp.subscription_id = s.id
                """
            
            let sql =
                match additionConditions with
                | Some conditions -> [ baseSql; conditions ] |> String.concat Environment.NewLine
                | None -> baseSql
            
            ctx.SelectAnon<ScheduledPipelineRunDetails>(sql, parameters)
    
    let allEventsInternal (ctx: MySqlContext) (logger: ILogger) (previousTip: int) =
        let rc = Events.selectAllEvents ctx previousTip

        match rc.HasErrors() with
        | true ->
            rc.Errors
            |> List.iter (fun e -> logger.LogError($"Failed to deserialize event. Error: {e}"))
        | false -> ()

        rc.Success |> FetchResult.Success

    let scheduleEventsInternal (ctx: MySqlContext) (logger: ILogger) (scheduleReference: string) (previousTip: int) =
        Fetch.scheduleByReference ctx scheduleReference
        |> FetchResult.map (fun sr ->
            let rc = Events.selectScheduleEvents ctx sr.Id previousTip

            match rc.HasErrors() with
            | true ->
                rc.Errors
                |> List.iter (fun e -> logger.LogError($"Failed to deserialize event. Error: {e}"))
            | false -> ()

            rc.Success)

    let activePipelinesForUser (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.toResult
        |> Result.bind (fun (ur, sr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr))
        |> Result.map (fun (ur, sr) ->
            Operations.selectPipelineScheduleRecords ctx [ "WHERE subscription_id = @0 AND active = TRUE" ] [ sr.Id ]
            |> List.choose (fun psr ->
                // TODO what to do if pipeline or pipeline version not found?
                Operations.selectPipelineVersionRecord ctx [ "WHERE id = @0" ] [ psr.PipelineVersionId ]
                |> Option.bind (fun pvr ->
                    Operations.selectPipelineRecord ctx [ "WHERE id = @0" ] [ pvr.PipelineId ]
                    |> Option.map (fun pr -> pr, pvr))
                |> Option.map (fun (pr, pvr) ->

                    ({ Reference = psr.Reference
                       SubscriptionReference = sr.Reference
                       PipelineReference = pr.Reference
                       Pipeline = pr.Name
                       PipelineVersionReference = pvr.Reference
                       PipelineVersion = pvr.Version
                       ScheduleCron = psr.ScheduleCron }
                    : Models.ScheduleOverview))))
        |> FetchResult.fromResult

    let allActiveSchedules (ctx: MySqlContext) (logger: ILogger) =
        try
            Operations.selectPipelineScheduleRecords ctx [ "WHERE active = TRUE" ] []
            |> List.choose (fun psr ->
                // TODO what to do if pipeline or pipeline version not found?
                Operations.selectPipelineVersionRecord ctx [ "WHERE id = @0" ] [ psr.PipelineVersionId ]
                |> Option.bind (fun pvr ->
                    Operations.selectPipelineRecord ctx [ "WHERE id = @0" ] [ pvr.PipelineId ]
                    |> Option.map (fun pr -> pr, pvr))
                |> Option.bind (fun (pr, pvr) ->
                    Operations.selectSubscriptionRecord ctx [ "WHERE id = @0" ] [ pr.SubscriptionId ]
                    |> Option.map (fun sr -> sr, pr, pvr))
                |> Option.map (fun (sr, pr, pvr) ->

                    ({ Reference = psr.Reference
                       SubscriptionReference = sr.Reference
                       PipelineReference = pr.Reference
                       Pipeline = pr.Name
                       PipelineVersionReference = pvr.Reference
                       PipelineVersion = pvr.Version
                       ScheduleCron = psr.ScheduleCron }
                    : Models.ScheduleOverview)))
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching active schedules"
              DisplayMessage = "Error fetching active schedules"
              Exception = Some ex }
            |> FetchResult.Failure

    let schedule (ctx: MySqlContext) (logger: ILogger) (userReference: string) (scheduleReference: string) =
        try
            Fetch.user ctx userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
            |> FetchResult.chain (fun (ur, sr) psr -> ur, sr, psr) (Fetch.scheduleByReference ctx scheduleReference)
            |> FetchResult.merge (fun (ur, sr, psr) pvr -> ur, sr, pvr, psr) (fun (_, _, psr) ->
                Fetch.pipelineVersionById ctx psr.PipelineVersionId)
            |> FetchResult.merge (fun (ur, sr, pvr, psr) pr -> ur, sr, pr, pvr, psr) (fun (_, _, pvr, _) ->
                Fetch.pipelineById ctx pvr.PipelineId)
            |> FetchResult.toResult
            |> Result.bind (fun (ur, sr, pr, pvr, psr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.subscriptionMatches sr psr.SubscriptionId
                      Verification.subscriptionMatches sr pr.SubscriptionId
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr, psr))
            |> Result.map (fun (ur, sr, pr, pvr, psr) ->
                ({ Reference = psr.Reference
                   SubscriptionReference = sr.Reference
                   PipelineReference = pr.Reference
                   Pipeline = pr.Name
                   PipelineVersionReference = pvr.Reference
                   PipelineVersion = pvr.Version
                   ScheduleCron = psr.ScheduleCron
                   Active = sr.Active }
                : Models.ScheduleDetails))
            |> FetchResult.fromResult
        with ex ->
            { Message = "Unhandled exception while fetching active schedules"
              DisplayMessage = "Error fetching active schedules"
              Exception = Some ex }
            |> FetchResult.Failure

    let allSchedulePipelineRuns (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        