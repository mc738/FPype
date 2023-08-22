namespace FPype.Infrastructure.Pipelines

open System

module Models =
    
    type PipelineRunDetails =
        {
            RunId: string
            SubscriptionReference: string
            PipelineReference: string
            PipelineName: string
            PipelineVersion: int
            PipelineVersionReference: string
            QueuedOn: DateTime 
            StartedOn: DateTime option
            CompletedOn: DateTime option
            WasSuccessful: bool option
            BasePath: string
            RunByReference: string
            RunByName: string
        }

