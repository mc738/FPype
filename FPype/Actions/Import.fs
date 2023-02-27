namespace FPype.Actions

[<RequireQualifiedAccess>]
module Import =

    open System.IO
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store

    [<RequireQualifiedAccess>]
    module ``import-file`` =
        let name = "import-file"

        type Parameters = { Path: string; Name: string }

        let run (parameters: Parameters) (store: PipelineStore) =
            match File.Exists parameters.Path, store.GetStateValue "__imports_path" with
            | true, Some importsPath ->
                //store.GetState()
                //let fi = FileInfo(path)
                let fileName = Path.GetFileName(parameters.Path)
                let newPath = Path.Combine(importsPath, fileName)
                File.Copy(parameters.Path, newPath)
                store.AddDataSource(parameters.Name, "file", newPath, "imports")
                Ok store
            | false, _ -> Error $"File `{parameters.Path}` not found."
            | _, None -> Error "Imports path not found in store state."

        let createAction parameters = run parameters |> createAction name
