namespace FPype.Interoperability.Common.Actions

module Import =

    open System.Text.Json.Serialization
    open FPype.Actions

    type ImportFileAction =
        { [<JsonPropertyName "path">]
          Path: string
          [<JsonPropertyName "dataSourceName">]
          DataSourceName: string }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Import.``import-file``.name

            member this.ToSerializedActionParameters() =
                ({ Path = this.Path
                   Name = this.DataSourceName }
                : Import.``import-file``.Parameters)
                |> serializeAsJson

    type ChunkFileAction =
        { [<JsonPropertyName "path">]
          Path: string
          [<JsonPropertyName "collectionName">]
          CollectionName: string
          [<JsonPropertyName "chunkSize">]
          ChunkSize: int }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Import.``chunk-file``.name

            member this.ToSerializedActionParameters() =
                ({ Path = this.Path
                   CollectionName = this.CollectionName
                   ChunkSize = this.ChunkSize }
                : Import.``chunk-file``.Parameters)
                |> serializeAsJson

    // TODO unzip-file

    type HttpGetAction =
        { [<JsonPropertyName "url">]
          Url: string
          [<JsonPropertyName "additionalHeaders">]
          AdditionHeaders: StringKeyValue list
          [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "responseType">]
          ResponseType: string option
          [<JsonPropertyName "collection">]
          Collection: string option }


        interface IPipelineAction with
        
            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Import.``http-get``.name

            member this.ToSerializedActionParameters() =
                ({ Url = this.Url
                   AdditionalHeaders = this.AdditionHeaders |> List.map (fun kv -> kv.Key, kv.Value) |> Map.ofList
                   Name = this.Name
                   ResponseType = this.ResponseType
                   Collection = this.Collection }
                : Import.``http-get``.Parameters)
                |> serializeAsJson
