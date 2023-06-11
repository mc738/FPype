namespace FPype.Infrastructure.Configuration.Pipelines

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

    type PipelineVersionDetails =
        { PipelineReference: string
          VersionReference: string
          Name: string
          Description: string
          Version: int
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
