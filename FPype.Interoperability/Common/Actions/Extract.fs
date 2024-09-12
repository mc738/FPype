namespace FPype.Interoperability.Common.Actions

module Extract =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type ParseCsvAction =
        { [<JsonPropertyName "dataSource">]
          DataSource: string
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
                        w.WriteString("source", this.DataSource)
                        this.Table.WriteToJsonProperty("table", w))
                )
