﻿namespace FPype.Configuration

open FPype.Configuration.Persistence
open Freql.Sqlite

module Pipelines =


    let getLatestVersion (ctx: SqliteContext) (pipeline: string) =
        Operations.selectPipelineVersionRecord ctx [ "WHERE pipeline = @0 ORDER BY version DESC LIMIT 1;" ] [ pipeline ]

    let getVersion (ctx: SqliteContext) (pipeline: string) (version: int) =
        Operations.selectPipelineVersionRecord ctx [ "WHERE pipeline = @0 AND version = @1;" ] [ pipeline ]

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
           CreatedOn = timestamp () }: Parameters.NewPipelineVersion)
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
               CreatedOn = timestamp () }: Parameters.NewPipelineVersion)
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

    let add (ctx: SqliteContext) (id: IdType) (pipeline: string) (description: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> addLatestVersion ctx id pipeline description |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx id pipeline description v

    let addTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (pipeline: string)
        (description: string)
        (version: ItemVersion)
        =
        ctx.ExecuteInTransactionV2(fun t -> add t id pipeline description version)

    