namespace FPype.Configuration

open FPype.Core.Types
open FPype.Data.Models

module Tables =

    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FPype.Configuration.Persistence
    
    let createImportHandler (json: JsonElement) =
        match Json.tryGetStringProperty "handler" json with
        | Some "parse_date" -> Json.tryGetStringProperty "format" json |> Option.map ImportHandlers.parseDate
        | _ -> None

    let createColumns (ctx: SqliteContext) (tableName: string) =
        try
            Operations.selectTableColumnRecords ctx [ "WHERE table_name = @0" ] [ tableName ]
            |> List.sortBy (fun tcr -> tcr.ColumnIndex)
            |> List.map (fun tcr ->
                let importHandler =
                    tcr.ImportHandler
                    |> Option.map (blobToString >> toJson)
                    |> Option.bind createImportHandler

                ({ Name = tcr.Name
                   Type =
                     BaseType.FromId(tcr.DataType, tcr.Optional)
                     |> Option.defaultWith (fun _ -> failwith $"Invalid type for column `{tcr.Name}`: `{tcr.DataType}`")
                   ImportHandler = importHandler }: TableColumn))
            |> Ok
        with exn ->
            Error $"Error creating table columns: {exn}"

    let getTable (ctx: SqliteContext) (tableName: string) =
        Operations.selectTableModelRecord ctx [ "WHERE name = @0" ] [ tableName ]

    let tryCreateTableModel (ctx: SqliteContext) (tableName: string) =
        getTable ctx tableName
        |> Option.map (fun t ->
            createColumns ctx t.Name
            |> Result.map (fun tc ->
                ({ Name = t.Name
                   Columns = tc
                   Rows = [] }: TableModel)))
        |> Option.defaultValue (Error $"Table `{tableName}` not found")

    let loadTableFromJson (ctx: SqliteContext) (propertyName: string) (json: JsonElement) =
        Json.tryGetStringProperty propertyName json
        |> Option.map (tryCreateTableModel ctx)
        |> Option.defaultValue (Error $"`{propertyName}` property missing")
