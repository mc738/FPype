open System
open FPype.App
open FPype.App.Actions
open FsToolbox.AppEnvironment.Args
open FsToolbox.AppEnvironment.Args.Mapping

let options =
    Environment.GetCommandLineArgs()
    |> List.ofArray
    |> ArgParser.tryGetOptions<Options>

let result =
    match options with
    | Ok o ->
        match o with
        | RunPipeline opts -> runPipeline opts |> Result.map (fun _ -> ())
        | ImportPipeline opts -> importPipeline opts
    | Error e -> Error $"Error parsing options: `{e}`"

match result with
| Ok s -> printf "Pipeline complete."
| Error e ->
    printf $"{e}"
    exit -1
