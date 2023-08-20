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
            ?runId: string,
            ?additionActions: Actions.ActionCollection
        ) =
        match additionActions with
        | Some aa -> config.CreateActions(pipeline, version, aa)
        | None -> config.CreateActions(pipeline, version)
        |> Result.bind (fun pa ->
            PipelineContext.Initialize(basePath, pa, runId, logger)
            |> Result.map (fun ctx ->

                config.GetPipelineVersion(pipeline, version)
                |> Option.iter (fun pv ->
                    config.GetPipelineResources(pv.Id)
                    |> List.iter (fun pr ->
                        match config.GetResourceVersion(pr.ResourceVersionId) with
                        | Some r -> ctx.Store.AddResource(r.Resource, r.ResourceType, r.RawBlob.ToBytes())
                        | None -> ())

                    ctx.Store.SetPipelineName(pv.Pipeline)
                    ctx.Store.SetPipelineVersion(pv.Version)
                    ctx.Store.SetPipelineVersionId(pv.Id))

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


        let executeAction (pa: PipelineAction) (serial: int) (store: PipelineStore) =

            let startTimestamp = DateTime.UtcNow
            store.Log(pa.StepName, pa.Name, "Started")

            match pa.Action store with
            | Ok s ->
                let endTimeStamp = DateTime.UtcNow
                let offset = endTimeStamp - startTimestamp
                let message = $"Complete ({offset.TotalSeconds}s)"

                store.Log(pa.StepName, pa.Name, message)

                store.AddRunStateItem(pa.StepName, message, true, startTimestamp, endTimeStamp, serial)
                Ok s
            | Error e ->
                let endTimeStamp = DateTime.UtcNow

                store.LogError(pa.StepName, pa.Name, $"Failed. Error: {e}")
                store.AddRunStateItem(pa.StepName, e, true, startTimestamp, endTimeStamp, serial)

                Error e

        // Take start timestamp

        let startTimestamp = DateTime.UtcNow

        p.Store.Log("start", "main", $"Starting pipeline {p.Id}")

        let result =
            p.Actions
            |> List.fold
                (fun (r: Result<PipelineStore * int, string>) a ->
                    r
                    |> Result.bind (fun (s, i) -> executeAction a i s |> Result.map (fun r -> r, i + 1)))
                (Ok(p.Store, 0))

        let endTimestamp = DateTime.UtcNow

        match result with
        | Ok store ->
            let offset = endTimestamp - startTimestamp

            store.Log("end", "main", $"Pipeline completed successful ({offset.TotalSeconds}s)")

            Ok store
        | Error e -> Error e
