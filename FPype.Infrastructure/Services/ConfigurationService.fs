namespace FPype.Infrastructure.Services

open System
open Microsoft.Extensions.Logging
open FPype.Infrastructure
open FPype.Infrastructure.Configuration
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results

type ConfigurationService(serviceContext: ServiceContext, log: ILogger<ConfigurationService>) =

    let ctx = serviceContext.GetContext()
    
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

    member _.GetPipelines(userReference) =
        Configuration.Pipelines.ReadOperations.pipelines ctx log userReference

    member _.GetPipeline(userReference, pipelineReference) =
        Configuration.Pipelines.ReadOperations.pipeline ctx log userReference pipelineReference

    member _.GetPipelineVersions(userReference, pipelineReference) =
        Configuration.Pipelines.ReadOperations.pipelineVersions ctx log userReference pipelineReference

    member _.GetSubscriptionPipelines(userReference) =
        Configuration.Pipelines.ReadOperations.subscriptionPipelines ctx log userReference

    member _.AddTable(userReference, table) =
        Configuration.Tables.CreateOperations.table ctx log userReference table

    member _.AddTableVersion(userReference, tableReference, version) =
        Configuration.Tables.CreateOperations.tableVersion ctx log userReference tableReference version

    member _.GetLatestTableVersion(userReference, tableReference) =
        Configuration.Tables.ReadOperations.latestTableVersion ctx log userReference tableReference

    member _.GetSpecificTableVersion(userReference, tableReference, version) =
        Configuration.Tables.ReadOperations.specificTableVersion ctx log userReference tableReference version

    member _.GetTables(userReference) =
        Configuration.Tables.ReadOperations.tables ctx log userReference

    member _.GetTable(userReference, tableReference) =
        Configuration.Tables.ReadOperations.table ctx log userReference tableReference

    member _.GetTableVersions(userReference, tableReference) =
        Configuration.Tables.ReadOperations.tableVersions ctx log userReference tableReference

    member _.AddRawQuery(userReference, query) =
        Configuration.Queries.CreateOperations.rawQuery ctx log userReference query

    member _.AddSerializedQuery(userReference, query) =
        Configuration.Queries.CreateOperations.serializedQuery ctx log userReference query

    member _.AddRawQueryVersion(userReference, queryReference, version) =
        Configuration.Queries.CreateOperations.rawQueryVersion ctx log userReference queryReference version

    member _.AddSerializedQueryVersion(userReference, queryReference, version) =
        Configuration.Queries.CreateOperations.serializedQueryVersion ctx log userReference queryReference version

    member _.GetLatestQueryVersion(userReference, queryReference) =
        Configuration.Queries.ReadOperations.latestQueryVersion ctx log userReference queryReference

    member _.GetSpecificQueryVersion(userReference, queryReference, version) =
        Configuration.Queries.ReadOperations.specificQueryVersion ctx log userReference queryReference version

    member _.GetQueries(userReference) =
        Configuration.Queries.ReadOperations.queries ctx log userReference

    member _.GetQuery(userReference, queryReference) =
        Configuration.Queries.ReadOperations.query ctx log userReference queryReference

    member _.GetQueryVersions(userReference, queryReference) =
        Configuration.Queries.ReadOperations.queryVersions ctx log userReference queryReference

    member _.AddResource(userReference, resource) =
        Configuration.Resources.CreateOperations.resource ctx log userReference resource

    member _.AddResourceVersion(userReference, resourceReference, version) =
        Configuration.Resources.CreateOperations.resourceVersion ctx log userReference resourceReference version

    member _.GetLatestResourceVersion(userReference, resourceReference) =
        Configuration.Resources.ReadOperations.latestResourceVersion ctx log userReference resourceReference

    member _.GetSpecificResourceVersion(userReference, resourceReference, version) =
        Configuration.Resources.ReadOperations.specificResourceVersion ctx log userReference resourceReference version

    member _.GetResources(userReference) =
        Configuration.Resources.ReadOperations.resources ctx log userReference

    member _.GetResourceVersions(userReference, resourceReference) =
        Configuration.Resources.ReadOperations.resourceVersions ctx log userReference resourceReference

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

    member _.GetTableObjectMappers(userReference) =
        Configuration.TableObjectMappers.ReadOperations.tableObjectMappers ctx log userReference

    member _.GetTableObjectMapperVersions(userReference, mapperReference) =
        Configuration.TableObjectMappers.ReadOperations.tableObjectMapperVersions ctx log userReference mapperReference

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

    member _.GetObjectTableMappers(userReference) =
        Configuration.ObjectTableMappers.ReadOperations.objectTableMappers ctx log userReference

    member _.GetObjectTableMapperVersions(userReference, mapperReference) =
        Configuration.ObjectTableMappers.ReadOperations.objectTableMapperVersions ctx log userReference mapperReference

    member _.BuildConfigurationStore(fileRepo, readArgs, subscriptionId, path, failOnError, additionalActions) =
        buildStore ctx log fileRepo readArgs subscriptionId path failOnError additionalActions
