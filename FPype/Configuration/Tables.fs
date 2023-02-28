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

    type NewTable =
        { Name: string
          Version: ItemVersion
          Columns: NewColumn list }

    and NewColumn =
        { Name: string
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
                { Name = n
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
                     |> Option.defaultWith (fun _ -> failwith $"Invalid type for column `{tcr.Name}`: `{tcr.DataType}`")
                   ImportHandler = importHandler }: TableColumn))
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
        | Latest -> getLatestVersion ctx tableName
        | Specific v -> getVersion ctx tableName v
        |> Option.map (fun tv ->
            createColumns ctx tv.Id
            |> Result.map (fun tc ->
                ({ Name = tv.TableModel
                   Columns = tc
                   Rows = [] }: TableModel)))
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
            [ query; version ],
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
           CreatedOn = timestamp () }: Parameters.NewTableModelVersion)
        |> Operations.insertTableModelVersion ctx

        columns
        |> List.iteri (fun i tc ->
            match tc.ImportHandler with
            | Some ih ->
                use ms = new MemoryStream(ih.ToUtf8Bytes())

                ({ Id = createId ()
                   TableVersionId = versionId
                   Name = tc.Name
                   DataType = tc.DataType
                   Optional = tc.Optional
                   ImportHandler = BlobField.FromBytes ms |> Some
                   ColumnIndex = i }: Parameters.NewTableColumn)
                |> Operations.insertTableColumn ctx
            | None ->
                ({ Id = createId ()
                   TableVersionId = versionId
                   Name = tc.Name
                   DataType = tc.DataType
                   Optional = tc.Optional
                   ImportHandler = None
                   ColumnIndex = i }: Parameters.NewTableColumn)
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
               CreatedOn = timestamp () }: Parameters.NewTableModelVersion)
            |> Operations.insertTableModelVersion ctx

            columns
            |> List.iteri (fun i tc ->
                match tc.ImportHandler with
                | Some ih ->
                    use ms = new MemoryStream(ih.ToUtf8Bytes())

                    ({ Id = createId ()
                       TableVersionId = versionId
                       Name = tc.Name
                       DataType = tc.DataType
                       Optional = tc.Optional
                       ImportHandler = BlobField.FromBytes ms |> Some
                       ColumnIndex = i }: Parameters.NewTableColumn)
                    |> Operations.insertTableColumn ctx
                | None ->
                    ({ Id = createId ()
                       TableVersionId = versionId
                       Name = tc.Name
                       DataType = tc.DataType
                       Optional = tc.Optional
                       ImportHandler = None
                       ColumnIndex = i }: Parameters.NewTableColumn)
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

    let add
        (ctx: SqliteContext)
        (id: IdType)
        (tableName: string)
        (columns: NewColumn list)
        (version: ItemVersion)
        =
        match version with
        | ItemVersion.Latest -> addLatestVersion ctx id tableName columns |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx id tableName columns v
        
    let addTransaction
        (ctx: SqliteContext)
        (id: IdType)
        (tableName: string)
        (columns: NewColumn list)
        (version: ItemVersion)
        =
        ctx.ExecuteInTransactionV2(fun t -> add t id tableName columns version)
    