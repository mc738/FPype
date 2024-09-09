namespace FPype.Actions

open FPype.Data.Models
open FPype.Data.Store

module Utils =

    open System.IO

    module ``create-directory`` =

        let name = "create_directory"

        type Parameters = { Path: string; Name: string }

        let run (parameters: Parameters) (store: PipelineStore) =
            let fullPath = store.SubstituteValues parameters.Path

            try
                match Directory.Exists fullPath with
                | true -> ()
                | false -> Directory.CreateDirectory fullPath |> ignore

                store.AddVariable(parameters.Name, fullPath)

                Ok store
            with ex ->
                Error $"Failed to create directory `{fullPath}` - {ex.Message}"

        let createAction stepName parameters =
            run parameters |> createAction name stepName

    module ``set-variable`` =

        let name = "set_variable"

        type Parameters = { Name: string; AllowOverride: bool }

        type VariableType =
            | Literal of string
            | CurrentTimestamp
            | Id

    module ``create-sqlite-database`` =

        open FPype.Connectors.Sqlite

        let name = "create_sqlite_database"

        type Parameters =
            { Path: string
              VariableName: string option
              Tables: TableModel list }

        let run (parameters: Parameters) (store: PipelineStore) =
            let fullPath = store.SubstituteValues parameters.Path

            createAndInitialize fullPath parameters.Tables
            |> Result.map (fun _ ->
                parameters.VariableName
                |> Option.iter (fun vn -> store.AddVariable(vn, fullPath))

                store)

        let createAction stepName parameters =
            run parameters |> createAction name stepName
