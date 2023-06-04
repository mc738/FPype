﻿namespace FPype.Infrastructure.Services

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
           QueuedOn = DateTime.UtcNow.AddMinutes(-10) 
           StartedOn = Some <| DateTime.UtcNow.AddMinutes(-5)
           CompletedOn = Some DateTime.UtcNow
           WasSuccessful = Some true
           BasePath = "D:\\DataSets\\sp_500\\pipelines\\v10\\pipeline\\runs"
           RunBy = 1 }
        : Records.PipelineRunItem)
        |> FetchResult.Success