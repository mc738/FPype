namespace FPype.Configuration

open FPype.Actions
open FPype.Actions.ML
open FPype.Visualizations.Charts.LineCharts
open Freql.Sqlite
open Microsoft.VisualBasic.CompilerServices

module Actions =

    open System.IO
    open System.Text.Json
    open FPype.Configuration.Persistence
    open Freql.Core.Common.Types
    open Microsoft.FSharp.Core
    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FsToolbox.Extensions
    open FPype.Core
    open FPype.Actions

    type NewPipelineAction =
        { Id: IdType
          Name: string
          Pipeline: string
          Version: ItemVersion
          ActionType: string
          ActionData: string
          Step: int option }

    [<AutoOpen>]
    module private Internal =

        let createQueryAndTable (ctx: SqliteContext) (queryVersion: QueryVersion) (tableVersion: TableVersion) =
            Queries.get ctx queryVersion.Name queryVersion.Version
            |> Option.map (fun q ->
                Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                |> Result.map (fun t -> q, t))
            |> Option.defaultWith (fun _ ->
                Error
                    $"Error creating query: query `{queryVersion.Name}` (version `{queryVersion.Version.ToLabel()}`) not found")

    module Utils =
        module ``create-directory`` =
            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "path" json, Json.tryGetStringProperty "name" json with
                | Some path, Some name ->
                    ({ Path = path; Name = name }: Utils.``create-directory``.Parameters)
                    |> Utils.``create-directory``.createAction
                    |> Ok
                | None, _ -> Error "Missing path property"
                | _, None -> Error "Missing name property"

        module ``create_sqlite_database`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "path" json, Json.tryGetArrayProperty "tables" json with
                | Some path, Some tables ->
                    tables
                    |> List.map (fun el ->
                        TableVersion.TryFromJson el
                        |> Result.bind (fun tv -> Tables.tryCreateTableModel ctx tv.Name tv.Version))
                    |> flattenResultList
                    |> Result.map (fun ts ->
                        ({ Path = path
                           Tables = ts
                           VariableName = Json.tryGetStringProperty "variable" json }
                        : Utils.``create-sqlite-database``.Parameters)
                        |> Utils.``create-sqlite-database``.createAction)
                | None, _ -> Error "Missing path property"
                | _, None -> Error "Missing tables property"

        let names =
            [ Utils.``create-directory``.name; Utils.``create-sqlite-database``.name ]

        let all ctx =
            [ Utils.``create-directory``.name, ``create-directory``.deserialize
              Utils.``create-sqlite-database``.name, create_sqlite_database.deserialize ctx ]

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

        module ``chunk-file`` =

            let deserialize (json: JsonElement) =
                match
                    Json.tryGetStringProperty "path" json,
                    Json.tryGetStringProperty "name" json,
                    Json.tryGetIntProperty "size" json
                with
                | Some path, Some name, Some size ->
                    ({ Path = path
                       CollectionName = name
                       ChunkSize = size }
                    : Import.``chunk-file``.Parameters)
                    |> Import.``chunk-file``.createAction
                    |> Ok
                | None, _, _ -> Error "Missing path property"
                | _, None, _ -> Error "Missing name property"
                | _, _, None -> Error "Missing size property"

        module ``http-get`` =
            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "url" json, Json.tryGetStringProperty "name" json with
                | Some url, Some name ->
                    let additionalHeaders =
                        Json.tryGetArrayProperty "additionalHeaders" json
                        |> Option.map (fun ahs ->
                            ahs
                            |> List.choose (fun ah ->
                                match Json.tryGetStringProperty "key" ah, Json.tryGetStringProperty "value" ah with
                                | Some k, Some v -> Some(k, v)
                                | None, _
                                | _, None -> None)
                            |> Map.ofList)
                        |> Option.defaultValue Map.empty

                    ({ Url = url
                       AdditionHeaders = additionalHeaders
                       Name = name
                       ResponseType = Json.tryGetStringProperty "responseType" json
                       Collection = Json.tryGetStringProperty "collection" json }
                    : Import.``http-get``.Parameters)
                    |> Import.``http-get``.createAction
                    |> Ok
                | None, _ -> Error "Missing url property"
                | _, None -> Error "Missing name property"

        let names =
            [ Import.``import-file``.name
              Import.``chunk-file``.name
              Import.``http-get``.name ]

        let all =
            [ Import.``import-file``.name, ``import-file``.deserialize
              Import.``chunk-file``.name, ``chunk-file``.deserialize
              Import.``http-get``.name, ``http-get``.deserialize ]

    module Extract =

        module ``parse-csv`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "source" json, TableVersion.TryCreate json with
                | Some source, Ok tableVersion ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ DataSource = source; Table = t }: Extract.``parse-csv``.Parameters)
                        |> Extract.``parse-csv``.createAction)
                | None, _ -> Error "Missing source property"
                | _, Error e -> Error $"Error creating table: {e}"

        module ``parse-csv-collection`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "collection" json, TableVersion.TryCreate json with
                | Some collection, Ok tableVersion ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ CollectionName = collection
                           Table = t }
                        : Extract.``parse-csv-collection``.Parameters)
                        |> Extract.``parse-csv-collection``.createAction)
                | None, _ -> Error "Missing collection property"
                | _, Error e -> Error $"Error creating table: {e}"

        module ``grok`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetStringProperty "source" json,
                    Json.tryGetStringProperty "grokString" json,
                    TableVersion.TryCreate json
                with
                | Some source, Some grokString, Ok tableVersion ->
                    Tables.tryCreateTableModel ctx tableVersion.Name tableVersion.Version
                    |> Result.map (fun t ->
                        ({ DataSource = source
                           Table = t
                           GrokString = grokString
                           ExtraPatterns =
                             Json.tryGetArrayProperty "extraPatterns" json
                             |> Option.map (fun eps ->
                                 eps
                                 |> List.choose (fun ep ->
                                     match
                                         Json.tryGetStringProperty "name" ep, Json.tryGetStringProperty "pattern" ep
                                     with
                                     | Some n, Some p -> Some(n, p)
                                     | _ -> None))
                             |> Option.defaultValue [] }
                        : Extract.``grok``.Parameters)
                        |> Extract.``grok``.createAction)
                | None, _, _ -> Error "Missing source property"
                | _, None, _ -> Error "Missing grokString property"
                | _, _, Error e -> Error $"Error creating table: {e}"

        module ``query-sqlite-database`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetStringProperty "path" json, TableVersion.TryCreate json, QueryVersion.TryCreate json
                with
                | Some path, Ok tableVersion, Ok query ->
                    createQueryAndTable ctx query tableVersion
                    |> Result.map (fun (q, t) ->
                        ({ Path = path
                           Table = t
                           Sql = q
                           Parameters = [] }
                        : Extract.``query-sqlite-database``.Parameters)
                        |> Extract.``query-sqlite-database``.createAction)
                | None, _, _ -> Error "Missing source property"
                | _, Error e, _ -> Error $"Error creating query: {e}"
                | _, _, Error e -> Error $"Error creating table: {e}"

        let names =
            [ Extract.``parse-csv``.name
              Extract.``parse-csv-collection``.name
              Extract.``grok``.name
              Extract.``query-sqlite-database``.name ]

        let all (ctx: SqliteContext) =
            [ Extract.``parse-csv``.name, ``parse-csv``.deserialize ctx
              Extract.``parse-csv-collection``.name, ``parse-csv``.deserialize ctx
              Extract.``grok``.name, ``grok``.deserialize ctx
              Extract.``query-sqlite-database``.name, ``query-sqlite-database``.deserialize ctx ]

    module Transform =

        module ``execute-query`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match QueryVersion.TryCreate json, TableVersion.TryCreate json with
                | Ok query, Ok tableVersion ->
                    createQueryAndTable ctx query tableVersion
                    |> Result.map (fun (q, t) ->
                        // NOTE currently parameters.Parameters is always empty.
                        ({ Table = t; Sql = q; Parameters = [] }: Transform.``execute-query``.Parameters)
                        |> Transform.``execute-query``.createAction)
                | Error e, _ -> Error $"Error creating query: {e}"
                | _, Error e -> Error $"Error creating table: {e}"

        module ``aggregate`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match QueryVersion.TryCreate json, TableVersion.TryCreate json with
                | Ok query, Ok tableVersion ->
                    createQueryAndTable ctx query tableVersion
                    |> Result.map (fun (q, t) ->
                        // NOTE currently parameters.Parameters is always empty.
                        ({ Table = t; Sql = q; Parameters = [] }: Transform.``aggregate``.Parameters)
                        |> Transform.aggregate.createAction)
                | Error e, _ -> Error $"Error creating query: {e}"
                | _, Error e -> Error $"Error creating table: {e}"

        module ``aggregate-by-date-and-category`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
                    Json.tryGetStringProperty "categoryField" json,
                    TableVersion.TryCreate json,
                    QueryVersion.TryCreate json
                with
                | Ok dateGroup, Some categoryField, Ok tableVersion, Ok query ->
                    createQueryAndTable ctx query tableVersion
                    |> Result.map (fun (q, t) ->
                        ({ Table = t
                           SelectSql = q
                           DateGroups = dateGroup
                           CategoryField = categoryField }
                        : Transform.``aggregate-by-date-and-category``.Parameters)
                        |> Transform.``aggregate-by-date-and-category``.createAction)
                | Error e, _, _, _ -> Error $"Error creating date groups: {e}"
                | _, None, _, _ -> Error "Missing `category` field"
                | _, _, Error e, _ -> Error $"Error creating table: {e}"
                | _, _, _, Error e -> Error $"Error creating query: {e}"

        module ``aggregate-by-date`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetElementsProperty "dateGroups" json |> Groups.createDateGroup,
                    TableVersion.TryCreate json,
                    QueryVersion.TryCreate json
                with
                | Ok dateGroup, Ok tableVersion, Ok query ->
                    createQueryAndTable ctx query tableVersion
                    |> Result.map (fun (q, t) ->
                        ({ Table = t
                           SelectSql = q
                           DateGroups = dateGroup }
                        : Transform.``aggregate-by-date``.Parameters)
                        |> Transform.``aggregate-by-date``.createAction)
                | Error e, _, _ -> Error $"Error creating date groups: {e}"
                | _, Error e, _ -> Error $"Error creating table: {e}"
                | _, _, Error e -> Error $"Error creating query: {e}"

        module ``map-to-object`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match Json.tryGetStringProperty "mapper" json, Json.tryGetIntProperty "version" json with
                | Some m, Some v -> ItemVersion.Specific v |> TableObjectMappers.load ctx m
                | Some m, None -> ItemVersion.Latest |> TableObjectMappers.load ctx m
                | None, _ -> Error "Missing `mapper` property"
                |> Result.map Transform.``map-to-object``.createAction

        let names =
            [ Transform.``execute-query``.name
              Transform.``aggregate``.name
              Transform.``aggregate-by-date-and-category``.name
              Transform.``aggregate-by-date``.name
              Transform.``map-to-object``.name ]

        let all (ctx: SqliteContext) =
            [ Transform.``execute-query``.name, ``execute-query``.deserialize ctx
              Transform.``aggregate``.name, ``aggregate``.deserialize ctx
              Transform.``aggregate-by-date-and-category``.name, ``aggregate-by-date-and-category``.deserialize ctx
              Transform.``aggregate-by-date``.name, ``aggregate-by-date``.deserialize ctx
              Transform.``map-to-object``.name, ``map-to-object``.deserialize ctx ]

    module Load =

        let names = []

        let all (ctx: SqliteContext) = []

    module Export =

        open FPype.Actions.Export


        module ``table-to-csv`` =
            ()
        (*    
            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    TableVersion.TryFromJson json,
                    
        *)

        module ``export-artifact`` =

            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "artifactName" json with
                | Some an ->
                    ({ ArtifactName = an
                       OutputPath = Json.tryGetStringProperty "outputPath" json
                       FileExtension = Json.tryGetStringProperty "fileExtension" json }
                    : ``export-artifact``.Parameters)
                    |> ``export-artifact``.createAction
                    |> Ok
                | None -> Error "Missing `artifactName` property"

        module ``export-artifact-bucket`` =

            let deserialize (json: JsonElement) =
                match Json.tryGetStringProperty "bucketName" json with
                | Some bn ->
                    ({ BucketName = bn
                       OutputPath = Json.tryGetStringProperty "outputPath" json }
                    : ``export-artifact-bucket``.Parameters)
                    |> ``export-artifact-bucket``.createAction
                    |> Ok
                | None -> Error "Missing `bucketName` property"

        let names = [ ``export-artifact``.name; ``export-artifact-bucket``.name ]

        let all (ctx: SqliteContext) =
            [ ``export-artifact``.name, ``export-artifact``.deserialize
              ``export-artifact-bucket``.name, ``export-artifact-bucket``.deserialize ]

    module ML =

        module ``train-binary-classification-model`` =
            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "trainingSettings" json
                    |> Option.map FPype.ML.BinaryClassification.TrainingSettings.FromJson
                    |> Option.defaultWith (fun _ -> Error "Missing `trainingSettings` property"),
                    Json.tryGetStringProperty "modelName" json,
                    Json.tryGetStringProperty "source" json,
                    Json.tryGetStringProperty "modelSavePath" json
                with
                | Ok ts, Some mn, Some ds, Some msp ->
                    ({ TrainingSettings = ts
                       ModelName = mn
                       DataSource = ds
                       ModelSavePath = msp
                       ContextSeed = Json.tryGetIntProperty "contextSeed" json }
                    : ML.``train-binary-classification-model``.Parameters)
                    |> ML.``train-binary-classification-model``.createAction
                    |> Ok
                | Error e, _, _, _ -> Error e
                | _, None, _, _ -> Error "Missing `modelName` property"
                | _, _, None, _ -> Error "Missing `source` property"
                | _, _, _, None -> Error "Missing `modelSavePath` property"

        module ``train-multiclass-classification-model`` =
            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "trainingSettings" json
                    |> Option.map FPype.ML.MulticlassClassification.TrainingSettings.FromJson
                    |> Option.defaultWith (fun _ -> Error "Missing `trainingSettings` property"),
                    Json.tryGetStringProperty "modelName" json,
                    Json.tryGetStringProperty "source" json,
                    Json.tryGetStringProperty "modelSavePath" json
                with
                | Ok ts, Some mn, Some ds, Some msp ->
                    ({ TrainingSettings = ts
                       ModelName = mn
                       DataSource = ds
                       ModelSavePath = msp
                       ContextSeed = Json.tryGetIntProperty "contextSeed" json }
                    : ML.``train-multiclass-classification-model``.Parameters)
                    |> ML.``train-multiclass-classification-model``.createAction
                    |> Ok
                | Error e, _, _, _ -> Error e
                | _, None, _, _ -> Error "Missing `modelName` property"
                | _, _, None, _ -> Error "Missing `source` property"
                | _, _, _, None -> Error "Missing `modelSavePath` property"

        module ``train-regression-model`` =
            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "trainingSettings" json
                    |> Option.map FPype.ML.Regression.TrainingSettings.FromJson
                    |> Option.defaultWith (fun _ -> Error "Missing `trainingSettings` property"),
                    Json.tryGetStringProperty "modelName" json,
                    Json.tryGetStringProperty "source" json,
                    Json.tryGetStringProperty "modelSavePath" json
                with
                | Ok ts, Some mn, Some ds, Some msp ->
                    ({ TrainingSettings = ts
                       ModelName = mn
                       DataSource = ds
                       ModelSavePath = msp
                       ContextSeed = Json.tryGetIntProperty "contextSeed" json }
                    : ML.``train-regression-model``.Parameters)
                    |> ML.``train-regression-model``.createAction
                    |> Ok
                | Error e, _, _, _ -> Error e
                | _, None, _, _ -> Error "Missing `modelName` property"
                | _, _, None, _ -> Error "Missing `source` property"
                | _, _, _, None -> Error "Missing `modelSavePath` property"

        module ``train-matrix-factorization-model`` =
            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "trainingSettings" json
                    |> Option.map FPype.ML.MatrixFactorization.TrainingSettings.FromJson
                    |> Option.defaultWith (fun _ -> Error "Missing `trainingSettings` property"),
                    Json.tryGetStringProperty "modelName" json,
                    Json.tryGetStringProperty "source" json,
                    Json.tryGetStringProperty "modelSavePath" json
                with
                | Ok ts, Some mn, Some ds, Some msp ->
                    ({ TrainingSettings = ts
                       ModelName = mn
                       DataSource = ds
                       ModelSavePath = msp
                       ContextSeed = Json.tryGetIntProperty "contextSeed" json }
                    : ML.``train-matrix-factorization-model``.Parameters)
                    |> ML.``train-matrix-factorization-model``.createAction
                    |> Ok
                | Error e, _, _, _ -> Error e
                | _, None, _, _ -> Error "Missing `modelName` property"
                | _, _, None, _ -> Error "Missing `source` property"
                | _, _, _, None -> Error "Missing `modelSavePath` property"

        let names =
            [ ML.``train-binary-classification-model``.name
              ML.``train-multiclass-classification-model``.name
              ML.``train-regression-model``.name
              ML.``train-matrix-factorization-model``.name ]

        let all (ctx: SqliteContext) =
            [ ML.``train-binary-classification-model``.name, ``train-binary-classification-model``.deserialize ctx
              ML.``train-multiclass-classification-model``.name,
              ``train-multiclass-classification-model``.deserialize ctx
              ML.``train-regression-model``.name, ``train-regression-model``.deserialize ctx
              ML.``train-matrix-factorization-model``.name, ``train-matrix-factorization-model``.deserialize ctx ]

    module Visualizations =

        open FPype.Actions.Visualizations

        module ``generate-time-series-chart-collection`` =

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "categoriesQuery" json
                    |> Option.map QueryVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `categoriesQuery` object"),
                    Json.tryGetProperty "categoriesTable" json
                    |> Option.map TableVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `categoriesTable` object"),
                    Json.tryGetIntProperty "categoryIndex" json,
                    Json.tryGetProperty "timeSeriesQuery" json
                    |> Option.map QueryVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `timeSeriesQuery` object"),
                    Json.tryGetProperty "timeSeriesTable" json
                    |> Option.map TableVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `timeSeriesTable` object"),
                    Json.tryGetProperty "settings" json
                    |> Option.map TimeSeriesChartGeneratorSettings.TryFromJson
                    |> Option.defaultValue (Error "Missing `settings` property")
                with
                | Ok cqv, Ok ctv, Some ci, Ok tqv, Ok ttv, Ok gs ->
                    match createQueryAndTable ctx cqv ctv, createQueryAndTable ctx tqv ttv with
                    | Ok(catQuery, catTable), Ok(tsQuery, tsTable) ->
                        ({ ResultBucket = Json.tryGetStringProperty "resultBucket" json |> Option.defaultValue "exports"
                           FileNameFormat = Json.tryGetStringProperty "fileNameFormat" json |> Option.defaultValue "{0}"
                           CategoriesQuerySql = catQuery
                           CategoriesTable = catTable
                           CategoryIndex = ci
                           TimeSeriesQuerySql = tsQuery
                           TimeSeriesTable = tsTable
                           GeneratorSettings = gs }
                        : ``generate-time-series-chart-collection``.Parameters)
                        |> ``generate-time-series-chart-collection``.createAction
                        |> Ok
                    | Error e, _ -> Error e
                    | _, Error e -> Error e
                | Error e, _, _, _, _, _ -> Error e
                | _, Error e, _, _, _, _ -> Error e
                | _, _, None, _, _, _ -> Error "Missing `categoryIndex` property"
                | _, _, _, Error e, _, _ -> Error e
                | _, _, _, _, Error e, _ -> Error e
                | _, _, _, _, _, Error e -> Error e

        module ``generate-candle-stick-chart-collection`` =

            open FPype.Visualizations.Charts.CandleStickCharts

            let deserialize (ctx: SqliteContext) (json: JsonElement) =
                match
                    Json.tryGetProperty "categoriesQuery" json
                    |> Option.map QueryVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `categoriesQuery` object"),
                    Json.tryGetProperty "categoriesTable" json
                    |> Option.map TableVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `categoriesTable` object"),
                    Json.tryGetIntProperty "categoryIndex" json,
                    Json.tryGetProperty "seriesQuery" json
                    |> Option.map QueryVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `seriesQuery` object"),
                    Json.tryGetProperty "seriesTable" json
                    |> Option.map TableVersion.TryFromJson
                    |> Option.defaultValue (Error "Missing `seriesTable` object"),
                    Json.tryGetProperty "settings" json
                    |> Option.map CandleStickChartGeneratorSettings.TryFromJson
                    |> Option.defaultValue (Error "Missing `settings` property")
                with
                | Ok cqv, Ok ctv, Some ci, Ok tqv, Ok ttv, Ok gs ->
                    match createQueryAndTable ctx cqv ctv, createQueryAndTable ctx tqv ttv with
                    | Ok(catQuery, catTable), Ok(tsQuery, tsTable) ->
                        ({ ResultBucket = Json.tryGetStringProperty "resultBucket" json |> Option.defaultValue "exports"
                           FileNameFormat = Json.tryGetStringProperty "fileNameFormat" json |> Option.defaultValue "{0}"
                           CategoriesQuerySql = catQuery
                           CategoriesTable = catTable
                           CategoryIndex = ci
                           SeriesQuerySql = tsQuery
                           SeriesTable = tsTable
                           GeneratorSettings = gs }
                        : ``generate-candle-stick-chart-collection``.Parameters)
                        |> ``generate-candle-stick-chart-collection``.createAction
                        |> Ok
                    | Error e, _ -> Error e
                    | _, Error e -> Error e
                | Error e, _, _, _, _, _ -> Error e
                | _, Error e, _, _, _, _ -> Error e
                | _, _, None, _, _, _ -> Error "Missing `categoryIndex` property"
                | _, _, _, Error e, _, _ -> Error e
                | _, _, _, _, Error e, _ -> Error e
                | _, _, _, _, _, Error e -> Error e

        let names =
            [ ``generate-time-series-chart-collection``.name
              ``generate-candle-stick-chart-collection``.name ]

        let all (ctx: SqliteContext) =
            [ ``generate-time-series-chart-collection``.name, ``generate-time-series-chart-collection``.deserialize ctx
              ``generate-candle-stick-chart-collection``.name,
              ``generate-candle-stick-chart-collection``.deserialize ctx ]

    let names =
        [ yield! Utils.names
          yield! Import.names
          yield! Extract.names
          yield! Transform.names
          yield! Load.names
          yield! Export.names
          yield! ML.names
          yield! Visualizations.names ]

    let all (ctx: SqliteContext) =
        [ yield! Utils.all ctx
          yield! Import.all
          yield! Extract.all ctx
          yield! Transform.all ctx
          yield! Load.all ctx
          yield! Export.all ctx
          yield! ML.all ctx
          yield! Visualizations.all ctx ]

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

    let stepExists (ctx: SqliteContext) (pipelineVersionId: string) (step: int) =
        ctx.Bespoke(
            "SELECT step FROM pipeline_actions WHERE pipeline_version_id = @0 AND step = @1 LIMIT 1;",
            [ pipelineVersionId; step ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead
        |> Option.map (fun _ -> true)
        |> Option.defaultValue false

    let addActionAsLast
        (ctx: SqliteContext)
        (pipeline: string)
        (version: ItemVersion)
        (id: IdType)
        (name: string)
        (actionType: string)
        (actionData: string)
        =
        //let versionId =
        match version with
        | ItemVersion.Latest -> Pipelines.getLatestVersionId ctx pipeline
        | ItemVersion.Specific v -> Pipelines.getVersionId ctx pipeline v
        |> Option.map (fun versionId ->
            let prevStep = getLastActionStep ctx versionId |> Option.defaultValue 0

            use ms = new MemoryStream(actionData.ToUtf8Bytes())
            let hash = ms.GetSHA256Hash()

            ({ Id = id.Get()
               PipelineVersionId = versionId
               Name = name
               ActionType = actionType
               ActionBlob = BlobField.FromBytes ms
               Hash = hash
               Step = prevStep + 1 }
            : Parameters.NewPipelineAction)
            |> Operations.insertPipelineAction ctx
            |> Ok)
        |> Option.defaultWith (fun _ -> Error $"Version `{version.ToLabel()}` of pipeline `{pipeline}` not found.")

    let addActionAsSpecificStep
        (ctx: SqliteContext)
        (pipeline: string)
        (version: ItemVersion)
        (id: IdType)
        (name: string)
        (actionType: string)
        (actionData: string)
        (step: int)
        =
        match version with
        | ItemVersion.Latest -> Pipelines.getLatestVersionId ctx pipeline
        | ItemVersion.Specific v -> Pipelines.getVersionId ctx pipeline v
        |> Option.map (fun versionId ->

            match stepExists ctx versionId step with
            | true -> Error $"Version `{version.ToLabel()}` of pipeline `{pipeline}` already contains step `{step}`."
            | false ->
                use ms = new MemoryStream(actionData.ToUtf8Bytes())
                let hash = ms.GetSHA256Hash()

                ({ Id = id.Get()
                   PipelineVersionId = versionId
                   Name = name
                   ActionType = actionType
                   ActionBlob = BlobField.FromBytes ms
                   Hash = hash
                   Step = step }
                : Parameters.NewPipelineAction)
                |> Operations.insertPipelineAction ctx
                |> Ok)
        |> Option.defaultWith (fun _ -> Error $"Version `{version.ToLabel()}` of pipeline `{pipeline}` not found")

    let add (ctx: SqliteContext) (action: NewPipelineAction) =
        match action.Step with
        | Some s ->
            addActionAsSpecificStep
                ctx
                action.Pipeline
                action.Version
                action.Id
                action.Name
                action.ActionType
                action.ActionData
                s
        | None ->
            addActionAsLast ctx action.Pipeline action.Version action.Id action.Name action.ActionType action.ActionData

    let addTransaction (ctx: SqliteContext) (action: NewPipelineAction) =
        ctx.ExecuteInTransactionV2(fun t -> add t action)

    let getAllTypes (ctx: SqliteContext) =
        Operations.selectActionTypeRecords ctx [] []

    let addType (ctx: SqliteContext) (name: string) =
        ({ Name = name }: Parameters.NewActionType) |> Operations.insertActionType ctx
