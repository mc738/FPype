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
        | PipelineArgAdded

        // Tables
        | TableAdded
        | TableVersionAdded
        | TableColumnAdded

        // Queries
        | QueryAdded
        | QueryVersionAdded

        // Resources
        | ResourceAdded
        | ResourceVersionAdded

        | TableObjectMapperAdded
        | TableObjectMapperVersionAdded

        | ObjectTableMapperAdded
        | ObjectTableMapperVersionAdded


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
    