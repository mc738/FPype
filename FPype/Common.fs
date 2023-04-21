﻿namespace FPype

open System
open System.IO
open System.Text.Json
open FPype.Actions
open FPype.Configuration
open FPype.Core.Types
open FPype.Data.Models
open FPype.Data.Store
open FsToolbox.Core

type PipelineContext =
    { Id: string
      StorePath: string
      Store: PipelineStore
      LogToConsole: bool
      Actions: PipelineAction list }

    static member Initialize(basePath: string, logToConsole: bool, actions: PipelineAction list) =
        match Directory.Exists basePath with
        | true ->
            let id = Guid.NewGuid().ToString("n")
            //let dir = Path.Combine(basePath, id)
            //let importDir = Path.Combine(dir, "imports")
            //let exportDir = Path.Combine(dir, "exports")
            //let tmpDir = Path.Combine(dir, "tmp")


            //Directory.CreateDirectory(dir) |> ignore
            //Directory.CreateDirectory(importDir) |> ignore
            //Directory.CreateDirectory(exportDir) |> ignore
            //Directory.CreateDirectory(tmpDir) |> ignore

            PipelineStore.Initialize(basePath, id)
            |> Result.map (fun store ->
                { Id = id
                  StorePath = store.StorePath
                  Store = store
                  LogToConsole = logToConsole
                  Actions = actions })

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
