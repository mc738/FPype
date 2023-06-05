namespace FPype.Configuration

module Import =

    open System.IO
    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open Microsoft.FSharp.Core
    open FPype.Core
    open FPype.Core.Logging
    
    [<AutoOpen>]
    module private Helpers =

        let getName (json: JsonElement) = Json.tryGetStringProperty "name" json

        let getType (json: JsonElement) = Json.tryGetStringProperty "type" json

        let getPipeline (json: JsonElement) =
            Json.tryGetStringProperty "pipeline" json

        let from = "import"

    let table (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json with
        | Some name ->
            Json.tryGetArrayProperty "columns" json
            |> Option.map (fun cs ->
                cs
                |> List.mapi (fun i c ->
                    match Json.tryGetStringProperty "name" c, Json.tryGetStringProperty "type" c with
                    | Some n, Some t ->
                        ({ Id = IdType.FromJson json
                           Name = n
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
                   Columns = columns }: Tables.NewTableVersion))
        | None -> Error "Missing `name` property"
        |> Result.bind (
            match transaction with
            | true -> Tables.addVersionTransaction ctx
            | false -> Tables.addVersion ctx
        )

    let tables (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (table ctx transaction)

    let query (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json, Json.tryGetStringProperty "query" json with
        | Some n, Some q ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Query = q }: Queries.NewQueryVersion)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `query` property"
        |> Result.bind (
            match transaction with
            | true -> Queries.addVersionTransaction ctx
            | false -> Queries.addVersion ctx
        )

    let queries (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (query ctx transaction)

    let tableObjectMapper (ctx: SqliteContext) (transaction: bool) (json: JsonElement) =
        match getName json, Json.tryGetProperty "mapper" json with
        | Some n, Some m ->
            ({ Id = IdType.FromJson json
               Name = n
               Version = ItemVersion.FromJson json
               Mapper = m.ToString() }: TableObjectMappers.NewTableObjectMapperVersion)
            |> Ok
        | None, _ -> Error "Missing `name` property"
        | _, None -> Error "Missing `mapper` property"
        |> Result.bind (
            match transaction with
            | true -> TableObjectMappers.addRawVersionTransaction ctx
            | false -> TableObjectMappers.addRawVersion ctx
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
               Version = ItemVersion.FromJson json }: Pipelines.NewPipelineVersion)
            |> fun pipeline ->
                match transaction with
                | true -> Pipelines.addVersionTransaction ctx pipeline
                | false -> Pipelines.addVersion ctx pipeline
        | None -> Error "Missing `name` property"

    let pipelines (ctx: SqliteContext) (transaction: bool) (json: JsonElement list) =
        json |> List.map (pipeline ctx transaction)

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

    let fromFileTransaction (ctx: SqliteContext) (path: string) =

        try
            match File.Exists path with
            | false ->
                let message = $"File `{path}` does not exist."
                logError from message
                Error message
            | true ->
                let json = File.ReadAllText path |> JsonDocument.Parse

                // Load pipelines.
                logInfo from "Importing pipelines"

                ctx.ExecuteInTransactionV2(fun t ->
                    Json.tryGetArrayProperty "pipelines" json.RootElement
                    |> Option.map (
                        pipelines t false
                        >> flattenResultList
                        >> Result.map (fun r -> logSuccess from $"{r.Length} pipeline(s) imported")
                    )
                    |> Option.defaultWith (fun _ ->
                        logInfo from "0 pipelines imported"
                        Ok())
                    |> Result.bind (fun _ ->
                        Json.tryGetArrayProperty "pipelineArgs" json.RootElement
                        |> Option.map (
                            pipelineArgs t false
                            >> flattenResultList
                            >> Result.map (fun r -> logSuccess from $"{r.Length} pipeline arg(s) imported")
                        )
                        |> Option.defaultWith (fun _ ->
                            logInfo from "0 pipeline args imported"
                            Ok()))
                    |> Result.bind (fun _ ->
                        Json.tryGetArrayProperty "tables" json.RootElement
                        |> Option.map (
                            tables t false
                            >> flattenResultList
                            >> Result.map (fun r -> logSuccess from $"{r.Length} table(s) imported")
                        )
                        |> Option.defaultWith (fun _ ->
                            logInfo from "0 tables imported"
                            Ok()))
                    |> Result.bind (fun _ ->
                        Json.tryGetArrayProperty "pipelineActions" json.RootElement
                        |> Option.map (
                            pipelineActions t false
                            >> flattenResultList
                            >> Result.map (fun r -> logSuccess from $"{r.Length} pipeline action(s) imported")
                        )
                        |> Option.defaultWith (fun _ ->
                            logInfo from "0 pipeline actions imported"
                            Ok()))
                    |> Result.bind (fun _ ->
                        Json.tryGetArrayProperty "queries" json.RootElement
                        |> Option.map (
                            queries t false
                            >> flattenResultList
                            >> Result.map (fun r -> logSuccess from $"{r.Length} query(s) imported")
                        )
                        |> Option.defaultWith (fun _ ->
                            logInfo from "0 queries imported"
                            Ok()))
                    |> Result.bind (fun _ ->
                        Json.tryGetArrayProperty "tableObjectMappers" json.RootElement
                        |> Option.map (
                            tableObjectMappers t false
                            >> flattenResultList
                            >> Result.map (fun r -> logSuccess from $"{r.Length} table object mapper(s) imported")
                        )
                        |> Option.defaultWith (fun _ ->
                            logInfo from "0 table object mappers imported"
                            Ok())))

        with exn ->
            let message =
                $"Unhandled exception while importing pipeline configurations: {exn.Message}"

            logError from message
            Error message
