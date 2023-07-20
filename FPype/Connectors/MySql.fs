namespace FPype.Connectors

open Freql.MySql

module MySql =

    open FPype.Core.Types
    open FPype.Data.Models
    open MySql.Data.MySqlClient
    open FPype.Data.ModelExtensions.MySql

    let selectConditional (connectionString: string) (table: TableModel) (conditions: string) (parameters: obj list) =
        use ctx = MySqlContext.Connect(connectionString)

        table.MySqlConditionalSelect(ctx, conditions, parameters)

    let selectBespoke (connectionString: string) (table: TableModel) (sql: string) (parameters: obj list) =
        use ctx = MySqlContext.Connect(connectionString)

        table.MySqlBespokeSelect(ctx, sql, parameters)

    let insert (connectionString: string) (table: TableModel) =
        use ctx = MySqlContext.Connect(connectionString)


        table.MySqlInsert(ctx)