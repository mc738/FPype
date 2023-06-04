namespace FPype.Configuration

open System.IO
open System.Text.Json
open Freql.Core.Common.Types


[<RequireQualifiedAccess>]
module Tables =

    open System.Text.Json
    open Freql.Sqlite
    open FsToolbox.Core
    open FsToolbox.Extensions
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Configuration.Persistence

    type NewTableVersion =
        { Id: IdType
          Name: string
          Version: ItemVersion
          Columns: NewColumn list }

    and NewColumn =
        { Id: IdType
          Name: string
          DataType: string
          Optional: bool
          ImportHandler: string option }

        static member Deserialize(json: JsonElement) =
            match
                Json.tryGetStringProperty "name" json,
                Json.tryGetStringProperty "dataType" json,
                Json.tryGetBoolProperty "optional" json
            with
            | Some n, Some dt, Some o ->
                { Id = IdType.FromJson json
                  Name = n
                  DataType = dt
                  Optional = o
                  ImportHandler =
                    Json.tryGetProperty "importHandler" json
                    |> Option.map (fun r -> (*TODO check!*) r.ToString()) }
                |> Ok
            | None, _, _ -> Error "Missing `name` property"
            | _, None, _ -> Error "Missing `dataType` property"
            | _, _, None -> Error "Missing `optional` property "

    let createImportHandler (json: JsonElement) =
        match Json.tryGetStringProperty "handler" json with
        | Some "parse_date" -> Json.tryGetStringProperty "format" json |> Option.map ImportHandlers.parseDate
        | _ -> None

    let createColumns (ctx: SqliteContext) (versionId: string) =
        try
            Operations.selectTableColumnRecords ctx [ "WHERE table_version_id = @0" ] [ versionId ]
            |> List.sortBy (fun tcr -> tcr.ColumnIndex)
            |> List.map (fun tcr ->
                let importHandler =
                    tcr.ImportHandler
                    |> Option.map (blobToString >> toJson)
                    |> Option.bind createImportHandler

                ({ Name = tcr.Name
                   Type =
                     BaseType.FromId(tcr.DataType, tcr.Optional)
                     |> Option.defaultWith (fun _ ->
                         failwith $"Invalid type for column `{tcr.Name}`: `{tcr.DataType}`")
                   ImportHandler = importHandler }
                : TableColumn))
            |> Ok
        with exn ->
            Error $"Error creating table columns: {exn}"

    let getTable (ctx: SqliteContext) (tableName: string) =
        Operations.selectTableModelRecord ctx [ "WHERE name = @0" ] [ tableName ]

    let getVersion (ctx: SqliteContext) (tableName: string) (version: int) =
        Operations.selectTableModelVersionRecord
            ctx
            [ "WHERE table_model = @0 AND version = @1;" ]
            [ tableName; version ]

    let getLatestVersion (ctx: SqliteContext) (tableName: string) =
        Operations.selectTableModelVersionRecord
            ctx
            [ "WHERE table_model = @0 ORDER BY version DESC LIMIT 1;" ]
            [ tableName ]

    let tryCreateTableModel (ctx: SqliteContext) (tableName: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx tableName
        | ItemVersion.Specific v -> getVersion ctx tableName v
        |> Option.map (fun tv ->
            createColumns ctx tv.Id
            |> Result.map (fun tc ->
                ({ Name = tv.TableModel
                   Columns = tc
                   Rows = [] }
                : TableModel)))
        |> Option.defaultValue (Error $"Table `{tableName}` not found")

    let loadTableFromJson (ctx: SqliteContext) (propertyName: string) (json: JsonElement) =
        match Json.tryGetStringProperty propertyName json, Json.tryGetIntProperty "version" json with
        | Some tn, Some v -> ItemVersion.Specific v |> tryCreateTableModel ctx tn
        | Some tn, None -> ItemVersion.Latest |> tryCreateTableModel ctx tn
        | None, _ -> Error $"`{propertyName}` property missing"

    let latestVersion (ctx: SqliteContext) (tableName: string) =
        ctx.Bespoke(
            "SELECT version FROM table_model_versions WHERE table_model = @0 ORDER BY version DESC LIMIT 1",
            [ tableName ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead

    let getVersionId (ctx: SqliteContext) (tableName: string) (version: int) =
        ctx.Bespoke(
            "SELECT id FROM table_model_versions WHERE table_model = @0 AND version = @1 LIMIT 1;",
            [ tableName; version ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let addLatestVersion (ctx: SqliteContext) (id: IdType) (tableName: string) (columns: NewColumn list) =
        let version =
            match latestVersion ctx tableName with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION - if there is no version this is a new table.
                ({ Name = tableName }: Parameters.NewTableModel)
                |> Operations.insertTableModel ctx

                1

        let versionId = id.Get()

        ({ Id = versionId
           TableModel = tableName
           Version = version
           CreatedOn = timestamp () }
        : Parameters.NewTableModelVersion)
        |> Operations.insertTableModelVersion ctx

        columns
        |> List.iteri (fun i tc ->
            match tc.ImportHandler with
            | Some ih ->
                use ms = new MemoryStream(ih.ToUtf8Bytes())

                ({ Id = tc.Id.Get()
                   TableVersionId = versionId
                   Name = tc.Name
                   DataType = tc.DataType
                   Optional = tc.Optional
                   ImportHandler = BlobField.FromBytes ms |> Some
                   ColumnIndex = i }
                : Parameters.NewTableColumn)
                |> Operations.insertTableColumn ctx
            | None ->
                ({ Id = tc.Id.Get()
                   TableVersionId = versionId
                   Name = tc.Name
                   DataType = tc.DataType
                   Optional = tc.Optional
                   ImportHandler = None
                   ColumnIndex = i }
                : Parameters.NewTableColumn)
                |> Operations.insertTableColumn ctx)

    let addLatestVersionTransaction (ctx: SqliteContext) (id: IdType) (tableName: string) (columns: NewColumn list) =
        ctx.ExecuteInTransaction(fun t -> addLatestVersion t id tableName columns)

    let addSpecificVersion
        (ctx: SqliteContext)
        (id: IdType)
        (tableName: string)
        (columns: NewColumn list)
        (version: int)
        =
        match getVersionId ctx tableName version with
        | Some _ -> Error $"Version `{version}` of table `{tableName}` already exists."
        | None ->
            match Operations.selectTableModelRecord ctx [ "WHERE name = @0;" ] [ tableName ] with
            | Some _ -> ()
            | None ->
                ({ Name = tableName }: Parameters.NewTableModel)
                |> Operations.insertTableModel ctx

            let versionId = id.Get()

            ({ Id = versionId
               TableModel = tableName
               Version = version
               CreatedOn = timestamp () }
            : Parameters.NewTableModelVersion)
            |> Operations.insertTableModelVersion ctx

            columns
            |> List.iteri (fun i tc ->
                match tc.ImportHandler with
                | Some ih ->
                    use ms = new MemoryStream(ih.ToUtf8Bytes())

                    ({ Id = tc.Id.Get()
                       TableVersionId = versionId
                       Name = tc.Name
                       DataType = tc.DataType
                       Optional = tc.Optional
                       ImportHandler = BlobField.FromBytes ms |> Some
                       ColumnIndex = i }
                    : Parameters.NewTableColumn)
                    |> Operations.insertTableColumn ctx
                | None ->
                    ({ Id = tc.Id.Get()
                       TableVersionId = versionId
                       Name = tc.Name
                       DataType = tc.DataType
                       Optional = tc.Optional
                       ImportHandler = None
                       ColumnIndex = i }
                    : Parameters.NewTableColumn)
                    |> Operations.insertTableColumn ctx)
            |> Ok

    let addSpecificVersionTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (tableName: string)
        (columns: NewColumn list)
        (version: int)
        =
        ctx.ExecuteInTransactionV2(fun t -> addSpecificVersion t id tableName columns version)

    let addVersion (ctx: SqliteContext) (table: NewTableVersion) =
        match table.Version with
        | ItemVersion.Latest -> addLatestVersion ctx table.Id table.Name table.Columns |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx table.Id table.Name table.Columns v

    let add (ctx: SqliteContext) (tableName: string) =
        ({ Name = tableName }: Parameters.NewTableModel)
        |> Operations.insertTableModel ctx

    let addColumn (ctx: SqliteContext) (versionReference: string) (column: NewColumn) =
        match Operations.selectTableModelVersionRecord ctx [ "WHERE reference = @0" ] [ versionReference ] with
        | Some tv ->
            let colIndex =
                Operations.selectTableColumnRecord
                    ctx
                    [ "WHERE table_version_id = @0 ORDER BY column_index DESC LIMIT 1" ]
                    [ tv.Version ]
                |> Option.map (fun tcr -> tcr.ColumnIndex + 1)
                |> Option.defaultValue 0

            match column.ImportHandler with
            | Some ih ->
                use ms = new MemoryStream(ih.ToUtf8Bytes())

                ({ Id = column.Id.Get()
                   TableVersionId = tv.Id
                   Name = column.Name
                   DataType = column.DataType
                   Optional = column.Optional
                   ImportHandler = BlobField.FromBytes ms |> Some
                   ColumnIndex = colIndex }
                : Parameters.NewTableColumn)
                |> Operations.insertTableColumn ctx
            | None ->
                ({ Id = column.Id.Get()
                   TableVersionId = tv.Id
                   Name = column.Name
                   DataType = column.DataType
                   Optional = column.Optional
                   ImportHandler = None
                   ColumnIndex = colIndex }
                : Parameters.NewTableColumn)
                |> Operations.insertTableColumn ctx
            |> Ok
        | None -> Error $"Table version `{versionReference}` not found"

    let addVersionTransaction (ctx: SqliteContext) (table: NewTableVersion) =
        ctx.ExecuteInTransactionV2(fun t -> addVersion t table)

    let addTransaction (ctx: SqliteContext) (tableName: string) =
        ctx.ExecuteInTransaction(fun t -> add t tableName)

    let addColumnTransaction (ctx: SqliteContext) (versionReference: string) (column: NewColumn) =
        ctx.ExecuteInTransactionV2(fun t -> addColumn t versionReference column)
