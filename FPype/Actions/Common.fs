namespace FPype.Actions

[<AutoOpen>]
module Common =

    open FPype.Data.Store

    type TableResolver =
        { GetName: unit -> string

         }


    ()

    type PipelineAction =
        { Name: string
          Action: PipelineStore -> Result<PipelineStore, string> }

        static member Create(name, action) = { Name = name; Action = action }


    let createAction (name: string) (action: PipelineStore -> Result<PipelineStore, string>) =
        PipelineAction.Create(name, action)
