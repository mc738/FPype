﻿namespace FPype.Configuration

open Microsoft.FSharp.Core

module Pipelines =

    open Freql.Sqlite
    open FPype.Configuration.Persistence
    open FPype.Data

    type NewPipelineVersion =
        { Id: IdType
          Name: string
          Description: string
          Version: ItemVersion }

    type NewPipelineArg =
        { Id: IdType
          Pipeline: string
          Name: string
          Version: ItemVersion
          Required: bool
          DefaultValue: string option }

    type NewPipelineResource =
        { Id: IdType
          PipelineVersionId: string
          ResourceVersionId: string }

    let getLatestVersion (ctx: SqliteContext) (pipeline: string) =
        Operations.selectPipelineVersionRecord ctx [ "WHERE pipeline = @0 ORDER BY version DESC LIMIT 1;" ] [ pipeline ]

    let getVersion (ctx: SqliteContext) (pipeline: string) (version: int) =
        Operations.selectPipelineVersionRecord ctx [ "WHERE pipeline = @0 AND version = @1;" ] [ pipeline; version ]

    let get (ctx: SqliteContext) (pipeline: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx pipeline
        | ItemVersion.Specific v -> getVersion ctx pipeline v

    let latestVersion (ctx: SqliteContext) (pipeline: string) =
        ctx.Bespoke(
            "SELECT version FROM pipeline_versions WHERE pipeline = @0 ORDER BY version DESC LIMIT 1;",
            [ pipeline ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead

    let getVersionId (ctx: SqliteContext) (pipeline: string) (version: int) =
        ctx.Bespoke(
            "SELECT id FROM pipeline_versions WHERE pipeline = @0 AND version = @1 LIMIT 1;",
            [ pipeline; version ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let getLatestVersionId (ctx: SqliteContext) (pipeline: string) =
        ctx.Bespoke(
            "SELECT id FROM pipeline_versions WHERE pipeline = @0 ORDER BY version DESC LIMIT 1;",
            [ pipeline ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let addLatestVersion (ctx: SqliteContext) (id: IdType) (pipeline: string) (description: string) =
        let version =
            match latestVersion ctx pipeline with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION if no version exists the pipeline is new.
                ({ Name = pipeline }: Parameters.NewPipeline) |> Operations.insertPipeline ctx
                1

        ({ Id = id.Get()
           Pipeline = pipeline
           Version = version
           Description = description
           CreatedOn = timestamp () }
        : Parameters.NewPipelineVersion)
        |> Operations.insertPipelineVersion ctx

    let addLatestVersionTransaction (ctx: SqliteContext) (id: IdType) (pipeline: string) (description: string) =
        ctx.ExecuteInTransaction(fun t -> addLatestVersion t id pipeline description)

    let addSpecificVersion (ctx: SqliteContext) (id: IdType) (pipeline: string) (description: string) (version: int) =
        match getVersionId ctx pipeline version with
        | Some _ -> Error $"Version `{version}` of pipeline `{pipeline}` already exists."
        | None ->
            match Operations.selectPipelineRecord ctx [ "WHERE name = @0;" ] [ pipeline ] with
            | Some _ -> ()
            | None -> ({ Name = pipeline }: Parameters.NewPipeline) |> Operations.insertPipeline ctx

            ({ Id = id.Get()
               Pipeline = pipeline
               Version = version
               Description = description
               CreatedOn = timestamp () }
            : Parameters.NewPipelineVersion)
            |> Operations.insertPipelineVersion ctx
            |> Ok

    let addSpecificVersionTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (pipeline: string)
        (description: string)
        (version: int)
        =
        ctx.ExecuteInTransactionV2(fun t -> addSpecificVersion t id pipeline description version)

    let addVersion (ctx: SqliteContext) (pipeline: NewPipelineVersion) =
        match pipeline.Version with
        | ItemVersion.Latest -> addLatestVersion ctx pipeline.Id pipeline.Name pipeline.Description |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx pipeline.Id pipeline.Name pipeline.Description v

    let addVersionTransaction (ctx: SqliteContext) (pipeline: NewPipelineVersion) =
        ctx.ExecuteInTransactionV2(fun t -> addVersion t pipeline)

    let add (ctx: SqliteContext) (pipelineName: string) =
        ({ Name = pipelineName }: Parameters.NewPipeline)
        |> Operations.insertPipeline ctx

    let addTransaction (ctx: SqliteContext) (pipelineName: string) =
        ctx.ExecuteInTransaction(fun t -> add t pipelineName)

    let getPipelineArg (ctx: SqliteContext) (versionId: string) (name: string) =
        Operations.selectPipelineArgRecord ctx [ "WHERE pipeline_version_id = @0 AND name = @1;" ] [ versionId; name ]
        |> Option.map (fun pa ->
            ({ Name = pa.Name
               Required = pa.Required
               DefaultValue = pa.DefaultValue }
            : PipelineArg))

    let getPipelineArgs (ctx: SqliteContext) (versionId: string) =
        Operations.selectPipelineArgRecords ctx [ "WHERE pipeline_version_id = @0;" ] [ versionId ]
        |> List.map (fun pa ->
            ({ Name = pa.Name
               Required = pa.Required
               DefaultValue = pa.DefaultValue }
            : PipelineArg))

    let addPipelineArg (ctx: SqliteContext) (arg: NewPipelineArg) =
        match arg.Version with
        | ItemVersion.Latest -> getLatestVersionId ctx arg.Pipeline
        | ItemVersion.Specific v -> getVersionId ctx arg.Pipeline v
        |> Option.map (fun versionId ->
            match getPipelineArg ctx versionId arg.Name with
            | Some _ -> Error $"Pipeline arg `{arg.Name}` already exists for pipeline version `{versionId}`."
            | None ->
                ({ Id = arg.Id.Get()
                   PipelineVersionId = versionId
                   Name = arg.Name
                   Required = arg.Required
                   DefaultValue = arg.DefaultValue }
                : Parameters.NewPipelineArg)
                |> Operations.insertPipelineArg ctx
                |> Ok)
        |> Option.defaultWith (fun _ ->
            Error $"Version `{arg.Version.ToLabel()}` of pipeline `{arg.Pipeline}` not found")

    let addPipelineArgTransaction (ctx: SqliteContext) (arg: NewPipelineArg) =
        ctx.ExecuteInTransactionV2(fun t -> addPipelineArg t arg)

    let addPipelineResource (ctx: SqliteContext) (resource: NewPipelineResource) =
        ({ Id = resource.Id.Get()
           PipelineVersionId = resource.PipelineVersionId
           ResourceVersionId = resource.ResourceVersionId }
        : Parameters.NewPipelineResource)
        |> Operations.insertPipelineResource ctx

    let addPipelineResourceTransaction (ctx: SqliteContext) (resource: NewPipelineResource) =
        ctx.ExecuteInTransaction(fun t -> addPipelineResource t resource)
