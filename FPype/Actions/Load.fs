namespace FPype.Actions

open FPype.Data.Models
open FPype.Data.Store

module Load =

    module ``save-query-result-to-sqlite-database`` =

        open FPype.Connectors.Sqlite

        let name = "save_query_result_to_sqlite_database"

        type Parameters =
            { Path: string
              Table: TableModel
              Sql: string
              Parameters: obj list }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let fullPath = store.SubstituteValues parameters.Path

            store.BespokeSelectRows(parameters.Table, parameters.Sql, parameters.Parameters)
            |> insert fullPath
            |> Result.map (fun rs ->
                store.Log(stepName, name, $"{rs.Length} rows inserted into {fullPath}.")

                store)
