module FPype.Infrastructure.Services

open System
open Microsoft.Extensions.Logging
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results

type ConfigurationService(ctx: MySqlContext, log: ILogger<ConfigurationService>) =

    member _.AddPipeline(userReference: string, pipeline: Configuration.Pipelines.Models.NewPipeline) =
        Configuration.Pipelines.CreateOperations.pipeline ctx log userReference pipeline

    member _.AddPipelineVersion(userReference, pipelineReference, version) =
        Configuration.Pipelines.CreateOperations.pipelineVersion ctx log userReference pipelineReference version

    member _.AddPipelineAction(userReference, versionReference, action) =
        Configuration.Pipelines.CreateOperations.pipelineActions ctx log userReference versionReference action

    member _.GetLatestPipelineVersion(userReference, pipelineReference) =
        Configuration.Pipelines.ReadOperations.latestPipelineVersion ctx log userReference pipelineReference

    member _.GetSpecificPipelineVersion(userReference, pipelineReference, version) =
        Configuration.Pipelines.ReadOperations.specificPipelineVersion ctx log userReference pipelineReference version

    member _.AddTable(userReference, table) =
        Configuration.Tables.CreateOperations.table ctx log userReference table

    member _.AddTableVersion(userReference, tableReference, version) =
        Configuration.Tables.CreateOperations.tableVersion ctx log userReference tableReference version

    member _.GetLatestTableVersion(userReference, tableReference) =
        Configuration.Tables.ReadOperations.latestTableVersion ctx log userReference tableReference

    member _.GetSpecificTableVersion(userReference, tableReference, version) =
        Configuration.Tables.ReadOperations.specificTableVersion ctx log userReference tableReference version

    member _.AddQuery(userReference, query) =
        Configuration.Queries.CreateOperations.query ctx log userReference query

    member _.AddQueryVersion(userReference, queryReference, version) =
        Configuration.Queries.CreateOperations.queryVersion ctx log userReference queryReference version

    member _.GetLatestQueryVersion(userReference, queryReference) =
        Configuration.Queries.ReadOperations.latestQueryVersion ctx log userReference queryReference

    member _.GetSpecificQueryVersion(userReference, queryReference, version) =
        Configuration.Queries.ReadOperations.specificQueryVersion ctx log userReference queryReference version

    member _.AddResource(userReference, resource) =
        Configuration.Resources.CreateOperations.resource ctx log userReference resource

    member _.AddResourceVersion(userReference, resourceReference, version) =
        Configuration.Resources.CreateOperations.resourceVersion ctx log userReference resourceReference version

    member _.GetLatestResourceVersion(userReference, resourceReference) =
        Configuration.Resources.ReadOperations.latestResourceVersion ctx log userReference resourceReference

    member _.GetSpecificResourceVersion(userReference, resourceReference, version) =
        Configuration.Resources.ReadOperations.specificResourceVersion ctx log userReference resourceReference version


    member _.AddTableObjectMapper(userReference, mapper) =
        Configuration.TableObjectMappers.CreateOperations.tableObjectMapper ctx log userReference mapper

    member _.AddTableObjectMapperVersion(userReference, mapperReference, version) =
        Configuration.TableObjectMappers.CreateOperations.tableObjectMapperVersion
            ctx
            log
            userReference
            mapperReference
            version

    member _.GetLatestTableObjectMapperVersion(userReference, mapperReference) =
        Configuration.TableObjectMappers.ReadOperations.latestTableObjectMapperVersion
            ctx
            log
            userReference
            mapperReference

    member _.GetSpecificTableObjectMapperVersion(userReference, mapperReference, version) =
        Configuration.TableObjectMappers.ReadOperations.specificTableObjectMapperVersion
            ctx
            log
            userReference
            mapperReference
            version

    member _.AddObjectTableMapper(userReference, mapper) =
        Configuration.ObjectTableMappers.CreateOperations.objectTableMapper ctx log userReference mapper

    member _.AddObjectTableMapperVersion(userReference, mapperReference, version) =
        Configuration.ObjectTableMappers.CreateOperations.objectTableMapperVersion
            ctx
            log
            userReference
            mapperReference
            version

    member _.GetLatestObjectTableMapperVersion(userReference, mapperReference) =
        Configuration.ObjectTableMappers.ReadOperations.latestObjectTableMapperVersion
            ctx
            log
            userReference
            mapperReference

    member _.GetSpecificObjectTableMapperVersion(userReference, mapperReference, version) =
        Configuration.ObjectTableMappers.ReadOperations.specificObjectTableMapperVersion
            ctx
            log
            userReference
            mapperReference
            version
