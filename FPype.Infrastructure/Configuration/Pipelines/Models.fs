namespace FPype.Infrastructure.Configuration.Pipelines

open System

[<AutoOpen>]
module Models =

    type NewPipeline =
        { Reference: string
          Name: string
          Version: NewPipelineVersion }

    and NewPipelineVersion =
        { Reference: string
          Description: string
          Actions: NewPipelineAction list }

    and NewPipelineAction =
        { Reference: string
          Name: string
          ActionType: string
          ActionData: string
          Step: int }

    type PipelineDetails =
        { Reference: string
          Name: string
          Versions: PipelineVersionDetails list }
    
    and PipelineVersionDetails =
        { PipelineReference: string
          VersionReference: string
          Name: string
          Description: string
          Version: int
          CreatedOn: DateTime
          Actions: PipelineActionDetails list }

    and PipelineActionDetails =
        { Reference: string
          Name: string
          ActionType: string
          ActionData: string
          Hash: string
          Step: int }

    type PipelineVersionOverview =
        { PipelineReference: string
          VersionReference: string
          Name: string
          Description: string
          Version: int }

    type PipelineOverview = { Reference: string; Name: string }
