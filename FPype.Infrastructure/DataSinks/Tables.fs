namespace FPype.Infrastructure.DataSinks

[<RequireQualifiedAccess>]
module Tables =

    open System
    open System.IO    
    open Freql.Core.Common.Types
    open Freql.Sqlite
    open FsToolbox.Core.Results
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.ModelExtensions.Sqlite
    open FPype.Configuration
    
    [<AutoOpen>]
    module private Internal =

        let createDataSinkTables (ctx: SqliteContext) =
            [ ReadRequest.CreateTableSql()
              Metadata.CreateTableSql()
              InsertError.CreatedTableSql() ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

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

        let appendDataSinkData (id: string) (row: TableRow) =
            [ Value.String id; Value.DateTime DateTime.UtcNow ] |> row.AppendValues

        let createTable (ctx: SqliteContext) (table: TableModel) = table.SqliteCreateTable ctx

        let getLatestReadRequest (ctx: SqliteContext) (requesterId: string) =
            let sql =
                """
                 SELECT
                     request_id,
                     requester,
                     request_timestamp,
                     was_successful
                 FROM __read_requests
                 WHERE
                     requester = @0 AND was_successful = TRUE
                 ORDER BY DATETIME(request_timestamp)
                 LIMIT 1;
                 """

            ctx.SelectSingleAnon<ReadRequest>(sql, [ requesterId ])

        let insertReadRequest (ctx: SqliteContext) (requestRequest: ReadRequest) =
            ctx.Insert("__read_requests", requestRequest)

        let insertMetadata (ctx: SqliteContext) (id: string) (key: string) (value: string) =
            ({ ItemId = id
               ItemKey = key
               ItemValue = value }
            : Metadata)
            |> fun md -> ctx.Insert("__metadata", md)

        let insertError (ctx: SqliteContext) (errorMessage: string) (data: byte array) =
            use ms = new MemoryStream(data)

            ({ ErrorMessage = errorMessage
               DataTimestamp = DateTime.UtcNow
               DataBlob = BlobField.FromStream ms }
            : InsertError)
            |> fun ie -> ctx.Insert("__insert_errors", ie)

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

                createDataSinkTables ctx |> Ok
        with exn ->
            ({ Message = $"Error creating `({schema.Name})` table: {exn.Message}"
               DisplayMessage = $"Error creating `({schema.Name})` table"
               Exception = Some exn }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult

    let insertRow
        (ctx: SqliteContext)
        (idType: IdType option)
        (tableSchema: TableSchema)
        (metadata: Map<string, string>)
        (row: TableRow)
        =

        try
            let id = idType |> Option.defaultValue IdType.Generated |> (fun id -> id.Get())

            let table = tableFromSchema tableSchema |> appendRow (row |> appendDataSinkData id)

            metadata |> Map.iter (insertMetadata ctx id)

            match table.SqliteInsert(ctx) with
            | Ok r -> Ok($"Successfully inserted {r.Length} row(s) into table {table.Name}")
            | Error e ->
                // Insert the error

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

    let selectRows (ctx: SqliteContext) (parameters: SelectOperationParameters) (tableSchema: TableSchema) =
        let table = TableModel.FromSchema tableSchema

        let (conditionSql, parameters) =
            match parameters.Operation with
            | SelectOperation.All -> String.Empty, []
            | SelectOperation.From timestamp -> "WHERE DATETIME(ds__timestamp) > DATETIME(@0)", [ box timestamp ]
            | SelectOperation.Between(fromTimestamp, toTimestamp) ->
                "WHERE DATETIME(ds__timestamp) > DATETIME(@0) AND DATETIME(ds__timestamp) <= DATETIME(@1)",
                [ box fromTimestamp; box toTimestamp ]
            | SelectOperation.SinceLastRead cutOffTimestamp ->
                match getLatestReadRequest ctx parameters.RequesterId with
                | Some rr -> "WHERE DATETIME(ds__timestamp) > DATETIME(@0)", [ rr.RequestTimestamp ]
                | None ->
                    match cutOffTimestamp with
                    | Some cts -> "WHERE DATETIME(ds__timestamp) > DATETIME(@0)", [ box cts ]
                    | None -> String.Empty, []

        table.SqliteConditionalSelect(ctx, conditionSql, parameters)

    let readRows (ctx: SqliteContext) (parameters: SelectOperationParameters) (tableSchema: TableSchema) =
        // NOTE should this be passed into selectRows to ensure only rows up until this point are read?
        let timestamp = DateTime.UtcNow

        let rows = selectRows ctx parameters tableSchema

        match parameters.Operation with
        | SelectOperation.SinceLastRead _ ->
            // NOTE should this record the operation type?
            ({ RequestId = System.Guid.NewGuid().ToString("n")
               Requester = parameters.RequesterId
               RequestTimestamp = timestamp
               WasSuccessful = true }
            : ReadRequest)
            |> insertReadRequest ctx
        | _ -> ()

        rows
