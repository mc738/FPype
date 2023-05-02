namespace FPype.Connectors

open FPype.Data.Models


module Sqlite =

    open Freql.Sqlite
    open FPype.Data.Models
    open FPype.Data.ModelExtensions.Sqlite

    let createTable (path: string) (table: TableModel) =
        use ctx = SqliteContext.Open(path)

        table.SqliteCreateTable ctx

    let createAndInitialize (path: string) (tables: TableModel list) =
        try
            use ctx = SqliteContext.Create path

            ctx.ExecuteInTransactionV2(fun t ->
                tables
                |> List.map (fun tm ->
                    tm.SqliteCreateTable t
                    |> fun c ->
                        { Name = tm.Name
                          Columns = c
                          Rows = tm.Rows })
                |> Ok)
        with ex ->
            Error $"Error creating Sqlite database: {ex.Message}"




    let select (path: string) (table: TableModel) =
        use ctx = SqliteContext.Open(path)

        table.SqliteSelect(ctx)

    let selectConditional (path: string) (table: TableModel) (conditions: string) (parameters: obj list) =
        use ctx = SqliteContext.Open(path)

        table.SqliteConditionalSelect(ctx, conditions, parameters)

    let selectBespoke (path: string) (table: TableModel) (sql: string) (parameters: obj list) =
        use ctx = SqliteContext.Open(path)

        table.SqliteBespokeSelect(ctx, sql, parameters)

    let insert (path: string) (table: TableModel) =
        use ctx = SqliteContext.Open(path)

        table.SqliteInsert(ctx)
