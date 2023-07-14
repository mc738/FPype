namespace FPype.Infrastructure.DataSinks

open FsToolbox.Core.Results



[<RequireQualifiedAccess>]
module Tables =


    open System.IO
    open Freql.Sqlite
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.ModelExtensions.Sqlite

    let dataSinkColumns =
        [ ({ Name = "ds__id"
             Type = BaseType.String
             ImportHandler = None })
          ({ Name = "ds__timestamp"
             Type = BaseType.DateTime
             ImportHandler = None }) ]

    let appendDataSinkColumns (table: TableModel) = table.AppendColumns dataSinkColumns

    let appendDataSinkData (row: TableRow) = ()

    let createTable (ctx: SqliteContext) (table: TableModel) = table.SqliteCreateTable ctx

    let initialize (id: string) (subscriptionId: string) (path: string) (schema: TableSchema) =
        let dir = Path.Combine(path, subscriptionId, id)

        Directory.CreateDirectory dir |> ignore
        let fullPath = Path.Combine(dir, $"{id}.db")
                
        match File.Exists fullPath with
        | true -> Ok()
        | false ->

            use ctx = SqliteContext.Create(fullPath)

            try
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

    let insertRow (row: TableRow) =


        ()
