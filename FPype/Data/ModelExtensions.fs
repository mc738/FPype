namespace FPype.Data

/// <summary>
/// A collection of specific extensions for models such as working with Sqlite or MySql databases etc.
/// </summary>
module ModelExtensions =

    open FPype.Core.Types
    open FPype.Data.Models

    module Sqlite =

        open Microsoft.Data.Sqlite
        open Freql.Sqlite

        [<RequireQualifiedAccess>]
        module private Internal =

            let rec typeName bt notNull =
                let nn = if notNull then " NOT NULL" else ""

                match bt with
                | BaseType.Boolean -> "INTEGER", nn
                | BaseType.Byte -> "INTEGER", nn
                | BaseType.Char -> "TEXT", nn
                | BaseType.Decimal -> "INTEGER", nn
                | BaseType.Double -> "INTEGER", nn
                | BaseType.Float -> "INTEGER", nn
                | BaseType.Int -> "INTEGER", nn
                | BaseType.Short -> "INTEGER", nn
                | BaseType.Long -> "INTEGER", nn
                | BaseType.String -> "TEXT", nn
                | BaseType.DateTime -> "TEXT", nn
                | BaseType.Guid -> "TEXT", nn
                | BaseType.Option t -> typeName t false


            let createTable (ctx: SqliteContext) (tableName: string) (columns: TableColumn list) =
                let columnText =
                    columns
                    |> List.map (fun c ->
                        let tn = typeName c.Type true |> fun (a, b) -> $"{a}{b}"

                        $"{c.Name} {tn}")
                    |> String.concat ","

                String.concat "" [ $"CREATE TABLE IF NOT EXISTS {tableName} ("; columnText; ")" ]
                |> ctx.ExecuteSqlNonQuery
                |> fun _ -> columns

            let insert (ctx: SqliteContext) (model: TableModel) =
                let (columns, parameters) =
                    model.Columns
                    |> List.mapi (fun i c -> $"{c.Name}", $"@{i}")
                    |> List.fold (fun (accC, accP) (c, p) -> accC @ [ c ], accP @ [ p ]) ([], [])
                    |> fun (c, p) -> String.concat "," c, String.concat "," p

                let sql =
                    String.concat "" [ $"INSERT INTO {model.Name} ("; columns; ")"; " VALUES ("; parameters; ")" ]

                ctx.ExecuteInTransaction(fun t ->
                    model.Rows |> List.map (fun r -> t.ExecuteVerbatimNonQueryAnon(sql, r.Box())))

            let mapper (columns: TableColumn list) (reader: SqliteDataReader) =
                let rec handler t i =
                    match t with
                    | BaseType.Boolean -> reader.GetBoolean i |> Value.Boolean
                    | BaseType.Byte -> reader.GetByte i |> Value.Byte
                    | BaseType.Char -> reader.GetChar i |> Value.Char
                    | BaseType.Decimal -> reader.GetDecimal i |> Value.Decimal
                    | BaseType.Double -> reader.GetDouble i |> Value.Double
                    | BaseType.Float -> reader.GetFloat i |> Value.Float
                    | BaseType.Int -> reader.GetInt32 i |> Value.Int
                    | BaseType.Short -> reader.GetInt16 i |> Value.Short
                    | BaseType.Long -> reader.GetInt64 i |> Value.Long
                    | BaseType.String -> reader.GetString i |> Value.String
                    | BaseType.DateTime -> reader.GetDateTime i |> Value.DateTime
                    | BaseType.Guid -> reader.GetGuid i |> Value.Guid
                    | BaseType.Option it ->
                        match reader.IsDBNull i with
                        | true -> None |> Value.Option
                        | false -> handler it i |> Some |> Value.Option

                [ while reader.Read() do
                      columns
                      |> List.map (fun c ->
                          let i = reader.GetOrdinal c.Name
                          handler c.Type i)
                      |> TableRow.FromValues ]

            let select (ctx: SqliteContext) (model: TableModel) =
                let names = model.Columns |> List.map (fun n -> n.Name) |> String.concat ","

                let sql = [ "SELECT"; names; "FROM"; model.Name ] |> String.concat " "

                ctx.Bespoke<TableRow>(sql, [], mapper model.Columns)

            let conditionalSelect (ctx: SqliteContext) (model: TableModel) (conditions: string) (parameters: obj list) =
                let names = model.Columns |> List.map (fun n -> n.Name) |> String.concat ","

                let sql = [ "SELECT"; names; "FROM"; model.Name; conditions ] |> String.concat " "

                ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)

            let bespokeSelect (ctx: SqliteContext) (model: TableModel) (sql: string) (parameters: obj list) =
                ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)

        type TableModel with

            member tm.SqliteCreateTable(ctx: SqliteContext) =
                Internal.createTable ctx tm.Name tm.Columns

            member tm.SqliteInsert(ctx: SqliteContext, ?createTable: bool) =
                match createTable |> Option.defaultValue true with
                | true -> tm.SqliteCreateTable ctx |> ignore
                | false -> ()

                Internal.insert ctx tm

            member tm.SqliteSelect(ctx: SqliteContext) = Internal.select ctx tm

            member tm.SqliteConditionalSelect(ctx: SqliteContext, conditions: string, parameters: obj list) =
                Internal.conditionalSelect ctx tm conditions parameters

            member tm.SqliteBespokeSelect(ctx: SqliteContext, sql: string, parameters: obj list) =
                Internal.bespokeSelect ctx tm sql parameters
