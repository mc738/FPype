namespace FPype.Configuration

open System.Text.Json
open FPype.Configuration.Persistence

module Actions =

    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FPype.Actions

    module Import =

        module ``import-file`` =
            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "path" json, Json.tryGetStringProperty "name" json with
                | Some path, Some name ->
                    ({ Path = path; Name = name }: Import.``import-file``.Parameters)
                    |> Import.``import-file``.createAction
                    |> Ok
                | None, _ -> Error "Missing path property"
                | _, None -> Error "Missing name property"

        let names = [ Import.``import-file``.name ]

        let all = [ Import.``import-file``.name, ``import-file``.deserialize ]

    module Extract =

        module ``parse-csv`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetStringProperty "source" json, Json.tryGetProperty "table" json |> TableVersion.TryCreate
                with
                | Some source, Ok tableVersion ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ DataSource = source; Table = t }: Extract.``parse-csv``.Parameters)
                        |> Extract.``parse-csv``.createAction)
                | None, _ -> Error "Missing source property"
                | _, Error e -> Error $"Error creating table: {e}"

        let names = [ Extract.``parse-csv``.name ]

        let all (ctx: SqliteContext) =
            [ Extract.``parse-csv``.name, ``parse-csv``.deserialize ctx ]

    module Transform =

        module ``aggregate`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Queries.tryCreate ctx json, Json.tryGetProperty "table" json |> TableVersion.TryCreate with
                | Some query, Ok tableVersion ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ Table = t
                           Sql = query
                           Parameters = [] }: Transform.``aggregate``.Parameters)
                        |> Transform.aggregate.createAction)
                | None, _ -> Error "Missing `query` property"
                | _, Error e -> Error $"Error creating table: {e}"

        module ``aggregate-by-date-and-category`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
                    Json.tryGetStringProperty "categoryField" json,
                    Json.tryGetProperty "table" json |> TableVersion.TryCreate,
                    Queries.tryCreate ctx json
                with
                | Ok dateGroup, Some categoryField, Ok tableVersion, Some query ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ Table = t
                           SelectSql = query
                           DateGroups = dateGroup
                           CategoryField = categoryField }: Transform.``aggregate-by-date-and-category``.Parameters)
                        |> Transform.``aggregate-by-date-and-category``.createAction)
                | Error e, _, _, _ -> Error $"Error creating date groups: {e}"
                | _, None, _, _ -> Error "Missing `category` field"
                | _, _, Error e, _ -> Error $"Error creating table: {e}"
                | _, _, _, None -> Error "Missing `query` field"

        module ``aggregate-by-date`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
                    Json.tryGetProperty "table" json |> TableVersion.TryCreate,
                    Queries.tryCreate ctx json
                with
                | Ok dateGroup, Ok tableVersion, Some query ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ Table = t
                           SelectSql = query
                           DateGroups = dateGroup }: Transform.``aggregate-by-date``.Parameters)
                        |> Transform.``aggregate-by-date``.createAction)
                | Error e, _, _ -> Error $"Error creating date groups: {e}"
                | _, Error e, _ -> Error $"Error creating table: {e}"
                | _, _, None -> Error "Missing query field"

        module ``map-to-object`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "mapper" json, Json.tryGetIntProperty "version" json with
                | Some m, Some v -> ItemVersion.Specific v |> TableObjectMappers.load ctx m
                | Some m, None -> ItemVersion.Latest |> TableObjectMappers.load ctx m
                | None, _ -> Error "Missing `mapper` property"
                |> Result.map Transform.``map-to-object``.createAction

        let names =
            [ Transform.``aggregate``.name
              Transform.``aggregate-by-date-and-category``.name
              Transform.``aggregate-by-date``.name
              Transform.``map-to-object``.name ]

        let all (ctx: SqliteContext) =
            [ Transform.``aggregate``.name, ``aggregate``.deserialize ctx
              Transform.``aggregate-by-date-and-category``.name, ``aggregate-by-date-and-category``.deserialize ctx
              Transform.``aggregate-by-date``.name, ``aggregate-by-date``.deserialize ctx
              Transform.``map-to-object``.name, ``map-to-object``.deserialize ctx ]

    module Load =

        let names = []

        let all (ctx: SqliteContext) = []

    module Export =

        let names = []

        let all (ctx: SqliteContext) = []

    let names =
        [ yield! Import.names
          yield! Extract.names
          yield! Transform.names
          yield! Load.names
          yield! Export.names ]

    let all (ctx: SqliteContext) =
        [ yield! Import.all
          yield! Extract.all ctx
          yield! Transform.all ctx
          yield! Load.all ctx
          yield! Export.all ctx ]

    let createAction
        (actionsMap: Map<string, JsonElement -> Result<PipelineAction, string>>)
        (action: Records.PipelineAction)
        =
        let actionData = action.ActionBlob |> blobToString |> toJson

        try
            actionsMap.TryFind action.ActionType
            |> Option.map (fun b -> b actionData)
            |> Option.defaultWith (fun _ -> Error $"Unknown action type: `{action.ActionType}`")
        with exn ->
            Error $"Failed to create action. Exception: {exn.Message}"

    let createActions (ctx: SqliteContext) (pipelineId: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest ->
            Operations.selectPipelineVersionRecord
                ctx
                [ "WHERE pipeline = @0 ORDER BY version DESC LIMIT 1;" ]
                [ pipelineId ]
        | ItemVersion.Specific v ->
            Operations.selectPipelineVersionRecord ctx [ "WHERE pipeline = @0 AND version = @1;" ] [ pipelineId; v ]
        |> Option.map (fun pv ->
            Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_version_id = @0" ] [ pv.Id ]
            |> List.sortBy (fun pa -> pa.Step)
            |> List.map (createAction (all ctx |> Map.ofList))
            |> flattenResultList)
        |> Option.defaultWith (fun _ -> Error $"Pipeline `{pipelineId}` (version {version.ToLabel()}) not found.")

    let getLastActionStep (ctx: SqliteContext) (pipelineVersionId: string) =
        ctx.Bespoke(
            "SELECT step FROM pipeline_actions WHERE pipeline_version_id = @0 ORDER BY step DESC LIMIT 1;",
            [ pipelineVersionId ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead
    
    let addAction () = ()
