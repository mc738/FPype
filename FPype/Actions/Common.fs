namespace FPype.Actions

open System
open System.IO
open System.Text.Json
open FPype.Data.Store
open FsToolbox.Extensions
open FPype.Data

[<AutoOpen>]
module Common =

    open FPype.Data.Store

    type TableResolver =
        { GetName: unit -> string }

    type PipelineAction =
        { Name: string
          StepName: string
          Action: PipelineStore -> Result<PipelineStore, string> }

        static member Create(name, stepName, action) = { Name = name; StepName = stepName; Action = action }

    let createAction (name: string) (stepName: string) (action: PipelineStore -> Result<PipelineStore, string>) =
        PipelineAction.Create(name, stepName, action)

    let getDataSourceAsString (store: PipelineStore) (dataSource: DataSource) =
        match DataSourceType.Deserialize dataSource.Type with
            | Some DataSourceType.File ->
                try
                    File.ReadAllText dataSource.Uri |> Ok
                with exn ->
                    Error $"Could not load file `{dataSource.Uri}`: {exn.Message}"
            | Some DataSourceType.Artifact ->
                store.GetArtifact(dataSource.Name)
                |> Option.map (fun a -> String.FromUtfBytes(a.Data.ToBytes()) |> Ok)
                |> Option.defaultWith (fun _ -> Error $"Artifact `{dataSource.Name}` not found.")
            | _ -> Error $"Unsupported source type: `{dataSource.Type}`"
    
    let getDataSourceAsLines (store: PipelineStore) (dataSource: DataSource) =
        match DataSourceType.Deserialize dataSource.Type with
            | Some DataSourceType.File ->
                try
                    File.ReadAllLines dataSource.Uri |> Ok
                with exn ->
                    Error $"Could not load file `{dataSource.Uri}`: {exn.Message}"
            | Some DataSourceType.Artifact ->
                store.GetArtifact(dataSource.Name)
                |> Option.map (fun a -> String.FromUtfBytes(a.Data.ToBytes()).Split(Environment.NewLine) |> Ok)
                |> Option.defaultWith (fun _ -> Error $"Artifact `{dataSource.Name}` not found")
            | None -> Error $"Unsupported source type: `{dataSource.Type}`"
    
    let getDataSourceAsStringByName (store: PipelineStore) (sourceName: string) =
        store.GetDataSource sourceName
        |> Option.map (getDataSourceAsString store)
        |> Option.defaultWith (fun _ -> Error $"Data source `{sourceName}` not found")

    let getDataSourceAsLinesByName (store: PipelineStore) (sourceName: string) =
        store.GetDataSource sourceName
        |> Option.map (getDataSourceAsLines store)
        |> Option.defaultWith (fun _ -> Error $"Data source `{sourceName}` not found")
        
    let getDataSourceAsFileUri (store: PipelineStore) (sourceName: string) (createTempFile: bool) =
        store.GetDataSource sourceName
        |> Option.map (fun ds ->
            match DataSourceType.Deserialize  ds.Type with
            | Some DataSourceType.File -> ds.Uri |> Ok
            | Some DataSourceType.Artifact ->
                match createTempFile with
                | true ->
                    match store.GetArtifact(ds.Name) with
                    | Some a ->
                        let tmpPath = store.GetTmpPath() |> Option.defaultValue store.DefaultTmpPath
                        let path = Path.Combine(tmpPath, $"{a.Name}.{a.Type}")
                        
                        match File.Exists path with
                        | true ->
                            // If the file already exists create a new one.
                            // This is basically a no-op.
                            Ok ()
                        | false ->
                            try
                                use fs = new FileStream(path, FileMode.Create)
                                a.Data.Value.CopyTo(fs)
                                fs.Flush() |> Ok
                            with
                            | exn -> Error $"Error creating file `{path}`: {exn.Message}"
                        |> Result.map (fun _ -> path)
                    | None -> Error $"Artifact `{ds.Name}` not found"
                | false -> Error "Data source is an artifact and `createTempFile` is set to false"
            | None -> Error "Unknown data source type")
        |> Option.defaultWith (fun _ -> Error $"Data source `{sourceName}` not found")
        
    let toJsonElement (str: string) =
        try
            (JsonDocument.Parse str).RootElement |> Ok
        with
        | exn -> Error $"Failed to deserialize json element. Error: {exn.Message}"