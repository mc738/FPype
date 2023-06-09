namespace FPype.Infrastructure.Configuration.Common

open System
open System.Text.Json.Serialization
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results
open Microsoft.Extensions.Logging

module Events =

    type ConfigurationEvent =
        // Pipelines
        | PipelineAdded of PipelineAddedEvent
        | PipelineVersionAdded of PipelineVersionAddedEvent
        | PipelineActionAdded of PipelineActionAddedEvent
        | PipelineResourceAdded of PipelineResourceAddedEvent
        | PipelineArgAdded of PipelineArgAddedEvent

        // Tables
        | TableAdded of TableAddedEvent
        | TableVersionAdded of TableVersionAddedEvent
        | TableColumnAdded of TableColumnAddedEvent

        // Queries
        | QueryAdded of QueryAddedEvent
        | QueryVersionAdded of QueryVersionAddedEvent

        // Resources
        | ResourceAdded of ResourceAddedEvent
        | ResourceVersionAdded of ResourceVersionAddedEvent

        // Table object mappers
        | TableObjectMapperAdded of TableObjectMapperAddedEvent
        | TableObjectMapperVersionAdded of TableObjectMapperVersionAddedEvent

        // Object table mappers
        | ObjectTableMapperAdded of ObjectTableMapperAddedEvent
        | ObjectTableMapperVersionAdded of ObjectTableMapperVersionAddedEvent

        static member TryDeserialize(name: string, data: string) =
            match name with
            | _ when name = PipelineAddedEvent.Name() ->
                fromJson<PipelineAddedEvent> data |> Result.map ConfigurationEvent.PipelineAdded
            | _ when name = PipelineVersionAddedEvent.Name() ->
                fromJson<PipelineVersionAddedEvent> data
                |> Result.map ConfigurationEvent.PipelineVersionAdded
            | _ when name = PipelineActionAddedEvent.Name() ->
                fromJson<PipelineActionAddedEvent> data
                |> Result.map ConfigurationEvent.PipelineActionAdded
            | _ when name = PipelineResourceAddedEvent.Name() ->
                fromJson<PipelineResourceAddedEvent> data
                |> Result.map ConfigurationEvent.PipelineResourceAdded
            | _ when name = PipelineArgAddedEvent.Name() ->
                fromJson<PipelineArgAddedEvent> data
                |> Result.map ConfigurationEvent.PipelineArgAdded
            | _ when name = TableAddedEvent.Name() ->
                fromJson<TableAddedEvent> data |> Result.map ConfigurationEvent.TableAdded
            | _ when name = TableVersionAddedEvent.Name() ->
                fromJson<TableVersionAddedEvent> data
                |> Result.map ConfigurationEvent.TableVersionAdded
            | _ when name = TableColumnAddedEvent.Name() ->
                fromJson<TableColumnAddedEvent> data
                |> Result.map ConfigurationEvent.TableColumnAdded
            | _ when name = QueryAddedEvent.Name() ->
                fromJson<QueryAddedEvent> data |> Result.map ConfigurationEvent.QueryAdded
            | _ when name = QueryVersionAddedEvent.Name() ->
                fromJson<QueryVersionAddedEvent> data
                |> Result.map ConfigurationEvent.QueryVersionAdded
            | _ when name = ResourceAddedEvent.Name() ->
                fromJson<ResourceAddedEvent> data |> Result.map ConfigurationEvent.ResourceAdded
            | _ when name = ResourceVersionAddedEvent.Name() ->
                fromJson<ResourceVersionAddedEvent> data
                |> Result.map ConfigurationEvent.ResourceVersionAdded
            | _ when name = TableObjectMapperAddedEvent.Name() ->
                fromJson<TableObjectMapperAddedEvent> data
                |> Result.map ConfigurationEvent.TableObjectMapperAdded
            | _ when name = TableObjectMapperVersionAddedEvent.Name() ->
                fromJson<TableObjectMapperVersionAddedEvent> data
                |> Result.map ConfigurationEvent.TableObjectMapperVersionAdded
            | _ when name = ObjectTableMapperAddedEvent.Name() ->
                fromJson<ObjectTableMapperAddedEvent> data
                |> Result.map ConfigurationEvent.ObjectTableMapperAdded
            | _ when name = ObjectTableMapperVersionAddedEvent.Name() ->
                fromJson<ObjectTableMapperVersionAddedEvent> data
                |> Result.map ConfigurationEvent.ObjectTableMapperVersionAdded
            | _ ->
                let message = $"Unknowing configuration event type: `{name}`"

                Error(
                    { Message = message
                      DisplayMessage = message
                      Exception = None }
                    : FailureResult
                )

        member ce.Serialize() =
            match ce with
            | PipelineAdded data -> toJson data |> Result.map (fun r -> PipelineAddedEvent.Name(), r)
            | PipelineVersionAdded data -> toJson data |> Result.map (fun r -> PipelineVersionAddedEvent.Name(), r)
            | PipelineActionAdded data -> toJson data |> Result.map (fun r -> PipelineActionAddedEvent.Name(), r)
            | PipelineResourceAdded data -> toJson data |> Result.map (fun r -> PipelineResourceAddedEvent.Name(), r)
            | PipelineArgAdded data -> toJson data |> Result.map (fun r -> PipelineArgAddedEvent.Name(), r)
            | TableAdded data -> toJson data |> Result.map (fun r -> TableAddedEvent.Name(), r)
            | TableVersionAdded data -> toJson data |> Result.map (fun r -> TableVersionAddedEvent.Name(), r)
            | TableColumnAdded data -> toJson data |> Result.map (fun r -> TableColumnAddedEvent.Name(), r)
            | QueryAdded data -> toJson data |> Result.map (fun r -> QueryAddedEvent.Name(), r)
            | QueryVersionAdded data -> toJson data |> Result.map (fun r -> QueryVersionAddedEvent.Name(), r)
            | ResourceAdded data -> toJson data |> Result.map (fun r -> ResourceAddedEvent.Name(), r)
            | ResourceVersionAdded data -> toJson data |> Result.map (fun r -> ResourceVersionAddedEvent.Name(), r)
            | TableObjectMapperAdded data -> toJson data |> Result.map (fun r -> TableObjectMapperAddedEvent.Name(), r)
            | TableObjectMapperVersionAdded data ->
                toJson data
                |> Result.map (fun r -> TableObjectMapperVersionAddedEvent.Name(), r)
            | ObjectTableMapperAdded data -> toJson data |> Result.map (fun r -> ObjectTableMapperAddedEvent.Name(), r)
            | ObjectTableMapperVersionAdded data ->
                toJson data
                |> Result.map (fun r -> ObjectTableMapperVersionAddedEvent.Name(), r)

    and [<CLIMutable>] PipelineAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("pipelineName")>]
          PipelineName: string }

        static member Name() = "pipeline-added"

    and [<CLIMutable>] PipelineVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("pipelineReference")>]
          PipelineReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("description")>]
          Description: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "pipeline-version-added"

    and [<CLIMutable>] PipelineActionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("versionReference")>]
          VersionReference: string
          [<JsonPropertyName("actionName")>]
          ActionName: string
          [<JsonPropertyName("actionType")>]
          ActionType: string
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("step")>]
          Step: int }

        static member Name() = "pipeline-action-added"

    and [<CLIMutable>] PipelineResourceAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("versionReference")>]
          VersionReference: string
          [<JsonPropertyName("resourceReference")>]
          ResourceReference: string }

        static member Name() = "pipeline-resource-added"

    and [<CLIMutable>] PipelineArgAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("versionReference")>]
          VersionReference: string
          [<JsonPropertyName("argName")>]
          ArgName: string
          [<JsonPropertyName("required")>]
          Required: bool
          [<JsonPropertyName("defaultValue")>]
          DefaultValue: string }

        static member Name() = "pipeline-arg-added"

    and [<CLIMutable>] TableAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("tableName")>]
          TableName: string }

        static member Name() = "table-added"

    and [<CLIMutable>] TableVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("tableReference")>]
          TableReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "table-version-added"

    and [<CLIMutable>] TableColumnAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("versionReference")>]
          VersionReference: string
          [<JsonPropertyName("columnName")>]
          ColumnName: string
          [<JsonPropertyName("dataType")>]
          DataType: string
          [<JsonPropertyName("optional")>]
          Optional: bool
          [<JsonPropertyName("columnIndex")>]
          ColumnIndex: int }

        static member Name() = "table-column-added"

    and [<CLIMutable>] QueryAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("queryName")>]
          QueryName: string }

        static member Name() = "query-added"

    and [<CLIMutable>] QueryVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("queryReference")>]
          QueryReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "query-version-added"

    and [<CLIMutable>] ResourceAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("resourceName")>]
          ResourceName: string }

        static member Name() = "resource-added"

    and [<CLIMutable>] ResourceVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("resourceReference")>]
          ResourceReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "resource-version-added"

    and [<CLIMutable>] TableObjectMapperAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("mapperName")>]
          MapperName: string }

        static member Name() = "table-object-mapper-added"

    and [<CLIMutable>] TableObjectMapperVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("mapperReference")>]
          MapperReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "table-object-mapper-version-added"

    and [<CLIMutable>] ObjectTableMapperAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("mapperName")>]
          MapperName: string }

        static member Name() = "object-table-mapper-added"

    and [<CLIMutable>] ObjectTableMapperVersionAddedEvent =
        { [<JsonPropertyName("reference")>]
          Reference: string
          [<JsonPropertyName("mapperReference")>]
          MapperReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "object-table-mapper-version-added"

    let addEvents
        (ctx: MySqlContext)
        (log: ILogger)
        (subscriptionId: int)
        (userId: int)
        (timestamp: DateTime)
        (events: ConfigurationEvent list)
        =
        let batchReference = createReference ()

        events
        |> List.fold
            (fun last e ->
                match e.Serialize() with
                | Ok(name, data) ->
                    ({ SubscriptionId = subscriptionId
                       EventType = name
                       EventTimestamp = timestamp
                       EventData = data
                       UserId = userId
                       BatchReference = batchReference }
                    : Parameters.NewConfigurationEvent)
                    |> Operations.insertConfigurationEvent ctx

                | Error e -> last)
            0UL
        |> int

    let selectEventRecords (ctx: MySqlContext) (subscriptionId: int) (previousTip: int) =
        Operations.selectConfigurationEventRecords
            ctx
            [ "WHERE subscription_id = @0 AND id > @1" ]
            [ subscriptionId; previousTip ]

    let selectTip (ctx: MySqlContext) (subscriptionId: int) =
        Operations.selectConfigurationEventRecord
            ctx
            [ "WHERE subscription_id = @0 ORDER BY id DESC" ]
            [ subscriptionId ]
        |> Option.map (fun er -> er.Id)
        |> Option.defaultValue 0

    let selectGlobalTip (ctx: MySqlContext) =
        Operations.selectConfigurationEventRecord ctx [ "ORDER BY id DESC" ] []
        |> Option.map (fun er -> er.Id)
        |> Option.defaultValue 0

    let deserializeRecords (events: Records.ConfigurationEvent list) =
        events
        |> List.map (fun ce ->
            ConfigurationEvent.TryDeserialize(ce.EventType, ce.EventData)
            |> FetchResult.fromResult)

    let selectEvents (ctx: MySqlContext) (subscriptionId: int) (previousTip: int) =
        selectEventRecords ctx subscriptionId previousTip
        |> deserializeRecords
