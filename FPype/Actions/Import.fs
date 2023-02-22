namespace FPype.Actions

[<RequireQualifiedAccess>]
module Import =

    open System.IO    
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store
     
    let file (path: string) (name: string) (store: PipelineStore) =
        match File.Exists path, store.GetStateValue "__imports_path" with
        | true, Some importsPath ->
            //store.GetState()
            //let fi = FileInfo(path)
            let fileName = Path.GetFileName(path)
            let newPath = Path.Combine(importsPath, fileName)
            File.Copy(path, newPath)
            store.AddDataSource(name, "file", newPath, "imports")
            Ok store
        | false, _ -> Error $"File `{path}` not found."
        | _, None -> Error "Imports path not found in store state."