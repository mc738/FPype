namespace FPype.Configuration

open System.Text.Json
open Freql.Sqlite
open FsToolbox.Core
open Google.Protobuf.WellKnownTypes
open Microsoft.FSharp.Core

module Import =

    [<AutoOpen>]
    module private Helpers =

        let getName (json: JsonElement) = Json.tryGetStringProperty "name" json

        let getType (json: JsonElement) = Json.tryGetStringProperty "type" json

        let getPipeline (json: JsonElement) =
            Json.tryGetStringProperty "pipeline" json

    let table (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json with
        | Some name ->
            Json.tryGetArrayProperty "columns" json
            |> Option.map (fun cs ->
                cs
                |> List.mapi (fun i c ->
                    match Json.tryGetStringProperty "name" c, Json.tryGetStringProperty "type" c with
                    | Some n, Some t ->
                        ({ Name = n
                           DataType = t
                           Optional = Json.tryGetBoolProperty "optional" c |> Option.defaultValue false
                           ImportHandler = Json.tryGetProperty "importHandler" c |> Option.map (fun ih -> ih.ToString()) }: Tables.NewColumn)
                        |> Ok
                    | None, _ -> Error $"Column `{i}` missing `name` property"
                    | _, None -> Error $"Column `{i}` missing `name` property")
                |> flattenResultList)
            |> Option.defaultWith (fun _ -> Error "Missing `columns` property")
            |> Result.map (fun columns ->
                ({ Id = IdType.FromJson json
                   Name = name
                   Version = ItemVersion.FromJson json
                   Columns = columns }: Tables.NewTable))
        | None -> Error "Missing `name` property"
        |> Result.bind (
            match transaction with
            | true -> Tables.addTransaction ctx
            | false -> Tables.add ctx
        )

    let tables (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (table ctx transaction)

    let query (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json, Json.tryGetStringProperty "query" json with
        | Some n, Some q ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Query = q }: Queries.NewQuery)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `query` property"
        |> Result.bind (
            match transaction with
            | true -> Queries.addTransaction ctx
            | false -> Queries.add ctx
        )

    let queries (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (query ctx transaction)

    let tableObjectMapper (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json, Json.tryGetProperty "mapper" json with
        | Some n, Some m ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Mapper = m.ToString() }: TableObjectMappers.NewTableObjectMapper)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `mapper` property"
        |> Result.bind (
            match transaction with
            | true -> TableObjectMappers.addRawTransaction ctx
            | false -> TableObjectMappers.addRaw ctx
        )

    let tableObjectMappers (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (tableObjectMapper ctx transaction)

    let pipelineAction (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match
            Json.tryGetStringProperty "pipeline" json, getName json, getType json, Json.tryGetProperty "data" json
        with
        | Some p, Some n, Some t, Some d ->
            ({ Id = IdType.FromJson json
               Name = n
               Pipeline = p
               Version = ItemVersion.FromJson json
               ActionType = t
               ActionData = d.ToString()
               Step = Json.tryGetIntProperty "step" json }: Actions.NewPipelineAction)
            |> Ok
        | None, _, _, _ -> Error "Missing `pipeline` property"
        | _, None, _, _ -> Error "Missing `name` property"
        | _, _, None, _ -> Error "Missing `type` property"
        | _, _, _, None -> Error "Missing `action` property"
        |> Result.bind (
            match transaction with
            | true -> Actions.addTransaction ctx
            | false -> Actions.add ctx
        )

    let pipelineActions (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (pipelineAction ctx transaction)

    let pipeline (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json with
        | Some n ->
            ({ Id = IdType.FromJson json
               Name = n
               Description = Json.tryGetStringProperty "description" json |> Option.defaultValue ""
               Version = ItemVersion.FromJson json }: Pipelines.NewPipeline)
            |> fun pipeline ->
                match transaction with
                | true -> Pipelines.addTransaction ctx pipeline
                | false -> Pipelines.add ctx pipeline
        | None -> Error "Missing `name` property"

    let pipelineArg (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json, getPipeline json with
        | Some n, Some p ->
            ({ Id = IdType.FromJson json
               Pipeline = p
               Name = n
               Version = ItemVersion.FromJson json
               Required = Json.tryGetBoolProperty "required" json |> Option.defaultValue false
               DefaultValue = Json.tryGetStringProperty "default" json }: Pipelines.NewPipelineArg)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `pipeline` property"
        |> Result.bind (
            match transaction with
            | true -> Pipelines.addPipelineArgTransaction ctx
            | false -> Pipelines.addPipelineArg ctx
        )

    let pipelineArgs (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (pipelineArg ctx transaction)
