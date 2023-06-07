namespace FPype.Infrastructure.Configuration.Common

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
            { Message = "Unhandled exception while fetching subscription"
              DisplayMessage = "Error fetching subscription"
              Exception = Some ex }
            |> FetchResult.Failure

    let subscriptionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectSubscriptionRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Subscription (id: {id}) not found"
                   DisplayMessage = "Subscription not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching subscription"
              DisplayMessage = "Error fetching subscription"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching user"
              DisplayMessage = "Error fetching user"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching pipeline"
              DisplayMessage = "Error fetching pipeline"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching pipeline"
              DisplayMessage = "Error fetching pipeline"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelinesBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectPipelineRecords ctx [ "WHERE subscription_id = @0;" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching pipelines"
              DisplayMessage = "Error fetching pipelines"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching latest pipeline version"
              DisplayMessage = "Error fetching latest pipeline version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching pipeline version"
              DisplayMessage = "Error fetching latest pipeline version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching pipeline version"
              DisplayMessage = "Error fetching pipeline version"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineVersionById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectPipelineVersionRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline version (id: {id}) not found"
                   DisplayMessage = "Pipeline versions not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching pipeline version"
              DisplayMessage = "Error fetching pipeline version"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineVersionsByPipelineId (ctx: MySqlContext) (pipelineId: int) =
        try
            Operations.selectPipelineVersionRecords ctx [ "WHERE pipeline_id = @0" ] [ pipelineId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching pipeline version"
              DisplayMessage = "Error fetching pipeline version"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineResourceByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineResourceRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline resource (ref: {reference}) not found"
                   DisplayMessage = "Pipeline resource not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching pipeline resource"
              DisplayMessage = "Error fetching pipeline resource"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineResourcesByPipelineVersionId (ctx: MySqlContext) (versionId: int) =
        try
            Operations.selectPipelineResourceRecords ctx [ "WHERE pipeline_version_id = @0" ] [ versionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching pipeline resources"
              DisplayMessage = "Error fetching pipeline resources"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineArgByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineArgRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline arg (ref: {reference}) not found"
                   DisplayMessage = "Pipeline arg not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching pipeline arg"
              DisplayMessage = "Error fetching pipeline arg"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineArgByVersionId (ctx: MySqlContext) (versionId: int) =
        try
            Operations.selectPipelineArgRecords ctx [ "WHERE pipeline_version_id = @0" ] [ versionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching pipeline args"
              DisplayMessage = "Error fetching pipeline args"
              Exception = Some ex }
            |> FetchResult.Failure
    
    let pipelineActionByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectPipelineActionRecord ctx [ "WHERE reference = @0" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Pipeline action (ref: {reference}) not found"
                   DisplayMessage = "Pipeline action not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching pipeline action"
              DisplayMessage = "Error fetching pipeline action"
              Exception = Some ex }
            |> FetchResult.Failure

    let pipelineActions (ctx: MySqlContext) (pipelineVersionId: int) =
        try
            Operations.selectPipelineActionRecords ctx [ "WHERE pipeline_version_id = @0" ] [ pipelineVersionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching pipeline actions"
              DisplayMessage = "Error fetching pipeline actions"
              Exception = Some ex }
            |> FetchResult.Failure

    let actionTypeById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectActionTypeRecord ctx [ "WHERE id = @0" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Action type (id: {id}) not found"
                   DisplayMessage = "Action type not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching action type"
              DisplayMessage = "Error fetching action type"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table"
              DisplayMessage = "Error fetching table"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table"
              DisplayMessage = "Error fetching table"
              Exception = Some ex }
            |> FetchResult.Failure

    let tablesBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectTableModelRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table"
              DisplayMessage = "Error fetching table"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table latest version"
              DisplayMessage = "Error fetching latest table version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table version"
              DisplayMessage = "Error fetching table version"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableVersions (ctx: MySqlContext) (tableId: int) =
        try
            Operations.selectTableModelVersionRecords ctx [ "WHERE table_id = @0 AND version = @1;" ] [ tableId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table version"
              DisplayMessage = "Error fetching table version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table version"
              DisplayMessage = "Error fetching table version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table version"
              DisplayMessage = "Error fetching table version"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableVersionsByTableId (ctx: MySqlContext) (id: int) =
        try
            Operations.selectTableModelVersionRecords ctx [ "WHERE table_id = @0;" ] [ id ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table version"
              DisplayMessage = "Error fetching table version"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableColumns (ctx: MySqlContext) (tableVersionId: int) =
        try
            Operations.selectTableColumnRecords ctx [ "WHERE table_version_id = @0" ] [ tableVersionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table columns"
              DisplayMessage = "Error fetching table columns"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableColumnByReference (ctx: MySqlContext) (reference: string) =
        try
            Operations.selectTableColumnRecord ctx [ "WHERE reference = @0;" ] [ reference ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Table column (ref: {reference}) not found"
                   DisplayMessage = "Table column not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching table column"
              DisplayMessage = "Error fetching table column"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching query"
              DisplayMessage = "Error fetching query"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching query"
              DisplayMessage = "Error fetching query"
              Exception = Some ex }
            |> FetchResult.Failure

    let queriesBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectQueryRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching query"
              DisplayMessage = "Error fetching query"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching query latest version"
              DisplayMessage = "Error fetching latest query version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching query version"
              DisplayMessage = "Error fetching query version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching query version"
              DisplayMessage = "Error fetching query version"
              Exception = Some ex }
            |> FetchResult.Failure
            
    let queryVersionsByQueryId (ctx: MySqlContext) (queryId: int) =
        try
            Operations.selectQueryVersionRecords ctx [ "WHERE query_id = @0;" ] [ queryId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching query version"
              DisplayMessage = "Error fetching query version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching resource"
              DisplayMessage = "Error fetching resource"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching resource"
              DisplayMessage = "Error fetching resource"
              Exception = Some ex }
            |> FetchResult.Failure

    
    let resourcesBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectResourceRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching resource"
              DisplayMessage = "Error fetching resource"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching resource latest version"
              DisplayMessage = "Error fetching latest resource version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching resource version"
              DisplayMessage = "Error fetching resource version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching resource version"
              DisplayMessage = "Error fetching resource version"
              Exception = Some ex }
            |> FetchResult.Failure

    let resourceVersionById (ctx: MySqlContext) (id: int) =
        try
            Operations.selectResourceVersionRecord ctx [ "WHERE id = @0;" ] [ id ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Resource version (id: {id}) not found"
                   DisplayMessage = "Resource version not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching resource version"
              DisplayMessage = "Error fetching resource version"
              Exception = Some ex }
            |> FetchResult.Failure

    let resourceVersionsByResourceId (ctx: MySqlContext) (resourceId: int) =
        try
            Operations.selectResourceVersionRecords ctx [ "WHERE resource_id = @0;" ] [ resourceId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching resource version"
              DisplayMessage = "Error fetching resource version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table object mapper"
              DisplayMessage = "Error fetching table object mapper"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table object mapper"
              DisplayMessage = "Error fetching table object mapper"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableObjectMappersBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectTableObjectMapperRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table object mapper"
              DisplayMessage = "Error fetching table object mapper"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table object mapper latest version"
              DisplayMessage = "Error fetching latest table object mapper version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table object mapper version"
              DisplayMessage = "Error fetching table object mapper version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching table object mapper version"
              DisplayMessage = "Error fetching table object mapper version"
              Exception = Some ex }
            |> FetchResult.Failure

    let tableObjectMapperVersionByMapperId (ctx: MySqlContext) (mapperId: int) =
        try
            Operations.selectTableObjectMapperVersionRecords ctx [ "WHERE table_object_mapper_id = @0;" ] [ mapperId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching table object mapper versions"
              DisplayMessage = "Error fetching table object mapper versions"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching object table mapper"
              DisplayMessage = "Error fetching object table mapper"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching object table mapper"
              DisplayMessage = "Error fetching object table mapper"
              Exception = Some ex }
            |> FetchResult.Failure

    let objectTableMappersBySubscriptionId (ctx: MySqlContext) (subscriptionId: int) =
        try
            Operations.selectObjectTableMapperRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching object table mappers"
              DisplayMessage = "Error fetching object table mappers"
              Exception = Some ex }
            |> FetchResult.Failure
    
    let objectTableMapperLatestVersion (ctx: MySqlContext) (objectTableMapperId: int) =
        try
            Operations.selectObjectTableMapperVersionRecord
                ctx
                [ "WHERE object_table_mapper_id = @0 ORDER BY version DESC LIMIT 1;" ]
                [ objectTableMapperId ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Latest version of object table mapper (id: {objectTableMapperId}) not found"
                   DisplayMessage = "Latest version of object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching object table mapper latest version"
              DisplayMessage = "Error fetching latest object table mapper version"
              Exception = Some ex }
            |> FetchResult.Failure

    let objectTableMapperVersion (ctx: MySqlContext) (objectTableMapperId: int) (version: int) =
        try
            Operations.selectObjectTableMapperVersionRecord
                ctx
                [ "WHERE object_table_mapper_id = @0 AND version = @1;" ]
                [ objectTableMapperId; version ]
            |> Option.map FetchResult.Success
            |> Option.defaultWith (fun _ ->
                ({ Message = $"Version {version} of object table mapper (id: {objectTableMapperId}) not found"
                   DisplayMessage = $"Version {version} of object table mapper not found"
                   Exception = None }
                : FailureResult)
                |> FetchResult.Failure)
        with ex ->
            { Message = "Unhandled exception while fetching object table mapper version"
              DisplayMessage = "Error fetching object table mapper version"
              Exception = Some ex }
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
            { Message = "Unhandled exception while fetching object table mapper version"
              DisplayMessage = "Error fetching object table mapper version"
              Exception = Some ex }
            |> FetchResult.Failure
            
    let objectTableMapperVersionByMapperId (ctx: MySqlContext) (mapperId: int) =
        try
            Operations.selectObjectTableMapperVersionRecords ctx [ "WHERE object_table_mapper_id = @0;" ] [ mapperId ]
            |> FetchResult.Success
        with ex ->
            { Message = "Unhandled exception while fetching object table mapper versions"
              DisplayMessage = "Error fetching object table mapper versions"
              Exception = Some ex }
            |> FetchResult.Failure
