namespace FPype.Infrastructure.Services

open System
open Microsoft.Extensions.Logging
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results

type PipelineService(ctx: MySqlContext, log: ILogger<PipelineService>) =

    member _.GetRunItem(userReference: string, runId: string) =

        ({ Id = 1
           Reference = runId
           SubscriptionId = 1
           PipelineVersionId = 1
           StartedOn = DateTime.UtcNow.AddMinutes(-5)
           CompletedOn = DateTime.UtcNow
           WasSuccessful = true
           BasePath = "D:\\DataSets\\sp_500\\pipelines\\v10\\pipeline\\runs"
           RunBy = 1 }
        : Records.PipelineRunItem)
        |> FetchResult.Success
