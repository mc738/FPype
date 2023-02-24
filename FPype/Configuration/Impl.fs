namespace FPype.Configuration

open System
open System.Globalization
open System.Text
open System.Text.Json
open FPype.Actions
open FPype.Core.Types
open FPype.Data.Grouping
open FPype.Data.Models
open FPype.Data.Store
open FPype.Configuration.Persistence
open Freql.Core.Common.Types
open Freql.Sqlite
open FsToolbox.Core

type PipelineAction =
    { Name: string
      Action: PipelineStore -> Result<PipelineStore, string> }

    static member Create(name, action) = { Name = name; Action = action }

module ImportHandlers =

    let parseDate (format: string) (v: string) =
        match DateTime.TryParseExact(v, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal) with
        | true, dt -> CoercionResult.Success <| Value.DateTime dt
        | false, _ -> CoercionResult.Failure "Wrong date format"

module Internal =

    [<AutoOpen>]
    module Common =
        let toJson (str: string) = JsonDocument.Parse(str).RootElement

        let blobToString (blob: BlobField) =
            blob.ToBytes() |> Encoding.UTF8.GetString

        let flattenResultList (r: Result<'a, string> list) =
            r
            |> List.fold
                (fun (s, err) r ->
                    match r with
                    | Ok v -> s @ [ v ], err
                    | Error e -> s, err @ [ e ])
                ([], [])
            |> fun (values, errors) ->
                match errors.IsEmpty with
                | true -> Ok values
                | false -> Error <| String.concat ", " errors


    module Tables =

        let createImportHandler (json: JsonElement) =
            match Json.tryGetStringProperty "handler" json with
            | Some "parse_date" ->
                Json.tryGetStringProperty "format" json
                |> Option.map ImportHandlers.parseDate
            | _ -> None

        let createColumns (ctx: SqliteContext) (tableName: string) =
            try
                Operations.selectTableColumnRecords ctx [ "WHERE table_name = @0" ] [ tableName ]
                |> List.sortBy (fun tcr -> tcr.ColumnIndex)
                |> List.map (fun tcr ->
                    let importHandler =
                        tcr.ImportHandler
                        |> Option.map (blobToString >> toJson)
                        |> Option.bind createImportHandler

                    ({ Name = tcr.Name
                       Type =
                         BaseType.FromId(tcr.DataType, tcr.Optional)
                         |> Option.defaultWith (fun _ ->
                             failwith $"Invalid type for column `{tcr.Name}`: `{tcr.DataType}`")
                       ImportHandler = importHandler }: TableColumn))
                |> Ok
            with
            | exn -> Error $"Error creating table columns: {exn}"

        let getTable (ctx: SqliteContext) (tableName: string) =
            Operations.selectTableModelRecord ctx [ "WHERE name = @0" ] [ tableName ]

        let tryCreateTableModel (ctx: SqliteContext) (tableName: string) =
            getTable ctx tableName
            |> Option.map (fun t ->
                createColumns ctx t.Name
                |> Result.map (fun tc ->
                    ({ Name = t.Name
                       Columns = tc
                       Rows = [] }: TableModel)))
            |> Option.defaultValue (Error $"Table `{tableName}` not found")

        let loadTableFromJson (ctx: SqliteContext) (propertyName: string) (json: JsonElement) =
            Json.tryGetStringProperty propertyName json
            |> Option.map (tryCreateTableModel ctx)
            |> Option.defaultValue (Error $"`{propertyName}` property missing")

    module Queries =

        let getQuery (ctx: SqliteContext) (queryName: string) =
            Operations.selectQueriesRecord ctx [ "WHERE name = @0" ] [ queryName ]
            |> Option.map (fun qr -> qr.QueryBlob |> blobToString)

        let tryCreate (ctx: SqliteContext) (json: JsonElement) =
            Json.tryGetStringProperty "query" json
            |> Option.bind (getQuery ctx)


    module ObjectMappers =

        let createTableColumnSource (json: JsonElement) =
            match Json.tryGetIntProperty "index" json with
            | Some i -> PropertySource.TableColumn i |> Ok
            | None -> Error "Missing `index` property"

        let rec createTableSource (ctx: SqliteContext) (json: JsonElement) (properties: PropertyMap list) =
            match Json.tryGetStringProperty "name" json,
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


        (*
        let rec createPropertySource (ctx: SqliteContext) (json: JsonElement) =
            match tryGetStringProperty "type" json with
            | Some sourceType when sourceType = "table_column" -> createTableColumnSource json
            | Some sourceType when sourceType = "table" ->
                match tryGetArrayProperty "properties" json with
                | Some properties ->
                    properties |> List.map (createPropertySource ctx)
                    |> flattenResultList
                    |> Result.bind (createTableSource ctx json)
                    //|> flattenResultList
                    //|> Result.map (fun p -> ({}: RelatedObjectTableSource))
                    |> ignore
                    Error ""
                | None -> Error "Missing `properties` array"
                //createTableSource ctx json
            | Some sourceType -> Error $"Unknown property source type `{sourceType}`"
            | None -> Error "Missing `type` property"
        *)

        let rec tryCreatePropertyMap (ctx: SqliteContext) (json: JsonElement) =
            // TODO if the type is source `table` then rec.
            match Json.tryGetStringProperty "name" json, Json.tryGetProperty "source" json with
            | Some name, Some source ->
                let source =
                    match Json.tryGetStringProperty "type" source with
                    | Some t when t = "table_column" -> createTableColumnSource source

                    | Some t when t = "table" ->
                        Json.tryGetArrayProperty "properties" source
                        |> Option.map (fun ps ->
                            ps
                            |> List.map (tryCreatePropertyMap ctx)
                            |> flattenResultList)
                        |> Option.defaultValue (Error "Missing `properties` element")
                        |> Result.bind (createTableSource ctx source)
                    | Some t -> Error $"Unknown source type `{t}`"
                    | None -> Error "Missing `type` property"

                source
                |> Result.map (fun s -> ({ Name = name; Source = s }: PropertyMap))
            | None, _ -> Error "Missing `name` property"
            | _, None -> Error "Missing `type` property"

        let tryCreateObjectMapper (ctx: SqliteContext) (json: JsonElement) =

            match Json.tryGetStringProperty "objectName" json,
                  Queries.tryCreate ctx json,
                  Tables.loadTableFromJson ctx "table" json,
                  Json.tryGetArrayProperty "properties" json
                with
            | Some objectName, Some query, Ok table, Some properties ->

                match properties
                      |> List.map (tryCreatePropertyMap ctx)
                      |> flattenResultList
                    with
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


        let load (ctx: SqliteContext) (mapperName: string) =

            Operations.selectObjectMapperRecord ctx [ "WHERE name = @0" ] [ mapperName ]
            |> Option.map (fun omr ->
                let json =
                    omr.Mapper |> blobToString |> toJson

                tryCreateObjectMapper ctx json)
            |> Option.defaultValue (Error $"Could not find object mapper `{mapperName}`")


    module Groups =

        /// Example json:
        /// {
        ///    "type": "month",
        ///    "start": "2014-01-01",
        ///    "length": 48,
        ///    "fieldName": "date"
        ///    "label": "Date"
        /// }
        let createMonthGroups (elements: Map<string, JsonElement>) =
            match elements.TryFind "start"
                  |> Option.bind Json.tryGetDateTime,
                  elements.TryFind "length" |> Option.bind Json.tryGetInt,
                  elements.TryFind "fieldName"
                  |> Option.map Json.getString,
                  elements.TryFind "label" |> Option.map Json.getString
                with
            | Some start, Some length, Some fieldName, Some label ->
                DateGroup.GenerateMonthGroups(start, length)
                |> fun dg ->
                    ({ FieldName = fieldName
                       Label = label
                       Groups = dg }: DateGroups)
                |> Ok
            | None, _, _, _ -> Error "Missing start property"
            | _, None, _, _ -> Error "Missing length property"
            | _, _, None, _ -> Error "Missing fieldName property"
            | _, _, _, None -> Error "Missing label property"

        let createDateGroup (properties: JsonProperty list option) =
            properties
            |> Option.map Json.propertiesToMap
            |> Option.map (fun elements ->
                match elements.TryFind "type"
                      |> Option.map (fun el -> el.GetString())
                      |> Option.defaultValue ""
                    with
                | "months" -> createMonthGroups elements
                | t -> Error $"Unknown group type: `{t}`")
            |> Option.defaultValue (Error "Missing dateGroup object")

    module Actions =
        let createImportFileAction (json: JsonElement) =
            match Json.tryGetStringProperty "path" json, Json.tryGetStringProperty "name" json with
            | Some path, Some name -> Ok(Import.file path name)
            | None, _ -> Error "Missing path property"
            | _, None -> Error "Missing name property"

        let createParseCsvAction (ctx: SqliteContext) (json: JsonElement) =
            match Json.tryGetStringProperty "source" json, Json.tryGetStringProperty "table" json with
            | Some source, Some tableName ->
                Tables.getTable ctx tableName
                |> Option.map (fun t ->
                    Tables.createColumns ctx t.Name
                    |> Result.map (fun tc -> Extract.parseCsv source tc t.Name))
                |> Option.defaultValue (Error $"Table `{tableName}` not found")
            | None, _ -> Error "Missing source property"
            | _, None -> Error "Missing table property"

        let createAggregateAction (ctx: SqliteContext) (json: JsonElement) =
            match Queries.tryCreate ctx json, Json.tryGetStringProperty "table" json with
            | Some query, Some tableName ->
                Tables.getTable ctx tableName
                |> Option.map (fun t ->
                    Tables.createColumns ctx t.Name
                    |> Result.map (fun tc -> Transform.aggregate t.Name tc query []))
                |> Option.defaultValue (Error $"Table `{tableName}` not found")
            | None, _ -> Error "Missing query property"
            | _, None -> Error "Missing table property"


        let createAggregateByCategoryAndDateAction (ctx: SqliteContext) (json: JsonElement) =
            match Json.tryGetElementsProperty "dateGroups" json
                  |> Groups.createDateGroup,
                  Json.tryGetStringProperty "categoryField" json,
                  Json.tryGetStringProperty "table" json,
                  Queries.tryCreate ctx json
                with
            | Ok dateGroup, Some categoryField, Some tableName, Some query ->
                Tables.getTable ctx tableName
                |> Option.map (fun t ->
                    Tables.createColumns ctx t.Name
                    |> Result.map (fun tc ->
                        Transform.aggregateByDateAndCategory dateGroup categoryField tc t.Name query))
                |> Option.defaultValue (Error $"Table `{tableName}` not found")
            | Error e, _, _, _ -> Error $"Error creating date groups: {e}"
            | _, None, _, _ -> Error "Missing category field"
            | _, _, None, _ -> Error "Missing table field"
            | _, _, _, None -> Error "Missing query field"

        let createAggregateByDateAction (ctx: SqliteContext) (json: JsonElement) =
            match Json.tryGetElementsProperty "dateGroups" json
                  |> Groups.createDateGroup,
                  Json.tryGetStringProperty "table" json,
                  Queries.tryCreate ctx json
                with
            | Ok dateGroup, Some tableName, Some query ->
                Tables.getTable ctx tableName
                |> Option.map (fun t ->
                    Tables.createColumns ctx t.Name
                    |> Result.map (fun tc -> Transform.aggregateByDate dateGroup tc t.Name query))
                |> Option.defaultValue (Error $"Table `{tableName}` not found")
            | Error e, _, _ -> Error $"Error creating date groups: {e}"
            | _, None, _ -> Error "Missing table field"
            | _, _, None -> Error "Missing query field"

        let createMapToObjectAction (ctx: SqliteContext) (json: JsonElement) =
            Json.tryGetStringProperty "mapper" json
            |> Option.map (fun mn -> ObjectMappers.load ctx mn)
            |> Option.defaultValue (Error "Missing `mapper` property")
            |> Result.map (fun m -> Transform.mapToObject m)
            
    let createAction (ctx: SqliteContext) (action: Records.PipelineAction) =
        let actionData =
            action.ActionBlob |> blobToString |> toJson

        let build fn =
            fn
            |> Result.map (fun a -> PipelineAction.Create(action.Name, a))

        try
            match action.ActionType with
            | "import_file" -> Actions.createImportFileAction actionData
            | "parse_csv" -> Actions.createParseCsvAction ctx actionData
            | "aggregate" -> Actions.createAggregateAction ctx actionData
            | "aggregate_by_date_and_category" -> Actions.createAggregateByCategoryAndDateAction ctx actionData
            | "aggregate_by_date" -> Actions.createAggregateByDateAction ctx actionData
            | "map_to_object" -> Actions.createMapToObjectAction ctx actionData
            | _ -> Error $"Unknown action type: `{action.ActionType}`"
            |> build
        with
        | exn -> Error $"Failed to create action. Exception: {exn.Message}"

    let createActions (ctx: SqliteContext) (pipelineId: string) =
        Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_id = @0" ] [ pipelineId ]
        |> List.sortBy (fun pa -> pa.Step)
        |> List.map (createAction ctx)

type ConfigurationStore(ctx: SqliteContext) =

    static member Load(path) =
        SqliteContext.Open path |> ConfigurationStore

    member pc.GetTable(tableName) =
        Internal.Tables.getTable ctx tableName
        |> Option.map (fun t ->
            Internal.Tables.createColumns ctx t.Name
            |> Result.map (fun tc ->
                ({ Name = t.Name
                   Columns = tc
                   Rows = [] }: TableModel)))
        |> Option.defaultValue (Error $"Table `{tableName}` not found")

    member pc.GetQuery(queryName) = Internal.Queries.getQuery ctx queryName

    member pc.CreateActions(pipelineId) =
        Internal.createActions ctx pipelineId
        |> Internal.Common.flattenResultList
        |> Result.mapError (fun msg -> $"Could not create actions: {msg}")

    member pc.GetObjectMapper(name) = Internal.ObjectMappers.load ctx name

