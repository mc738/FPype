namespace FPype.Infrastructure.Scheduling

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
                      Verification.userSubscriptionMatches ur pr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr))
            // Create
            |> Result.map (fun (ur, sr, pr, pvr) ->
                let scheduleId =
                    ({ Reference = schedule.Reference
                       SubscriptionId = sr.Id
                       PipelineVersionId = pvr.Id
                       ScheduleCron = schedule.ScheduleCron
                       Active = true }
                    : Parameters.NewPipelineSchedule)
                    |> Operations.insertPipelineSchedule t
                    |> int

                [ Events.ScheduleEvent.ScheduleCreated
                      { Reference = schedule.Reference
                        ScheduleCron = schedule.ScheduleCron }
                  Events.ScheduleEvent.ScheduleActivated { Reference = schedule.Reference } ]
                |> FPype.Infrastructure.Scheduling.Events.addEvents t logger sr.Id ur.Id (getTimestamp ())
                |> ignore))
        |> toActionResult "Create schedule"
