namespace FPype.Scripting

open System
open System.IO.Pipes
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Channels
open FPype.Core.Types
open FPype.Data
open FPype.Data.Models
open FPype.Data.Store
open FsToolbox.Core

module Core =

    /// <summary>
    /// A collection so models specifically for use in scripting.
    /// NOTE - some of these might be similar/the same to entities in FPype.Data.Store.
    /// But these are what should be used in script clients.
    /// </summary>
    [<RequireQualifiedAccess>]
    module Models =

        [<CLIMutable>]
        type State =
            { [<JsonPropertyName("values")>]
              Values: StateValue seq }

            static member FromEntities(entities: Store.StateValue list) =
                { Values = entities |> List.map (fun sv -> { Key = sv.Name; Value = sv.Value }) }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<State> json |> Ok
                with ex ->
                    Error $"Failed to deserialize state model: {ex.Message}"

            member s.Serialize() = JsonSerializer.Serialize s

            member s.ToMap() =
                s.Values |> List.ofSeq |> List.map (fun sv -> sv.Key, sv.Value) |> Map.ofList

        and [<CLIMutable>] StateValue =
            { [<JsonPropertyName("key")>]
              Key: string
              [<JsonPropertyName("value")>]
              Value: string }

        [<CLIMutable>]
        type DataSource =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("type")>]
              Type: string
              [<JsonPropertyName("uri")>]
              Uri: string
              [<JsonPropertyName("collectionName")>]
              CollectionName: string }

            static member FromEntity(entity: Store.DataSource) =

                { Name = entity.Name
                  Type = entity.Type
                  Uri = entity.Uri
                  CollectionName = entity.CollectionName }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<DataSource> json |> Ok
                with ex ->
                    Error $"Failed to deserialize data source model: {ex.Message}"

            member ds.Serialize() = JsonSerializer.Serialize ds

        [<CLIMutable>]
        type Artifact =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("bucket")>]
              Bucket: string
              [<JsonPropertyName("type")>]
              Type: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string }

            static member FromEntity(entity: Store.Artifact) =
                { Name = entity.Name
                  Bucket = entity.Name
                  Type = entity.Type
                  Base64Data = entity.Data.ToBytes() |> Conversions.toBase64 }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<Artifact> json |> Ok
                with ex ->
                    Error $"Failed to deserialize artifact model: {ex.Message}"

            member a.Serialize() = JsonSerializer.Serialize a

            member a.GetBytes() =
                try
                    a.Base64Data |> Conversions.fromBase64 |> Ok
                with ex ->
                    Error $"Failed to convert from base64: {ex.Message}"

        [<CLIMutable>]
        type ArtifactBucket =
            { [<JsonPropertyName("artifacts")>]
              Artifacts: Artifact seq }

            static member FromEntities(artifacts: Store.Artifact list) =
                { Artifacts = artifacts |> List.map Artifact.FromEntity }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<Resource> json |> Ok
                with ex ->
                    Error $"Failed to deserialize artifact bucket model: {ex.Message}"

            member ab.Serialize() = JsonSerializer.Serialize ab

        [<CLIMutable>]
        type Resource =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("type")>]
              Type: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string
              [<JsonPropertyName("hash")>]
              Hash: string }

            static member FromEntity(entity: Store.Resource) =
                { Name = entity.Name
                  Type = entity.Type
                  Base64Data = entity.Data.ToBytes() |> Conversions.toBase64
                  Hash = entity.Hash }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<Resource> json |> Ok
                with ex ->
                    Error $"Failed to deserialize resource model: {ex.Message}"

            member r.Serialize() = JsonSerializer.Serialize r

            member r.GetBytes() =
                try
                    r.Base64Data |> Conversions.fromBase64 |> Ok
                with ex ->
                    Error $"Failed to convert from base64: {ex.Message}"

        [<CLIMutable>]
        type CacheItem =
            { [<JsonPropertyName("key")>]
              Key: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string
              [<JsonPropertyName("hash")>]
              Hash: string
              [<JsonPropertyName("createdOn")>]
              CreatedOn: DateTime
              [<JsonPropertyName("expiresOn")>]
              ExpiresOn: DateTime }

            static member FromEntity(entity: Store.CacheItem) =
                { Key = entity.ItemKey
                  Base64Data = entity.ItemValue.ToBytes() |> Conversions.toBase64
                  Hash = entity.Hash
                  CreatedOn = entity.CreatedOn
                  ExpiresOn = entity.ExpiresOn }

            static member Deserialize(json: string) =
                try
                    JsonSerializer.Deserialize<CacheItem> json |> Ok
                with ex ->
                    Error $"Failed to deserialize cache item model: {ex.Message}"

            member ci.Serialize() = JsonSerializer.Serialize ci

            member ci.GetBytes() =
                try
                    ci.Base64Data |> Conversions.fromBase64 |> Ok
                with ex ->
                    Error $"Failed to convert from base64: {ex.Message}"


    [<RequireQualifiedAccess>]
    module Iterators =

        type Table =
            { Id: string
              Current: int
              ChunkSize: int
              Table: TableModel
              Query: string
              Parameters: obj list }

            member t.GetQueryAndParameters() =
                match t.IsStart() with
                | true -> $"{t.Query} LIMIT @{t.Parameters.Length}", t.Parameters @ [ t.ChunkSize ]
                | false ->
                    $"{t.Query} LIMIT @{t.Parameters.Length} OFFSET @{t.Parameters.Length + 1}",
                    t.Parameters @ [ t.ChunkSize; t.Current ]

            member t.Next() =
                { t with
                    Current = t.Current + t.ChunkSize }

            member t.IsStart() = t.Current = 0

            member t.Restart() = { t with Current = 0 }

        type IteratorSettings =
            { ChunkSize: int
              Table: TableModel
              Query: string
              Parameters: Value list }

        let iter
            (fn: TableRow list -> unit)
            (fetch: int -> IteratorSettings -> TableRow list)
            (settings: IteratorSettings)
            =
            let rec run (i: int) =
                let rows = fetch i settings

                match rows.IsEmpty with
                | true -> ()
                | false ->
                    fn rows
                    run (i + settings.ChunkSize)

            run 0

    [<RequireQualifiedAccess>]
    module IPC =

        let deserialize<'T> (data: byte array) =
            try
                data |> Encoding.UTF8.GetString |> JsonSerializer.Deserialize<'T> |> Ok
            with exn ->
                Error $"Failed to deserialize message: {exn.Message}"

        let serialize<'T> (value: 'T) = JsonSerializer.Serialize value

        let magicBytes = [ 14uy; 06uy ]

        type Header =
            { Length: int
              MessageTypeByte: byte
              MaskByte: byte }

            static member Create(length: int, messageTypeByte: byte, ?maskByte: byte) =
                { Length = length
                  MessageTypeByte = messageTypeByte
                  MaskByte = maskByte |> Option.defaultValue 0uy }

            static member TryDeserialize(bytes: byte array) =
                match bytes.Length >= 8 with
                | true ->
                    match [ bytes[0]; bytes[1] ] = magicBytes with
                    | true ->
                        { Length = BitConverter.ToInt32(bytes[4..])
                          MessageTypeByte = bytes[2]
                          MaskByte = bytes[3] }
                        |> Ok
                    | false -> Error "Magic bytes do not match."
                | false -> Error $"Header length ({bytes.Length}) is too short."

            member h.Serialize() =
                [| yield! magicBytes
                   h.MessageTypeByte
                   h.MaskByte
                   yield! BitConverter.GetBytes(h.Length) |]

        type Message =
            { Header: Header
              Body: byte array }

            static member Create(header: Header, body: byte array) = { Header = header; Body = body }

            static member Create(body: byte array, messageTypeByte: byte) =
                { Header = Header.Create(body.Length, messageTypeByte)
                  Body = body }

            static member Create(body: string, messageTypeByte: byte) =
                let b = Encoding.UTF8.GetBytes(body)

                Message.Create(b, messageTypeByte)

            static member CreateEmpty(messageTypeByte: byte) =
                { Header = Header.Create(0, messageTypeByte)
                  Body = [||] }


            member m.Serialize() =
                [| yield! m.Header.Serialize(); yield! m.Body |]

            member m.BodyAsUtf8String() = Encoding.UTF8.GetString m.Body

        [<RequireQualifiedAccess>]
        type RequestMessage =
            | RawMessage of Body: string
            | AddStateValue of Request: AddStateValueRequest
            | UpdateStateValue of Request: UpdateStateValueRequest
            | GetState
            | GetStateValue of Request: GetStateValueRequest
            | StateValueExists of RequestMessage: StateValueExistsRequest
            //| GetStateValueAsKey
            //| GetStateValueAsDateTime
            //| GetStateValueAsGuid
            //| GetStateValueAsBool
            | GetId
            | GetComputerName
            | GetUserName
            | GetBasePath
            | GetImportsPath
            | GetExportsPath
            | GetTmpPath
            | GetStorePath
            // ClearTmp?
            | AddDataSource of Request: AddDataSourceRequest
            | GetDataSource of Request: GetDataSourceRequest
            | AddArtifact of Request: AddArtifactRequest
            | GetArtifact of Request: GetArtifactRequest
            | GetArtifactBucket of Request: GetArtifactBucketRequest
            | AddResource of Request: AddResourceRequest
            | GetResource of Request: GetResourceRequest
            | AddCacheItem of Request: AddCacheItemRequest
            | GetCacheItem of Request: GetCacheItemRequest
            | DeleteCacheItem of Request: DeleteCacheItemRequest
            // Clear cache??
            | AddResult of Request: AddResultRequest
            | AddImportError of Request: AddImportErrorRequest
            | AddVariable of Request: AddVariableRequest
            | SubstituteValues of Request: SubstituteValueRequest
            | CreateTable of Request: CreateTableRequest
            | InsertRows of Request: InsertRowsRequest
            | SelectRows of Request: SelectRowsRequest
            | Log of Request: LogRequest
            | LogError of Request: LogErrorRequest
            | LogWarning of Request: LogWarningRequest
            //| IteratorNext
            //| IteratorBreak
            | Close

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> RequestMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> RequestMessage.RawMessage |> Ok
                | 2uy -> message.Body |> deserialize<AddStateValueRequest> |> Result.map AddStateValue
                | 3uy ->
                    message.Body
                    |> deserialize<UpdateStateValueRequest>
                    |> Result.map UpdateStateValue
                | 4uy -> GetState |> Ok
                | 5uy -> message.Body |> deserialize<GetStateValueRequest> |> Result.map GetStateValue
                | 6uy ->
                    message.Body
                    |> deserialize<StateValueExistsRequest>
                    |> Result.map StateValueExists
                | 7uy -> GetId |> Ok
                | 8uy -> GetComputerName |> Ok
                | 9uy -> GetUserName |> Ok
                | 10uy -> GetBasePath |> Ok
                | 11uy -> GetImportsPath |> Ok
                | 12uy -> GetExportsPath |> Ok
                | 13uy -> GetTmpPath |> Ok
                | 14uy -> GetStorePath |> Ok
                | 15uy -> message.Body |> deserialize<AddDataSourceRequest> |> Result.map AddDataSource
                | 16uy -> message.Body |> deserialize<GetDataSourceRequest> |> Result.map GetDataSource
                | 17uy -> message.Body |> deserialize<AddArtifactRequest> |> Result.map AddArtifact
                | 18uy -> message.Body |> deserialize<GetArtifactRequest> |> Result.map GetArtifact
                | 19uy ->
                    message.Body
                    |> deserialize<GetArtifactBucketRequest>
                    |> Result.map GetArtifactBucket
                | 20uy -> message.Body |> deserialize<AddResourceRequest> |> Result.map AddResource
                | 21uy -> message.Body |> deserialize<GetResourceRequest> |> Result.map GetResource
                | 22uy -> message.Body |> deserialize<AddCacheItemRequest> |> Result.map AddCacheItem
                | 23uy -> message.Body |> deserialize<GetCacheItemRequest> |> Result.map GetCacheItem
                | 24uy ->
                    message.Body
                    |> deserialize<DeleteCacheItemRequest>
                    |> Result.map DeleteCacheItem
                | 25uy -> message.Body |> deserialize<AddResultRequest> |> Result.map AddResult
                | 26uy -> message.Body |> deserialize<AddImportErrorRequest> |> Result.map AddImportError
                | 27uy -> message.Body |> deserialize<AddVariableRequest> |> Result.map AddVariable
                | 28uy ->
                    message.Body
                    |> deserialize<SubstituteValueRequest>
                    |> Result.map SubstituteValues
                | 29uy ->
                    message.Body
                    |> TableModel.TryDeserialize
                    |> Result.map (fst >> fun t -> ({ Table = t }: CreateTableRequest) |> CreateTable)
                | 30uy ->
                    message.Body
                    |> TableModel.TryDeserialize
                    |> Result.map (fst >> fun t -> ({ Table = t }: InsertRowsRequest) |> InsertRows)
                | 31uy ->
                    message.Body
                    |> TableModel.TryDeserialize
                    |> Result.bind (fun (table, data) ->
                        // Get query
                        match data.Length >= 4 with
                        | true ->
                            let qlb, tail = data |> Array.splitAt 4
                            let qLen = BitConverter.ToInt32 qlb

                            match tail.Length >= qLen with
                            | true ->
                                let qb, tail = tail |> Array.splitAt qLen

                                Ok(table, Encoding.UTF8.GetString qb, tail)
                            | false -> Error "Unable to deserialize query: data too short"
                        | false -> Error "Query data length is too short")
                    |> Result.bind (fun (table, query, data) ->
                        // Get the parameters.

                        match data.Length >= 4 with
                        | true ->
                            let plb, tail = data |> Array.splitAt 4
                            let pCount = BitConverter.ToInt32 plb

                            let rec handler (acc, i, d) =
                                match i < pCount with
                                | true ->
                                    match Value.TryDeserialize d with
                                    | Ok(v, tail) -> handler (acc @ [ v ], i + 1, tail)
                                    | Error e -> Error e
                                | false -> Ok acc

                            handler ([], 0, tail)
                            |> Result.map (fun pv ->
                                { Table = table
                                  QuerySql = query
                                  Parameters = pv }
                                |> SelectRows)
                        | false -> Error "Parameter data length is too short")
                | 32uy -> failwith "todo" // SelectBespokeRows -> failwith "todo"
                | 100uy -> message.Body |> deserialize<LogRequest> |> Result.map Log
                | 101uy -> message.Body |> deserialize<LogErrorRequest> |> Result.map LogError
                | 102uy -> message.Body |> deserialize<LogWarningRequest> |> Result.map LogWarning
                //| 254uy -> failwith "todo" // IteratorNext
                //| 255uy -> failwith "todo" // IteratorBreak
                | _ -> Error $"Unknown message type ({message})"

            member rm.GetMessageTypeByte() =
                match rm with
                | RawMessage _ -> 1uy
                | AddStateValue _ -> 2uy
                | UpdateStateValue _ -> 3uy
                | GetState -> 4uy
                | GetStateValue _ -> 5uy
                | StateValueExists _ -> 6uy
                | GetId -> 7uy
                | GetComputerName -> 8uy
                | GetUserName -> 9uy
                | GetBasePath -> 10uy
                | GetImportsPath -> 11uy
                | GetExportsPath -> 12uy
                | GetTmpPath -> 13uy
                | GetStorePath -> 14uy
                | AddDataSource _ -> 15uy
                | GetDataSource _ -> 16uy
                | AddArtifact _ -> 17uy
                // NOTE Internally this uses TryGetArtifact to handle errors with trying to add an artifact with the same name.
                | GetArtifact _ -> 18uy
                | GetArtifactBucket _ -> 19uy
                | AddResource _ -> 20uy
                | GetResource _ -> 21uy
                | AddCacheItem _ -> 22uy
                | GetCacheItem _ -> 23uy
                | DeleteCacheItem _ -> 24uy
                | AddResult _ -> 25uy
                | AddImportError _ -> 26uy
                | AddVariable _ -> 27uy
                | SubstituteValues _ -> 28uy
                | CreateTable _ -> 29uy
                | InsertRows _ -> 30uy
                | SelectRows _ -> 31uy
                | Log _ -> 100uy
                | LogError _ -> 101uy
                | LogWarning _ -> 102uy
                //| IteratorNext -> 254uy
                //| IteratorBreak -> 255uy
                | Close -> 0uy

            member rm.ToMessage() =
                let mbt = rm.GetMessageTypeByte()

                match rm with
                | RawMessage body -> Message.Create(body, mbt)
                | Close -> Message.CreateEmpty(mbt)
                | AddStateValue request -> Message.Create(serialize request, mbt)
                | UpdateStateValue request -> Message.Create(serialize request, mbt)
                | GetState -> Message.CreateEmpty(mbt)
                | GetStateValue request -> Message.Create(serialize request, mbt)
                | StateValueExists request -> Message.Create(serialize request, mbt)
                | GetId -> Message.CreateEmpty(mbt)
                | GetComputerName -> Message.CreateEmpty(mbt)
                | GetUserName -> Message.CreateEmpty(mbt)
                | GetBasePath -> Message.CreateEmpty(mbt)
                | GetImportsPath -> Message.CreateEmpty(mbt)
                | GetExportsPath -> Message.CreateEmpty(mbt)
                | GetTmpPath -> Message.CreateEmpty(mbt)
                | GetStorePath -> Message.CreateEmpty(mbt)
                | AddDataSource request -> Message.Create(serialize request, mbt)
                | GetDataSource request -> Message.Create(serialize request, mbt)
                | AddArtifact request -> Message.Create(serialize request, mbt)
                | GetArtifact request -> Message.Create(serialize request, mbt)
                | GetArtifactBucket request -> Message.Create(serialize request, mbt)
                | AddResource request -> Message.Create(serialize request, mbt)
                | GetResource request -> Message.Create(serialize request, mbt)
                | AddCacheItem request -> Message.Create(serialize request, mbt)
                | GetCacheItem request -> Message.Create(serialize request, mbt)
                | DeleteCacheItem request -> Message.Create(serialize request, mbt)
                | AddResult request -> Message.Create(serialize request, mbt)
                | AddImportError request -> Message.Create(serialize request, mbt)
                | AddVariable request -> Message.Create(serialize request, mbt)
                | SubstituteValues request -> Message.Create(serialize request, mbt)
                | CreateTable request -> Message.Create(request.Table.Serialize(), mbt)
                | InsertRows request -> Message.Create(request.Table.Serialize(), mbt)
                | SelectRows request ->

                    let queryBytes = request.QuerySql |> Encoding.UTF8.GetBytes

                    let body =
                        [| yield! request.Table.Serialize()
                           yield! queryBytes.Length |> BitConverter.GetBytes
                           yield! queryBytes
                           yield! request.Parameters.Length |> BitConverter.GetBytes
                           yield! request.Parameters |> List.map (fun p -> p.Serialize()) |> Array.concat |]

                    Message.Create(body, mbt)
                | Log request -> Message.Create(serialize request, mbt)
                | LogError request -> Message.Create(serialize request, mbt)
                | LogWarning request -> Message.Create(serialize request, mbt)
            //| IteratorNext -> failwith "todo"
            //| IteratorBreak -> failwith "todo"

            member rm.Serialize() = rm.ToMessage().Serialize()


        and [<CLIMutable>] AddStateValueRequest =
            { [<JsonPropertyName("key")>]
              Key: string
              [<JsonPropertyName("value")>]
              Value: string }

        and [<CLIMutable>] UpdateStateValueRequest =
            { [<JsonPropertyName("key")>]
              Key: string
              [<JsonPropertyName("value")>]
              Value: string }

        and [<CLIMutable>] GetStateValueRequest =
            { [<JsonPropertyName("key")>]
              Key: string }

        and [<CLIMutable>] StateValueExistsRequest =
            { [<JsonPropertyName("key")>]
              Key: string }

        and [<CLIMutable>] AddDataSourceRequest =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("dataSourceType")>]
              DataSourceType: string
              [<JsonPropertyName("uri")>]
              Uri: string
              [<JsonPropertyName("collectionName")>]
              CollectionName: string }

        and [<CLIMutable>] GetDataSourceRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] GetDataSourcesByCollectionRequest =
            { [<JsonPropertyName("collectionName")>]
              CollectionName: string }

        and [<CLIMutable>] AddArtifactRequest =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("bucket")>]
              Bucket: string
              [<JsonPropertyName("type")>]
              Type: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string }

        and [<CLIMutable>] GetArtifactRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] GetArtifactBucketRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] AddResourceRequest =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("type")>]
              Type: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string }

        and [<CLIMutable>] GetResourceRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] AddCacheItemRequest =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("base64Data")>]
              Base64Data: string
              [<JsonPropertyName("ttl")>]
              Ttl: int }

        and [<CLIMutable>] GetCacheItemRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] DeleteCacheItemRequest =
            { [<JsonPropertyName("name")>]
              Name: string }

        and [<CLIMutable>] AddResultRequest =
            { [<JsonPropertyName("step")>]
              Step: string
              [<JsonPropertyName("result")>]
              Result: string
              [<JsonPropertyName("startUtc")>]
              StartUtc: DateTime
              [<JsonPropertyName("endUtc")>]
              EndUtc: DateTime
              [<JsonPropertyName("serial")>]
              Serial: int64 }

        and [<CLIMutable>] AddImportErrorRequest =
            { [<JsonPropertyName("step")>]
              Step: string
              [<JsonPropertyName("error")>]
              Error: string
              [<JsonPropertyName("value")>]
              Value: string }

        and [<CLIMutable>] AddVariableRequest =
            { [<JsonPropertyName("name")>]
              Name: string
              [<JsonPropertyName("value")>]
              Value: string }

        and [<CLIMutable>] SubstituteValueRequest =
            { [<JsonPropertyName("value")>]
              Value: string }


        // Tables
        // NOTE this are not serialized as json.
        and CreateTableRequest = { Table: TableModel }

        and InsertRowsRequest = { Table: TableModel }

        and SelectRowsRequest =
            { Table: TableModel
              QuerySql: string
              Parameters: Value list }

        and [<CLIMutable>] LogRequest =
            { [<JsonPropertyName("step")>]
              Step: string
              [<JsonPropertyName("message")>]
              Message: string }

        and [<CLIMutable>] LogErrorRequest =
            { [<JsonPropertyName("step")>]
              Step: string
              [<JsonPropertyName("message")>]
              Message: string }

        and [<CLIMutable>] LogWarningRequest =
            { [<JsonPropertyName("step")>]
              Step: string
              [<JsonPropertyName("message")>]
              Message: string }

        [<RequireQualifiedAccess>]
        type ResponseMessage =
            | RawMessage of Body: string
            | Acknowledge
            | Value of Value: Value
            | String of Value: string
            | Bool of Value: bool
            | Rows of Rows: TableRow list
            | Close
            | Error of Message: string

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> ResponseMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> ResponseMessage.RawMessage |> Ok
                | 2uy -> ResponseMessage.Acknowledge |> Ok
                | 3uy -> Value.TryDeserialize message.Body |> Result.map (fst >> ResponseMessage.Value)
                | 4uy -> message.Body |> Encoding.UTF8.GetString |> ResponseMessage.String |> Ok
                // NOTE - need try/catch?
                | 5uy -> BitConverter.ToBoolean message.Body |> ResponseMessage.Bool |> Ok
                | 6uy ->
                    // Get count and create rows
                    match message.Body.Length >= 4 with
                    | true ->
                        let (lb, d) = message.Body |> Array.splitAt 4

                        let count = BitConverter.ToInt32 lb

                        let rec handle (acc, i, d) =
                            match i < count with
                            | true ->
                                match TableRow.TryDeserialize(d) with
                                | Ok(tr, tail) -> handle (acc @ [ tr ], i + 1, tail)
                                | Result.Error e -> Result.Error e
                            | false -> Result.Ok acc

                        handle ([], 0, d) |> Result.map ResponseMessage.Rows
                    | false -> Result.Error "Data length is too short"
                | 255uy -> message.Body |> Encoding.UTF8.GetString |> ResponseMessage.Error |> Ok
                | _ -> Result.Error $"Unknown message type ({message})"

            member rm.GetMessageTypeByte() =
                match rm with
                | Close -> 0uy
                | RawMessage _ -> 1uy
                | Acknowledge -> 2uy
                | Value _ -> 3uy
                | String _ -> 4uy
                | Bool _ -> 5uy
                | Rows _ -> 6uy
                | Error _ -> 255uy

            member rm.ToMessage() =
                let mbt = rm.GetMessageTypeByte()

                match rm with
                | RawMessage body -> Message.Create(body, mbt)
                | Close -> Message.CreateEmpty(mbt)
                | Acknowledge -> Message.CreateEmpty(mbt)
                | Value value -> Message.Create(value.Serialize(), mbt)
                | String value -> Message.Create(value, mbt)
                | Bool value -> Message.Create(BitConverter.GetBytes(value), mbt)
                | Rows rows ->
                    [| yield! rows.Length |> BitConverter.GetBytes
                       yield! rows |> List.map (fun r -> r.Serialize()) |> Array.concat |]
                    |> fun d -> Message.Create(d, mbt)
                | Error value -> Message.Create(value, mbt)

            member rm.Serialize() = rm.ToMessage().Serialize()
