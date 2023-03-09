namespace FPype.Actions

open FPype.Data.Store

[<RequireQualifiedAccess>]
module Import =

    open System.IO
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store

    [<RequireQualifiedAccess>]
    module ``import-file`` =
        let name = "import_file"

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


    module ``chunk-file`` =
        let name = "chunk_file"

        type Parameters =
            { Path: string
              CollectionName: string
              ChunkSize: int }

        let run (parameters: Parameters) (store: PipelineStore) =
            match File.Exists parameters.Path, store.GetStateValue "__imports_path" with
            | true, Some importsPath ->
                let fileName = Path.GetFileNameWithoutExtension(parameters.Path)
                let ext = Path.GetExtension(parameters.Path)

                File.ReadLines(parameters.Path)
                |> Seq.chunkBySize parameters.ChunkSize
                |> Seq.iteri (fun i ls ->
                    let path = Path.Combine(importsPath, $"{fileName}___{i}{ext}")

                    File.WriteAllLines(path, ls)

                    store.AddDataSource($"{fileName}___{i}", "file", path, parameters.CollectionName))

                Ok store
            | false, _ -> Error $"File `{parameters.Path}` not found."
            | _, None -> Error "Imports path not found in store state."

        let createAction parameters = run parameters |> createAction name
        
    module ``unzip-files`` =
        
        let name = "unzip_files"
        
        
        
        let run () (store: PipelineStore) =
            
            //FsToolbox.Core.Compression.u
            
            ()
        
        
        ()