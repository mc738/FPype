﻿namespace FPype.Infrastructure.Configuration.Common

[<RequireQualifiedAccess>]
module Fetch =

    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let subscriptionById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectSubscriptionRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Subscription (id: {id}) not found"
                   DisplayMessage = "Subscription not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching subscription"
               DisplayMessage = "Error fetching subscription"
               Exception = Some ex })
            |> FetchResult.Failure

    let user (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectUserRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"User (ref: {reference}) not found"
                   DisplayMessage = "User not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching user"
               DisplayMessage = "Error fetching user"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipeline (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline (ref: {reference}) not found."
                   DisplayMessage = "Pipeline not found."
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline"
               DisplayMessage = "Error fetching pipeline"
               Exception = Some ex })
            |> FetchResult.Failure


    let pipelineById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectPipelineRecord ctx [ "WHERE id = @0;" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline (id: {id}) not found."
                   DisplayMessage = "Pipeline not found."
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline"
               DisplayMessage = "Error fetching pipeline"
               Exception = Some ex })
            |> FetchResult.Failure


    let pipelineLatestVersion (ctx: MySqlContext) (pipelineId: int) =
        try
            Operations.selectPipelineVersionRecord
                ctx
                [ "WHERE pipeline_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ pipelineId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of pipeline (id: {pipelineId}) not found"
                   DisplayMessage = "Latest version of pipeline not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching latest pipeline version"
               DisplayMessage = "Error fetching latest pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineVersion (ctx: MySqlContext) (pipelineId: int) (version: int) =
        try
            Operations.selectPipelineVersionRecord
                ctx
                [ "WHERE pipeline_id = @0 AND version = @1;" ]
                [ pipelineId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of pipeline (id: {pipelineId}) not found"
                   DisplayMessage = $"Version {version} of pipeline not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline version"
               DisplayMessage = "Error fetching latest pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineVersionRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline version (ref: {reference}) not found"
                   DisplayMessage = "Pipeline versions not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline version"
               DisplayMessage = "Error fetching pipeline version"
               Exception = Some ex })
            |> FetchResult.Failure

    let pipelineActions (ctx: MySqlContext) (pipelineVersionId: int) =
        try
            Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_version_id = @0" ] [ pipelineVersionId ]
            |> FetchResult.Success
        with ex ->
            ({ Message = "Unhandled exception while fetching pipeline actions"
               DisplayMessage = "Error fetching pipeline actions"
               Exception = Some ex })
            |> FetchResult.Failure

    // Tables
    let table (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableModelRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table (ref: {reference}) not found"
                   DisplayMessage = "Table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table"
               DisplayMessage = "Error fetching table"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectTableModelRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table (id: {id}) not found"
                   DisplayMessage = "Table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table"
               DisplayMessage = "Error fetching table"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableLatestVersion (ctx: MySqlContext) (tableId: int) =
        try
            Operations.selectTableModelVersionRecord
                ctx
                [ "WHERE table_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ tableId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of table (id: {tableId}) not found"
                   DisplayMessage = "Latest version of table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table latest version"
               DisplayMessage = "Error fetching latest table version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableVersion (ctx: MySqlContext) (tableId: int) (version: int) =
        try
            Operations.selectTableModelVersionRecord
                ctx
                [ "WHERE table_id = @0 AND version = @1;" ]
                [ tableId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of table (id: {tableId}) not found"
                   DisplayMessage = $"Version {version} of table not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table version"
               DisplayMessage = "Error fetching table version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableModelVersionRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table version (ref: {reference}) not found"
                   DisplayMessage = "Table version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table version"
               DisplayMessage = "Error fetching table version"
               Exception = Some ex })
            |> FetchResult.Failure
            
    let tableVersionById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectTableModelVersionRecord ctx [ "WHERE id = @0;" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table version (id: {id}) not found"
                   DisplayMessage = "Table version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table version"
               DisplayMessage = "Error fetching table version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableColumns (ctx: MySqlContext) (tableVersionId: int) =
        try
            Operations.selectTableColumnRecords ctx [ "WHERE table_version_id = @0" ] [ tableVersionId ]
            |> FetchResult.Success
        with ex ->
            ({ Message = "Unhandled exception while fetching table columns"
               DisplayMessage = "Error fetching table columns"
               Exception = Some ex })
            |> FetchResult.Failure

    // Queries
    let query (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectQueryRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Query (ref: {reference}) not found"
                   DisplayMessage = "Query not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching query"
               DisplayMessage = "Error fetching query"
               Exception = Some ex })
            |> FetchResult.Failure

    let queryById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectQueryRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Query (id: {id}) not found"
                   DisplayMessage = "Query not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching query"
               DisplayMessage = "Error fetching query"
               Exception = Some ex })
            |> FetchResult.Failure

    let queryLatestVersion (ctx: MySqlContext) (queryId: int) =
        try
            Operations.selectQueryVersionRecord ctx [ "WHERE query_id = @0 ORDER BY version DESC LIMIT 1;" ] [ queryId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of query (id: {queryId}) not found"
                   DisplayMessage = "Latest version of query not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching query latest version"
               DisplayMessage = "Error fetching latest query version"
               Exception = Some ex })
            |> FetchResult.Failure

    let queryVersion (ctx: MySqlContext) (queryId: int) (version: int) =
        try
            Operations.selectQueryVersionRecord ctx [ "WHERE query_id = @0 AND version = @1;" ] [ queryId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of query (id: {queryId}) not found"
                   DisplayMessage = $"Version {version} of query not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching query version"
               DisplayMessage = "Error fetching query version"
               Exception = Some ex })
            |> FetchResult.Failure

    let queryVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectQueryVersionRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Query version (ref: {reference}) not found"
                   DisplayMessage = "Query version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching query version"
               DisplayMessage = "Error fetching query version"
               Exception = Some ex })
            |> FetchResult.Failure

    // Resources
    let resource (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectResourceRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Resource (ref: {reference}) not found"
                   DisplayMessage = "Resource not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching resource"
               DisplayMessage = "Error fetching resource"
               Exception = Some ex })
            |> FetchResult.Failure

    let resourceById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectResourceRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Resource (id: {id}) not found"
                   DisplayMessage = "Resource not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching resource"
               DisplayMessage = "Error fetching resource"
               Exception = Some ex })
            |> FetchResult.Failure

    let resourceLatestVersion (ctx: MySqlContext) (resourceId: int) =
        try
            Operations.selectResourceVersionRecord
                ctx
                [ "WHERE resource_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ resourceId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of resource (id: {resourceId}) not found"
                   DisplayMessage = "Latest version of resource not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching resource latest version"
               DisplayMessage = "Error fetching latest resource version"
               Exception = Some ex })
            |> FetchResult.Failure

    let resourceVersion (ctx: MySqlContext) (resourceId: int) (version: int) =
        try
            Operations.selectResourceVersionRecord
                ctx
                [ "WHERE resource_id = @0 AND version = @1;" ]
                [ resourceId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of resource (id: {resourceId}) not found"
                   DisplayMessage = $"Version {version} of resource not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching resource version"
               DisplayMessage = "Error fetching resource version"
               Exception = Some ex })
            |> FetchResult.Failure

    let resourceVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectResourceVersionRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Resource version (ref: {reference}) not found"
                   DisplayMessage = "Resource version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching resource version"
               DisplayMessage = "Error fetching resource version"
               Exception = Some ex })
            |> FetchResult.Failure

    // Table object mappers
    let tableObjectMapper (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableObjectMapperRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table object mapper (ref: {reference}) not found"
                   DisplayMessage = "Table object mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table object mapper"
               DisplayMessage = "Error fetching table object mapper"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableObjectMapperById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectTableObjectMapperRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table object mapper (id: {id}) not found"
                   DisplayMessage = "Table object mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table object mapper"
               DisplayMessage = "Error fetching table object mapper"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableObjectMapperLatestVersion (ctx: MySqlContext) (tableObjectMapperId: int) =
        try
            Operations.selectTableObjectMapperVersionRecord
                ctx
                [ "WHERE table_object_mapper_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ tableObjectMapperId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of table object mapper (id: {tableObjectMapperId}) not found"
                   DisplayMessage = "Latest version of table object mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table object mapper latest version"
               DisplayMessage = "Error fetching latest table object mapper version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableObjectMapperVersion (ctx: MySqlContext) (tableObjectMapperId: int) (version: int) =
        try
            Operations.selectTableObjectMapperVersionRecord
                ctx
                [ "WHERE table_object_mapper_id = @0 AND version = @1;" ]
                [ tableObjectMapperId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of query (id: {tableObjectMapperId}) not found"
                   DisplayMessage = $"Version {version} of query not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table object mapper version"
               DisplayMessage = "Error fetching table object mapper version"
               Exception = Some ex })
            |> FetchResult.Failure

    let tableObjectMapperVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableObjectMapperVersionRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table object mapper version (ref: {reference}) not found"
                   DisplayMessage = "Table object mapper version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching table object mapper version"
               DisplayMessage = "Error fetching table object mapper version"
               Exception = Some ex })
            |> FetchResult.Failure

    // Object table mappers
    let objectTableMapper (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectObjectTableMapperRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Object table mapper (ref: {reference}) not found"
                   DisplayMessage = "Object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching object table mapper"
               DisplayMessage = "Error fetching object table mapper"
               Exception = Some ex })
            |> FetchResult.Failure

    let objectTableMapperById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectObjectTableMapperRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Object table mapper (id: {id}) not found"
                   DisplayMessage = "Object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching object table mapper"
               DisplayMessage = "Error fetching object table mapper"
               Exception = Some ex })
            |> FetchResult.Failure

    let objectTableMapperLatestVersion (ctx: MySqlContext) (objectTableMapperId: int) =
        try
            Operations.selectObjectTableMapperVersionRecord ctx [ "WHERE object_table_mapper_id = @0 ORDER BY version DESC LIMIT 1;" ] [ objectTableMapperId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of object table mapper (id: {objectTableMapperId}) not found"
                   DisplayMessage = "Latest version of object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching object table mapper latest version"
               DisplayMessage = "Error fetching latest object table mapper version"
               Exception = Some ex })
            |> FetchResult.Failure

    let objectTableMapperVersion (ctx: MySqlContext) (objectTableMapperId: int) (version: int) =
        try
            Operations.selectObjectTableMapperVersionRecord ctx [ "WHERE object_table_mapper_id = @0 AND version = @1;" ] [ objectTableMapperId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of object table mapper (id: {objectTableMapperId}) not found"
                   DisplayMessage = $"Version {version} of object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching object table mapper version"
               DisplayMessage = "Error fetching object table mapper version"
               Exception = Some ex })
            |> FetchResult.Failure

    let objectTableMapperVersionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectObjectTableMapperVersionRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Object table mapper version (ref: {reference}) not found"
                   DisplayMessage = "Object table mapper version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            ({ Message = "Unhandled exception while fetching object table mapper version"
               DisplayMessage = "Error fetching object table mapper version"
               Exception = Some ex })
            |> FetchResult.Failure