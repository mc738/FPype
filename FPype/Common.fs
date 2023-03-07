namespace FPype

open System
open System.IO
open System.Text.Json
open FPype.Actions
open FPype.Configuration
open FPype.Core.Types
open FPype.Data.Models
open FPype.Data.Store
open FsToolbox.Core

(*
module Serialization =

    let getData (element: JsonElement) =
        Json.tryGetElementsProperty "data" element |> Option.map Json.propertiesToMap

    let createField (element: JsonElement) =
        let name = Json.tryGetStringProperty "name" element

        let fieldType =
            Json.tryGetStringProperty "type" element
            |> Option.bind (fun t ->
                BaseType.FromId(t, Json.tryGetBoolProperty "optional" element |> Option.defaultValue false))

        //TODO import handler.

        match name, fieldType with
        | Some n, Some ft ->
            Ok(
                { Name = n
                  Type = ft
                  ImportHandler = None }: TableColumn
            )
        | None, _ -> Error "Missing name property."
        | _, None -> Error "Failed to create column type."

    let createFields (elements: JsonElement list) =
        elements
        |> List.map createField
        |> List.fold
            (fun (r, e) tc ->
                match tc with
                | Ok c -> r @ [ c ], e
                | Error em -> r, e @ [ em ])
            ([], [])
        |> fun (tcs, e) ->
            match e |> List.isEmpty with
            | true -> Ok tcs
            | false ->
                let errors = e |> String.concat " "
                Error $"Failed to create table columns. Errors: {errors}"

    let createTable (element: JsonElement) =
        let name = Json.tryGetStringProperty "name" element

        let fields =
            Json.tryGetArrayProperty "fields" element
            |> Option.map createFields
            |> Option.defaultValue (Error "Missing fields property")

        match name, fields with
        | Some n, Ok f -> Ok(n, f)
        | None, _ -> Error "Missing name property"
        | _, Error e -> Error e

    let createOutputTable (data: Map<string, JsonElement>) =
        data.TryFind "outputTable"
        |> Option.map createTable
        |> Option.defaultValue (Error "Missing outputTable property")

    let createImportAction (data: Map<string, JsonElement>) =
        let path = data.TryFind "path" |> Option.map (fun el -> el.GetString())

        let name = data.TryFind "name" |> Option.map (fun el -> el.GetString())

        match path, name with
        | Some p, Some n -> Import.file p n |> Ok
        | None, _ -> Error $"Missing path value in action data."
        | _, None -> Error "Missing name value in action data."

    let createParseCsvAction (data: Map<string, JsonElement>) =
        let source = data.TryFind "source" |> Option.map (fun el -> el.GetString())

        match source, createOutputTable data with
        | Some s, Ok (n, tc) -> Extract.parseCsv s tc n |> Ok
        | None, _ -> Error $"Missing path value in action data."
        | _, Error e -> Error e

    let createAggregateAction (data: Map<string, JsonElement>) =



        ()

    let createAction (element: JsonElement) : Result<PipelineStore -> Result<PipelineStore, string>, string> =
        Json.tryGetStringProperty "action" element
        |> Option.map (fun action ->
            match action, getData element with
            | "import", Some data -> createImportAction data
            | "parseCsv", Some data -> createParseCsvAction data
            | _, Some _ -> Error $"Unknown action: {action}"
            | _, None -> Error "Missing data property.")
        |> Option.defaultValue (Error "Missing `action` property.")
*)

type PipelineContext =
    { Id: string
      Directory: string
      ImportsPath: string
      StorePath: string
      Store: PipelineStore
      LogToConsole: bool
      Actions: PipelineAction list }

    static member Initialize(basePath: string, logToConsole: bool, actions: PipelineAction list) =
        match Directory.Exists basePath with
        | true ->
            let id = Guid.NewGuid().ToString("n")
            let dir = Path.Combine(basePath, id)
            let importDir = Path.Combine(dir, "imports")

            Directory.CreateDirectory(dir) |> ignore
            Directory.CreateDirectory(importDir) |> ignore

            let storePath = Path.Combine(dir, "store.db")

            let store = PipelineStore.Create(storePath)

            // Add some basic values for use in later steps (if needed).
            // TODO make a separate context table for these?
            store.AddStateValue("__id", id)
            store.AddStateValue("__computer_name", Environment.MachineName)
            store.AddStateValue("__user_name", Environment.UserName)
            store.AddStateValue("__base_path", dir)
            store.AddStateValue("__imports_path", importDir)

            { Id = id
              Directory = dir
              ImportsPath = importDir
              StorePath = storePath
              Store = store
              LogToConsole = logToConsole
              Actions = actions }
            |> Ok

        | false -> Error $"Base directory `{basePath}` does not exist."

    static member Create
        (
            config: ConfigurationStore,
            basePath: string,
            logToConsole: bool,
            pipeline: string,
            version: ItemVersion,
            args: Map<string, string>
        ) =
        config.CreateActions(pipeline, version)
        |> Result.bind (fun pa ->
            PipelineContext.Initialize(basePath, logToConsole, pa)
            |> Result.map (fun ctx ->

                config.GetPipelineVersion(pipeline, version)
                |> Option.iter (fun pv ->
                    config.GetPipelineResources(pv.Id)
                    |> List.iter (fun pr ->
                        match config.GetResourceVersion(pr.ResourceVersionId) with
                        | Some r -> ctx.Store.AddResource(r.Resource, r.ResourceType, r.RawBlob.ToBytes())
                        | None -> ()))

                // Get and validate args

                ctx))

    static member Deserialize(json: string) =
        try
            let jDoc = JsonDocument.Parse json

            let root = jDoc.RootElement

            let id =
                Json.tryGetStringProperty "id" root
                |> Option.defaultValue (Guid.NewGuid().ToString("n"))
            //let name

            Ok()
        with exn ->
            Error $"Could not deserialize pipeline. Error: {exn.Message}"

    member p.Run() =

        let log (store: PipelineStore) (name: string) (message: string) =
            store.Log(name, message)

            if p.LogToConsole then
                printfn $"[{DateTime.UtcNow}] {name} - {message}"

        let logError (store: PipelineStore) (name: string) (message: string) =
            store.LogError(name, message)

            if p.LogToConsole then
                printfn $"[{DateTime.UtcNow}] {name} - {message}"

        let executeAction (pa: PipelineAction) (store: PipelineStore) =
            log store pa.Name "Started"

            match pa.Action store with
            | Ok s ->
                log s pa.Name "Complete"
                Ok s
            | Error e ->
                logError store pa.Name $"Failed. Error: {e}"
                Error e

        p.Actions
        |> List.fold (fun r a -> r |> Result.bind (executeAction a)) (Ok p.Store)
