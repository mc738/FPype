namespace FPype.Configuration

open System.Text.Json
open Freql.Sqlite
open FsToolbox.Core
open Google.Protobuf.WellKnownTypes

module Import =

    [<AutoOpen>]
    module private Helpers =

        let getName (json: JsonElement) = Json.tryGetStringProperty "name" json

    let table (ctx: SqliteContext) (json: JsonElement) =
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
        |> Result.bind (Tables.addTransaction ctx)

    let tables (ctx: SqliteContext) (json: JsonElement list) = json |> List.map (table ctx)

    let query (ctx: SqliteContext) (json: JsonElement) =
        match getName json, Json.tryGetStringProperty "query" json with
        | Some n, Some q ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Query = q }: Queries.NewQuery)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `query` property"
        |> Result.bind (Queries.addTransaction ctx)

    let queries (ctx: SqliteContext) (json: JsonElement list) = json |> List.map (query ctx)

    let tableObjectMapper (ctx: SqliteContext) (json: JsonElement) =
        match getName json, Json.tryGetProperty "mapper" json with
        | Some n, Some m ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Mapper = m.ToString() }: TableObjectMappers.NewTableObjectMapper)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `mapper` property"
        |> Result.bind (TableObjectMappers.addRaw ctx)

