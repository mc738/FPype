namespace FPype.Interoperability.Common.Actions

module Transform =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type ExecuteQueryAction =
        { [<JsonPropertyName "query">]
          Query: ActionQueryVersion
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Transform.``execute-query``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.Table.WriteToJsonProperty("query", w)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type AggregateAction =
        { [<JsonPropertyName "query">]
          Query: ActionQueryVersion
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Transform.``execute-query``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.Table.WriteToJsonProperty("query", w)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type AggregateByDateAndCategoryAction =
        { [<JsonPropertyName "dateGroups">]
          DateGroups: IDateGroups
          [<JsonPropertyName "categoryField">]
          CategoryField: string
          [<JsonPropertyName "query">]
          Query: ActionQueryVersion
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() =
                Transform.``aggregate-by-date-and-category``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.DateGroups.WriteToJsonProperty("dateGroups", w)
                        w.WriteString("categoryField", this.CategoryField)
                        this.Table.WriteToJsonProperty("query", w)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type AggregateByDateAction =
        { [<JsonPropertyName "dateGroups">]
          DateGroups: IDateGroups
          [<JsonPropertyName "query">]
          Query: ActionQueryVersion
          [<JsonPropertyName "table">]
          Table: ActionTableVersion }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Transform.``aggregate-by-date``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.DateGroups.WriteToJsonProperty("dateGroups", w)
                        this.Table.WriteToJsonProperty("query", w)
                        this.Table.WriteToJsonProperty("table", w))
                )

    type MapToObjectAction =
        { [<JsonPropertyName "mapper">]
          Mapper: string
          [<JsonPropertyName "version">]
          Version: int option }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this


            member this.GetActionName() = Transform.``map-to-object``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("mapper", this.Mapper)
                        this.Version |> Option.iter (fun v -> w.WriteNumber("version", 0)))
                )
