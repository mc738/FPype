namespace FPype.Infrastructure.Scheduling

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
        
    let schedule (ctx: MySqlContext) (userReference: string) (schedule: NewSchedule) =
        ctx.ExecuteInTransaction(fun t ->
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) pvr -> ur, sr, pvr) (Fetch.pipelineVersionByReference t schedule.PipelineVersionReference)
            |> FetchResult.merge (fun (ur, sr, pvr) pr -> ur, sr, pr, pvr) (fun (ur, sr, pvr) -> Fetch.pipelineById t pvr.PipelineId)
            )
