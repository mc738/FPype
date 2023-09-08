namespace FPype.Infrastructure.Scheduling

[<RequireQualifiedAccess>]
module UpdateOperations =
    
    open Microsoft.Extensions.Logging
    open FPype.Infrastructure.Scheduling.Models
    open Freql.MySql
    open FPype.Infrastructure.Configuration.Common
    open FsToolbox.Core.Results
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    
    let schedule (ctx: MySqlContext) (logger: ILogger) (userReference: string) (update: UpdateSchedule) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline t pipelineReference)
            |> FetchResult.merge (fun (ur, sr, pr) pvr -> ur, sr, pr, pvr) (fun (_, _, pr) ->
                Fetch.pipelineLatestVersion t pr.Id)
            |> FetchResult.toResult
            )
    

