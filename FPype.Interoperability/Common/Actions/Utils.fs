namespace FPype.Interoperability.Common.Actions

open System.IO
open System.Text

module Utils =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type CreateDirectoryAction =
        { [<JsonPropertyName "path">]
          Path: string
          [<JsonPropertyName "name">]
          Name: string }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Utils.``create-directory``.name

            member this.ToSerializedActionParameters() =
                ({ Path = this.Path; Name = this.Name }: Utils.``create-directory``.Parameters)
                |> serializeAsJson

    type SetVariableAction =
        { [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "allowOverride">]
          AllowOverride: bool }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Utils.``set-variable``.name

            member this.ToSerializedActionParameters() =
                ({ Name = this.Name
                   AllowOverride = this.AllowOverride }
                : Utils.``set-variable``.Parameters)
                |> serializeAsJson


    type CreateSqliteDatabaseAction =
        { [<JsonPropertyName "path">]
          Path: string
          [<JsonPropertyName "variableName">]
          VariableName: string option
          [<JsonPropertyName "tables">]
          Tables: ActionTableVersion list }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = Utils.``create-sqlite-database``.name

            member this.ToSerializedActionParameters() =
                // This is handled specifically because of table models.
                // So instead it serialized the data specifically.
                writeJson (
                    Json.writeObject (fun w ->
                        w.WriteString("path", this.Path)
                        this.VariableName |> Option.iter (fun v -> w.WriteString("variable", v))
                        Json.writeArray (fun aw -> this.Tables |> List.iter (fun t -> t.WriteToJson aw)) "tables" w)
                )
