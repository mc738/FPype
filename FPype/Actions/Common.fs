namespace FPype.Actions

open System
open System.IO
open FPype.Data.Store
open FsToolbox.Extensions
open FPype.Data


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
                |> Option.defaultWith (fun _ -> Error $"Artifact `{dataSource.Name}` not found.")
            | None -> Error $"Unsupported source type: `{dataSource.Type}`"
    
    let getDataSourceAsStringByName (store: PipelineStore) (sourceName: string) =
        store.GetDataSource sourceName
        |> Option.map (getDataSourceAsString store)
        |> Option.defaultWith (fun _ -> Error $"Data source `{sourceName}` not found.")

    let getDataSourceAsLinesByName (store: PipelineStore) (sourceName: string) =
        store.GetDataSource sourceName
        |> Option.map (getDataSourceAsLines store)
        |> Option.defaultWith (fun _ -> Error $"Data source `{sourceName}` not found.")