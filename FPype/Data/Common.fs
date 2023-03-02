namespace FPype.Data

[<AutoOpen>]
module Common =


    type PipelineArg =
        { Name: string
          Required: bool
          DefaultValue: string option }
