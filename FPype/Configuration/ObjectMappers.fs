namespace FPype.Configuration

module ObjectMappers =

    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FPype.Configuration.Persistence
    open FPype.Data.Models

    let createTableColumnSource (json: JsonElement) =
        match Json.tryGetIntProperty "index" json with
        | Some i -> PropertySource.TableColumn i |> Ok
        | None -> Error "Missing `index` property"

    let rec createTableSource (ctx: SqliteContext) (json: JsonElement) (properties: PropertyMap list) =
        match
            Json.tryGetStringProperty "name" json,
            Tables.loadTableFromJson ctx "table" json,
            Queries.tryCreate ctx json,
            Json.tryGetIntArrayProperty "parameterIndexes" json
        with
        | Some name, Ok table, Some query, Some parameterIndexes ->
            ({ Name = name
               Query = query
               Table = table
               ParameterIndexes = parameterIndexes
               Properties = properties }: RelatedObjectTableSource)
            |> PropertySource.Table
            |> Ok
        | None, _, _, _ -> Error "Missing `name` property"
        | _, Error e, _, _ -> Error e
        | _, _, None, _ -> Error "Missing query"
        | _, _, _, None -> Error "Missing `parameterIndexes` property"

    let rec tryCreatePropertyMap (ctx: SqliteContext) (json: JsonElement) =
        // TODO if the type is source `table` then rec.
        match Json.tryGetStringProperty "name" json, Json.tryGetProperty "source" json with
        | Some name, Some source ->
            let source =
                match Json.tryGetStringProperty "type" source with
                | Some t when t = "table_column" -> createTableColumnSource source

                | Some t when t = "table" ->
                    Json.tryGetArrayProperty "properties" source
                    |> Option.map (fun ps -> ps |> List.map (tryCreatePropertyMap ctx) |> flattenResultList)
                    |> Option.defaultValue (Error "Missing `properties` element")
                    |> Result.bind (createTableSource ctx source)
                | Some t -> Error $"Unknown source type `{t}`"
                | None -> Error "Missing `type` property"

            source |> Result.map (fun s -> ({ Name = name; Source = s }: PropertyMap))
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `type` property"

    let tryCreateObjectMapper (ctx: SqliteContext) (json: JsonElement) =

        match
            Json.tryGetStringProperty "objectName" json,
            Queries.tryCreate ctx json,
            Tables.loadTableFromJson ctx "table" json,
            Json.tryGetArrayProperty "properties" json
        with
        | Some objectName, Some query, Ok table, Some properties ->

            match properties |> List.map (tryCreatePropertyMap ctx) |> flattenResultList with
            | Ok p ->
                ({ ObjectName = objectName
                   Table = table
                   Query = query
                   Properties = p }: ObjectTableMap)
                |> Ok
            | Error e -> Error $"Failed to create properties. Error: {e}"
        | None, _, _, _ -> Error "Missing `objectName` property"
        | _, None, _, _ -> Error "Could not load query"
        | _, _, Error e, _ -> Error e
        | _, _, _, None -> Error "Missing `properties` array"

    let getVersion (ctx: SqliteContext) (mapperName: string) (version: int) =
        Operations.selectTableModelVersionRecord
            ctx
            [ "WHERE object_mapper = @0 AND version = @1;" ]
            [ mapperName; version ]

    
    let 

    let load (ctx: SqliteContext) (mapperName: string) =

        Operations.selectObjectMapperRecord ctx [ "WHERE name = @0" ] [ mapperName ]
        |> Option.map (fun omr ->
            let json = omr.Mapper |> blobToString |> toJson

            tryCreateObjectMapper ctx json)
        |> Option.defaultValue (Error $"Could not find object mapper `{mapperName}`")
