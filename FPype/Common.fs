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
      Actions: PipelineAction list
      Logger: PipelineLogger }

    static member Initialize
        (
            basePath: string,
            actions: PipelineAction list,
            runId: string option,
            logger: PipelineLogger
        ) =
        match Directory.Exists basePath with
        | true ->
            let id = runId |> Option.defaultWith (fun _ -> Guid.NewGuid().ToString("n"))

            PipelineStore.Initialize(basePath, id, logger)
            |> Result.map (fun store ->
                { Id = id
                  StorePath = store.StorePath
                  Store = store
                  Actions = actions
                  Logger = logger })

        | false -> Error $"Base directory `{basePath}` does not exist."

    static member Create
        (
            config: ConfigurationStore,
            basePath: string,
            pipeline: string,
            version: ItemVersion,
            args: Map<string, string>,
            logger: PipelineLogger,
            ?runId: string
        ) =
        config.CreateActions(pipeline, version)
        |> Result.bind (fun pa ->
            PipelineContext.Initialize(basePath, pa, runId, logger)
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


        let executeAction (pa: PipelineAction) (store: PipelineStore) =

            store.Log(pa.StepName, pa.Name, "Started")

            match pa.Action store with
            | Ok s ->
                store.Log(pa.StepName, pa.Name, "Complete")

                Ok s
            | Error e ->
                store.LogError(pa.StepName, pa.Name, $"Failed. Error: {e}")

                Error e

        // Take start timestamp

        let startTimestamp = DateTime.UtcNow

        let result =
            p.Actions
            |> List.fold (fun r a -> r |> Result.bind (executeAction a)) (Ok p.Store)

        let endTimestamp = DateTime.UtcNow

        match result with
        | Ok s -> Ok s
        | Error e -> Error e
