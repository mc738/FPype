namespace FPype.Infrastructure.Configuration.Common

open System
open System.Text.Json.Serialization

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
                fromJson<PipelineVersionAddedEvent> data |> Result.map ConfigurationEvent.PipelineVersionAdded
            | _ when name = PipelineActionAddedEvent.Name() ->
                fromJson<PipelineActionAddedEvent> data |> Result.map ConfigurationEvent.PipelineActionAdded
            | _ when name = PipelineResourceAddedEvent.Name() ->
                fromJson<PipelineResourceAddedEvent> data |> Result.map ConfigurationEvent.PipelineResourceAdded
            | _ when name = PipelineArgAddedEvent.Name() ->
                fromJson<PipelineArgAddedEvent> data |> Result.map ConfigurationEvent.PipelineArgAdded
            | _ when name = TableAddedEvent.Name() ->
                fromJson<TableAddedEvent> data |> Result.map ConfigurationEvent.TableAdded
            | _ when name = TableVersionAddedEvent.Name() ->
                fromJson<TableVersionAddedEvent> data |> Result.map ConfigurationEvent.TableVersionAdded
            | _ when name = TableColumnAddedEvent.Name() ->
                fromJson<TableColumnAddedEvent> data |> Result.map ConfigurationEvent.TableColumnAdded
            | _ when name = QueryAddedEvent.Name() ->
                fromJson<QueryAddedEvent> data |> Result.map ConfigurationEvent.QueryAdded
            | _ when name = QueryVersionAddedEvent.Name() ->
                fromJson<QueryVersionAddedEvent> data |> Result.map ConfigurationEvent.QueryVersionAdded
            
    



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
          mapperReference: string
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
          mapperReference: string
          [<JsonPropertyName("version")>]
          Version: int
          [<JsonPropertyName("hash")>]
          Hash: string
          [<JsonPropertyName("createdOn")>]
          CreatedOnDateTime: DateTime }

        static member Name() = "object-table-mapper-version-added"
