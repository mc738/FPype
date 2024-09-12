namespace FPype.Interoperability.Common.Actions

module Extract =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type ParseCsvAction =
        { [<JsonPropertyName "source">]
          Source: string
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Extract.``parse-csv``.name

            member this.ToSerializedActionParameters() =
                // This is handled specifically because of table models.
                // So instead it serialized the data specifically.
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("source", this.Source)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type ParseCsvCollectionAction =
        { [<JsonPropertyName "collection">]
          Collection: string
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Extract.``parse-csv-collection``.name

            member this.ToSerializedActionParameters() =
                // This is handled specifically because of table models.
                // So instead it serialized the data specifically.
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("collection", this.Collection)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type GrokAction =
        { [<JsonPropertyName "source">]
          Source: string
          Table: ActionTableVersion
          GrokString: string
          ExtraPatterns: GrokExtraPattern list }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Extract.grok.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("source", this.Source)
                        this.Table.WriteToJsonProperty("table", w)
                        w.WriteString("grokString", this.GrokString)

                        Json.writeArray
                            (fun aw -> this.ExtraPatterns |> List.iter (fun ep -> ep.WriteToJsonValue(aw)))
                            "extraPatterns"
                            w)
                )

    and GrokExtraPattern =
        { [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "pattern">]
          Pattern: string }

        member gep.WriteToJsonValue(writer: Utf8JsonWriter) =
            Json.writeObject
                (fun w ->
                    w.WriteString("name", gep.Name)
                    w.WriteString("pattern", gep.Pattern))
                writer

    type ExtractFromXlsxAction =
        { [<JsonPropertyName "source">]
          DataSource: string
          [<JsonPropertyName "table">]
          Table: ActionTableVersion
          [<JsonPropertyName "worksheetName">]
          WorksheetName: string
          [<JsonPropertyName "startRowIndex">]
          StartRowIndex: int option
          [<JsonPropertyName "endRowIndex">]
          EndRowIndex: int option
          [<JsonPropertyName "columnMap">]
          ColumnMap: XlsxMappedColumn list }


        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Extract.``extract-from-xlsx``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("source", this.DataSource)
                        this.Table.WriteToJsonProperty("table", w)
                        w.WriteString("worksheetName", this.WorksheetName)
                        this.StartRowIndex |> Option.iter (fun v -> w.WriteNumber("startRowIndex", v))
                        this.EndRowIndex |> Option.iter (fun v -> w.WriteNumber("endRowIndex", v))

                        Json.writeArray
                            (fun aw -> this.ColumnMap |> List.iter (fun cm -> cm.WriteToJsonValue aw))
                            "columnMap"
                            w)
                )

    and XlsxMappedColumn =
        { [<JsonPropertyName "tableColumnN">]
          TableColumn: string
          [<JsonPropertyName "columnReference">]
          ColumnReference: string }

        member xmc.WriteToJsonValue(writer: Utf8JsonWriter) =
            Json.writeObject
                (fun w ->
                    w.WriteString("tableColumn", xmc.TableColumn)
                    w.WriteString("columnReference", xmc.ColumnReference))
                writer
