﻿namespace FPype.Scripting

open System
open System.IO.Pipes
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Channels
open FPype.Data.Store

module Core =

    // Notes
    //
    // Need to handle 2 types of response (?):
    // Fixed length (with a length value)
    // Streams (?) (with length value -1 - send data until 0uy reached)

    [<RequireQualifiedAccess>]
    module IPC =

        open System.IO
        open System.IO.Pipes

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

            static member Create(body: string, messageTypeByte: byte) =
                let b = Encoding.UTF8.GetBytes(body)

                { Header = Header.Create(b.Length, messageTypeByte)
                  Body = b }

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
            | CreateTable
            | InsertRows
            | SelectRows
            | SelectBespokeRows
            | Log
            | LogError
            | LogWarning
            | IteratorNext
            | IteratorBreak
            | Close

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> RequestMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> RequestMessage.RawMessage |> Ok
                | 2uy -> message.Body |> deserialize<AddStateValueRequest> |> Result.map AddStateValue
                | 3uy -> message.Body |> deserialize<UpdateStateValueRequest> |> Result.map UpdateStateValue
                | 4uy -> GetState |> Ok
                | 5uy -> message.Body |> deserialize<GetStateValueRequest> |> Result.map GetStateValue 
                | 6uy -> message.Body |> deserialize<StateValueExistsRequest> |> Result.map StateValueExists
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
                | 19uy -> message.Body |> deserialize<GetArtifactBucketRequest> |> Result.map GetArtifactBucket
                | 20uy -> message.Body |> deserialize<AddResourceRequest> |> Result.map AddResource
                | 21uy -> failwith "todo" //GetResource -> failwith "todo"
                | 22uy -> failwith "todo" //AddCacheItem -> failwith "todo"
                | 23uy -> failwith "todo" // GetCacheItem -> failwith "todo"
                | 24uy -> failwith "todo" // DeleteCacheItem -> failwith "todo"
                | 25uy -> failwith "todo" // AddResult -> failwith "todo"
                | 26uy -> failwith "todo" // AddImportError -> failwith "todo"
                | 27uy -> failwith "todo" // AddVariable -> failwith "todo"
                | 28uy -> failwith "todo" // SubstituteValues -> failwith "todo"
                | 29uy -> failwith "todo" // CreateTable -> failwith "todo"
                | 30uy -> failwith "todo" // InsertRows -> failwith "todo"
                | 31uy -> failwith "todo" // SelectRows -> failwith "todo"
                | 32uy -> failwith "todo" // SelectBespokeRows -> failwith "todo"
                | 33uy -> failwith "todo" // Log -> failwith "todo"
                | 34uy -> failwith "todo" // LogError -> failwith "todo"
                | 35uy -> failwith "todo" // LogWarning -> failwith "todo"
                | 254uy -> failwith "todo" // IteratorNext
                | 255uy -> failwith "todo" // IteratorBreak
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
                | CreateTable -> 29uy
                | InsertRows -> 30uy
                | SelectRows -> 31uy
                | SelectBespokeRows -> 32uy
                | Log -> 33uy
                | LogError -> 34uy
                | LogWarning -> 35uy
                | IteratorNext -> 254uy
                | IteratorBreak -> 255uy
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
                | CreateTable -> failwith "todo"
                | InsertRows -> failwith "todo"
                | SelectRows -> failwith "todo"
                | SelectBespokeRows -> failwith "todo"
                | Log -> failwith "todo"
                | LogError -> failwith "todo"
                | LogWarning -> failwith "todo"
                | IteratorNext -> failwith "todo"
                | IteratorBreak -> failwith "todo"

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


        [<RequireQualifiedAccess>]
        type ResponseMessage =
            | RawMessage of Body: string
            | Acknowledge
            | Value
            | String

            | Close

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> ResponseMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> ResponseMessage.RawMessage |> Ok
                | _ -> Error $"Unknown message type ({message})"

            member rm.GetMessageTypeByte() =
                match rm with
                | RawMessage _ -> 1uy
                | Close -> 0uy

            member rm.ToMessage() =
                match rm with
                | RawMessage body -> Message.Create(body, rm.GetMessageTypeByte())
                | Close -> Message.CreateEmpty(rm.GetMessageTypeByte())

            member rm.Serialize() = rm.ToMessage().Serialize()
