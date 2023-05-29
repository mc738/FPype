namespace FPype.App

open System.IO
open FPype
open FPype.Configuration
open FsToolbox.AppEnvironment.Args.Mapping

module Actions =

    type Options =
        | [<CommandValue("import")>] ImportPipeline of ImportPipelineOptions
        | [<CommandValue("run")>] RunPipeline of RunPipelineOptions

    and RunPipelineOptions =
        { [<ArgValue("-c", "--config")>]
          ConfigurationPath: string
          [<ArgValue("-p", "--path")>]
          BasePath: string
          [<ArgValue("-n", "--name")>]
          PipelineName: string
          [<ArgValue("-v", "--version")>]
          PipelineVersion: int option }

    and ImportPipelineOptions =
        { [<ArgValue("-c", "--config")>]
          ConfigurationPath: string
          [<ArgValue("-p", "--path")>]
          Path: string }

    let importPipeline (options: ImportPipelineOptions) =
        let cfg =
            ConfigurationStore.Initialize options.ConfigurationPath

        cfg.ImportFromFile <| options.Path
        
    let runPipeline (options: RunPipelineOptions) =
        let cfg = ConfigurationStore.Load options.ConfigurationPath

        PipelineContext.Create(
            cfg,
            options.BasePath,
            true,
            options.PipelineName,
            (match options.PipelineVersion with
             | Some v -> ItemVersion.Specific 1
             | None -> ItemVersion.Latest),
            Map.empty
        )
        |> Result.bind (fun ctx -> ctx.Run())
