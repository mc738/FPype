namespace FPype.Infrastructure.DataSinks

open System
open FPype.Configuration
open FPype.Data.Store
open FsToolbox.Core.Results



[<RequireQualifiedAccess>]
module Tables =


    open System.IO
    open Freql.Sqlite
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.ModelExtensions.Sqlite

    [<AutoOpen>]
    module private Internal =
        
        let metadataTableSql = """

        """
        
        let dataSinkColumns =
            [ ({ Name = "ds__id"
                 Type = BaseType.String
                 ImportHandler = None })
              ({ Name = "ds__timestamp"
                 Type = BaseType.DateTime
                 ImportHandler = None }) ]

        let tableFromSchema (schema: TableSchema) = TableModel.FromSchema schema

        let appendRows (rows: TableRow list) (table: TableModel) = table.AppendRows rows

        let appendRow (row: TableRow) (table: TableModel) = table.AppendRows([ row ])

        let appendDataSinkColumns (table: TableModel) = table.AppendColumns dataSinkColumns

        let appendDataSinkData (idType: IdType option) (row: TableRow) =
            [ Value.String(idType |> Option.defaultValue IdType.Generated |> (fun id -> id.Get()))
              Value.DateTime DateTime.UtcNow ]
            |> row.AppendValues

        let createTable (ctx: SqliteContext) (table: TableModel) = table.SqliteCreateTable ctx

    let initialize (id: string) (subscriptionId: string) (path: string) (schema: TableSchema) =
        try
            let dir = Path.Combine(path, subscriptionId, id)

            Directory.CreateDirectory dir |> ignore
            let fullPath = Path.Combine(dir, $"{id}.db")

            match File.Exists fullPath with
            | true -> Ok()
            | false ->

                use ctx = SqliteContext.Create(fullPath)

                TableModel.FromSchema schema
                |> appendDataSinkColumns
                |> createTable ctx
                |> ignore
                |> Ok
        with exn ->
            ({ Message = $"Error creating `({schema.Name})` table: {exn.Message}"
               DisplayMessage = $"Error creating `({schema.Name})` table"
               Exception = Some exn }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult

    let insertRow (ctx: SqliteContext) (idType: IdType option) (tableSchema: TableSchema) (row: TableRow) =

        try
            let table =
                tableFromSchema tableSchema |> appendRow (row |> appendDataSinkData idType)

            match table.SqliteInsert(ctx) with
            | Ok r -> Ok($"Successfully inserted {r.Length} row(s) into table {table.Name}")
            | Error e ->
                ({ Message = $"Failed to insert rows: {e}"
                   DisplayMessage = "Failed to insert rows"
                   Exception = None }
                : FailureResult)
                |> Error
        with exn ->
            ({ Message = $"Unhandled exception while inserting insert rows: {exn.Message}"
               DisplayMessage = "Failed to insert rows"
               Exception = Some exn }
            : FailureResult)
            |> Error

        |> ActionResult.fromResult


    let selectRows (ctx: SqliteContext) (tableSchema: TableSchema) =
        ()