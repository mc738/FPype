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
            let name = ""

            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "path" json, Json.tryGetStringProperty "name" json with
                | Some path, Some name -> Ok(Import.file path name)
                | None, _ -> Error "Missing path property"
                | _, None -> Error "Missing name property"
                |> Result.map (fun a -> PipelineAction.Create(name, a))

        let all = [ ``import-file``.name, ``import-file``.deserialize ]

    module Extract =

        module ``parse-csv`` =

            let name = ""

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "source" json, Json.tryGetStringProperty "table" json with
                | Some source, Some tableName ->
                    Tables.getTable ctx tableName
                    |> Option.map (fun t ->
                        Tables.createColumns ctx t.Name
                        |> Result.map (fun tc -> Extract.parseCsv source tc t.Name))
                    |> Option.defaultValue (Error $"Table `{tableName}` not found")
                | None, _ -> Error "Missing source property"
                | _, None -> Error "Missing table property"
                |> Result.map (fun a -> PipelineAction.Create(name, a))

        let all (ctx: SqliteContext) =
            [ ``parse-csv``.name, ``parse-csv``.deserialize ctx ]

    module Transform =

        module ``aggregate`` =
            let name = ""

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Queries.tryCreate ctx json, Json.tryGetStringProperty "table" json with
                | Some query, Some tableName ->
                    Tables.getTable ctx tableName
                    |> Option.map (fun t ->
                        Tables.createColumns ctx t.Name
                        |> Result.map (fun tc -> Transform.aggregate t.Name tc query []))
                    |> Option.defaultValue (Error $"Table `{tableName}` not found")
                | None, _ -> Error "Missing query property"
                | _, None -> Error "Missing table property"
                |> Result.map (fun a -> PipelineAction.Create(name, a))

        module ``aggregate-by-category-and-date`` =

            let name = ""

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
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
                |> Result.map (fun a -> PipelineAction.Create(name, a))

        module ``aggregate-by-date`` =

            let name = ""

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
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
                |> Result.map (fun a -> PipelineAction.Create(name, a))

        module ``map-to-object`` =

            let name = ""

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                Json.tryGetStringProperty "mapper" json
                |> Option.map (fun mn -> ObjectMappers.load ctx mn)
                |> Option.defaultValue (Error "Missing `mapper` property")
                |> Result.map (fun m -> Transform.mapToObject m)
                |> Result.map (fun a -> PipelineAction.Create(name, a))


        let all (ctx: SqliteContext) =
            [ ``aggregate``.name, ``aggregate``.deserialize ctx
              ``aggregate-by-category-and-date``.name, ``aggregate-by-category-and-date``.deserialize ctx
              ``aggregate-by-date``.name, ``aggregate-by-date``.deserialize ctx
              ``map-to-object``.name, ``map-to-object``.deserialize ctx ]

    module Load =

        let all (ctx: SqliteContext) = []

    let all (ctx: SqliteContext) =
        [ yield! Import.all
          yield! Extract.all ctx
          yield! Transform.all ctx
          yield! Load.all ctx ]
        
    let createAction (actionsMap: Map<string,JsonElement -> Result<PipelineAction, string>>) (action: Records.PipelineAction) =
        let actionData =
            action.ActionBlob |> blobToString |> toJson

        try
            actionsMap.TryFind action.ActionType
            |> Option.map (fun b -> b actionData)
            |> Option.defaultWith (fun _ -> Error $"Unknown action type: `{action.ActionType}`")
        with
        | exn -> Error $"Failed to create action. Exception: {exn.Message}"

    let createActions (ctx: SqliteContext) (pipelineId: string) =
        Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_id = @0" ] [ pipelineId ]
        |> List.sortBy (fun pa -> pa.Step)
        |> List.map (createAction (all ctx |> Map.ofList))