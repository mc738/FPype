namespace FPype.Interoperability.Common.Actions

[<AutoOpen>]
module Shared =

    open System.IO
    open System.Text
    open System.Text.Json
    open System.Text.Json.Serialization
    open FsToolbox.Core

    type IPipelineAction =

        abstract member ActionType: string

        abstract member ToSerializedActionParameters: unit -> string

        abstract member GetActionName: unit -> string

    type StringKeyValue =
        { [<JsonPropertyName "key">]
          Key: string
          [<JsonPropertyName "value">]
          Value: string }

    type ActionTableVersion =
        { [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "version">]
          Version: int option }

        member atv.WriteToJsonValue(writer: Utf8JsonWriter) =
            Json.writeObject
                (fun w ->
                    w.WriteString("name", atv.Name)
                    atv.Version |> Option.iter (fun v -> w.WriteNumber("version", v)))
                writer

        member atv.WriteToJsonProperty(propertyName: string, writer: Utf8JsonWriter) =
            Json.writePropertyObject
                (fun w ->
                    w.WriteString("name", atv.Name)
                    atv.Version |> Option.iter (fun v -> w.WriteNumber("version", v)))
                propertyName
                writer


    let serializeAsJson<'T> (value: 'T) = JsonSerializer.Serialize value

    let writeJson (fn: Utf8JsonWriter -> unit) =
        use ms = new MemoryStream()
        use writer = new Utf8JsonWriter(ms)

        fn writer

        writer.Flush()
        ms.ToArray() |> Encoding.UTF8.GetString
