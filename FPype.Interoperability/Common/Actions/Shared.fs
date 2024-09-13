namespace FPype.Interoperability.Common.Actions

open System

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

    type IDateGroups =

        abstract member GroupType: string

        abstract member WriteToJsonValue: Writer: Utf8JsonWriter -> unit

        abstract member WriteToJsonProperty: PropertyName: string * Writer: Utf8JsonWriter -> unit

    type MonthDateGroup =
        { [<JsonPropertyName "fieldName">]
          FieldName: string
          [<JsonPropertyName "label">]
          Label: string
          [<JsonPropertyName "start">]
          Start: DateTime
          [<JsonPropertyName "length">]
          Length: int }

        interface IDateGroups with

            [<JsonPropertyName "groupType">]
            member this.GroupType = nameof this

            member this.WriteToJsonProperty(propertyName, writer) =
                Json.writePropertyObject
                    (fun w ->
                        w.WriteString("type", "months")

                        w.WriteString("fieldName", this.FieldName)
                        w.WriteString("label", this.Label)
                        w.WriteString("start", this.Start)
                        w.WriteNumber("length", this.Length))
                    propertyName
                    writer

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "months")

                        w.WriteString("fieldName", this.FieldName)
                        w.WriteString("label", this.Label)
                        w.WriteString("start", this.Start)
                        w.WriteNumber("length", this.Length))
                    writer

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

    type ActionQueryVersion =
        { [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "version">]
          Version: int option }

        member aqv.WriteToJsonValue(writer: Utf8JsonWriter) =
            Json.writeObject
                (fun w ->
                    w.WriteString("name", aqv.Name)
                    aqv.Version |> Option.iter (fun v -> w.WriteNumber("version", v)))
                writer

        member aqv.WriteToJsonProperty(propertyName: string, writer: Utf8JsonWriter) =
            Json.writePropertyObject
                (fun w ->
                    w.WriteString("name", aqv.Name)
                    aqv.Version |> Option.iter (fun v -> w.WriteNumber("version", v)))
                propertyName
                writer


    let serializeAsJson<'T> (value: 'T) = JsonSerializer.Serialize value

    let writeJson (fn: Utf8JsonWriter -> unit) =
        use ms = new MemoryStream()
        use writer = new Utf8JsonWriter(ms)

        fn writer

        writer.Flush()
        ms.ToArray() |> Encoding.UTF8.GetString
