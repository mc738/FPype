namespace FPype.Scripting

open System
open System.IO.Pipes
open System.Text
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
            | AddStateValue
            | UpdateStateValue
            | GetState
            | GetStateValue
            | StateValueExists
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
            | AddDataSource
            | GetDataSource
            | AddArtifact
            | GetArtifact
            | GetArtifactBucket
            | AddResource
            | GetResource
            | AddCacheItem
            | GetCacheItem
            | DeleteCacheItem
            // Clear cache??
            | AddResult
            | AddImportError
            | AddVariable
            | SubstituteValues
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
                | 2uy -> failwith "todo" //AddStateValue
                | 3uy -> failwith "todo" // UpdateStateValue -> failwith "todo"
                | 4uy -> failwith "todo" // GetState -> failwith "todo"
                | 5uy -> failwith "todo" // GetStateValue -> failwith "todo"
                | 6uy -> failwith "todo" //StateValueExists -> failwith "todo"
                | 7uy -> failwith "todo" // GetId -> failwith "todo"
                | 8uy -> failwith "todo" // GetComputerName -> failwith "todo"
                | 9uy -> failwith "todo" // GetUserName -> failwith "todo"
                | 10uy -> failwith "todo" // GetBasePath -> failwith "todo"
                | 11uy -> failwith "todo" // GetImportsPath -> failwith "todo"
                | 12uy -> failwith "todo" //GetExportsPath -> failwith "todo"
                | 13uy -> failwith "todo" // GetTmpPath -> failwith "todo"
                | 14uy -> failwith "todo" // GetStorePath -> failwith "todo"
                | 15uy -> failwith "todo" //AddDataSource -> failwith "todo"
                | 16uy -> failwith "todo" //GetDataSource -> failwith "todo"
                | 17uy -> failwith "todo" //AddArtifact -> failwith "todo"
                | 18uy -> failwith "todo" //GetArtifact -> failwith "todo"
                | 19uy -> failwith "todo" //GetArtifactBucket -> failwith "todo"
                | 20uy -> failwith "todo" //AddResource -> failwith "todo"
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
                | AddStateValue -> 2uy
                | UpdateStateValue -> 3uy
                | GetState -> 4uy
                | GetStateValue -> 5uy
                | StateValueExists -> 6uy
                | GetId -> 7uy
                | GetComputerName -> 8uy
                | GetUserName -> 9uy
                | GetBasePath -> 10uy
                | GetImportsPath -> 11uy
                | GetExportsPath -> 12uy
                | GetTmpPath -> 13uy
                | GetStorePath -> 14uy
                | AddDataSource -> 15uy
                | GetDataSource -> 16uy
                | AddArtifact -> 17uy
                | GetArtifact -> 18uy
                | GetArtifactBucket -> 19uy
                | AddResource -> 20uy
                | GetResource -> 21uy
                | AddCacheItem -> 22uy
                | GetCacheItem -> 23uy
                | DeleteCacheItem -> 24uy
                | AddResult -> 25uy
                | AddImportError -> 26uy
                | AddVariable -> 27uy
                | SubstituteValues -> 28uy
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
                match rm with
                | RawMessage body -> Message.Create(body, rm.GetMessageTypeByte())
                | Close -> Message.CreateEmpty(rm.GetMessageTypeByte())
                | AddStateValue -> failwith "todo"
                | UpdateStateValue -> failwith "todo"
                | GetState -> failwith "todo"
                | GetStateValue -> failwith "todo"
                | StateValueExists -> failwith "todo"
                | GetId -> failwith "todo"
                | GetComputerName -> failwith "todo"
                | GetUserName -> failwith "todo"
                | GetBasePath -> failwith "todo"
                | GetImportsPath -> failwith "todo"
                | GetExportsPath -> failwith "todo"
                | GetTmpPath -> failwith "todo"
                | GetStorePath -> failwith "todo"
                | AddDataSource -> failwith "todo"
                | GetDataSource -> failwith "todo"
                | AddArtifact -> failwith "todo"
                | GetArtifact -> failwith "todo"
                | GetArtifactBucket -> failwith "todo"
                | AddResource -> failwith "todo"
                | GetResource -> failwith "todo"
                | AddCacheItem -> failwith "todo"
                | GetCacheItem -> failwith "todo"
                | DeleteCacheItem -> failwith "todo"
                | AddResult -> failwith "todo"
                | AddImportError -> failwith "todo"
                | AddVariable -> failwith "todo"
                | SubstituteValues -> failwith "todo"
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

        and [<CLIMutable>] GetDataSourcesByCollection =
            { [<JsonPropertyName("collectionName")>]
              CollectionName: string }

        
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
