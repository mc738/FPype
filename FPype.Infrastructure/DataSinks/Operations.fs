namespace FPype.Infrastructure.DataSinks

[<RequireQualifiedAccess>]
module Operations =

    open System
    open System.IO
    open Freql.Core.Common.Types
    open Freql.Sqlite

    let createDataSinkTables (ctx: SqliteContext) =
        [ ReadRequest.CreateTableSql()
          Metadata.CreateTableSql()
          InsertError.CreatedTableSql() ]
        |> List.map ctx.ExecuteSqlNonQuery
        |> ignore

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
        ctx.Insert(ReadRequest.TableName(), requestRequest)

    // Metadata

    let getMetadata (ctx: SqliteContext) (id: string) (key: string) =
        ctx.SelectSingleAnon<Metadata>(
            "SELECT item_id, item_key, item_value FROM `__metadata` WHERE item_id = @0 AND item_key = @1",
            [ id; key ]
        )

    let getMetadataValue (ctx: SqliteContext) (id: string) (key: string) =
        getMetadata ctx id key |> Option.map (fun mdi -> mdi.ItemValue)

    let getGlobalMetadata (ctx: SqliteContext) (key: string) =
        getMetadata ctx (Metadata.GlobalItemId()) key

    let getGlobalMetadataValue (ctx: SqliteContext) (key: string) =
        getGlobalMetadata ctx key |> Option.map (fun mdi -> mdi.ItemValue)

    let getAllMetadataForId (ctx: SqliteContext) (id: string) =
        ctx.SelectAnon<Metadata>("SELECT item_id, item_key, item_value FROM `__metadata` WHERE item_id = @0;", [ id ])

    let getAllGlobalMetadata (ctx: SqliteContext) =
        getAllMetadataForId ctx <| Metadata.GlobalItemId()

    let metadataExists (ctx: SqliteContext) (id: string) (key: string) = getMetadata ctx id key |> Option.isSome

    let globalMetadataExists (ctx: SqliteContext) (key: string) =
        getGlobalMetadata ctx key |> Option.isSome

    let insertMetadata (ctx: SqliteContext) (id: string) (key: string) (value: string) =
        ({ ItemId = id
           ItemKey = key
           ItemValue = value }
        : Metadata)
        |> fun md -> ctx.Insert(Metadata.TableName(), md)

    let insertGlobalMetadata (ctx: SqliteContext) (key: string) (value: string) =
        insertMetadata ctx (Metadata.GlobalItemId()) key value

    let updateMetadataValue (ctx: SqliteContext) (id: string) (key: string) (value: string) =
        ctx.ExecuteVerbatimNonQueryAnon(
            "UPDATE `__metadata` SET item_value = @0 WHERE item_id = @1 AND item_key = @2",
            [ value; id; key ]
        )

    let updateGlobalMetadataValue (ctx: SqliteContext) (key: string) (value: string) =
        updateMetadataValue ctx (Metadata.GlobalItemId()) key value

    let tryInsertMetadata (ctx: SqliteContext) (id: string) (key: string) (value: string) =
        match metadataExists ctx id key with
        | true -> Error $"Metadata item `{key}` already exists for id `{id}`"
        | false -> insertMetadata ctx id key value |> Ok

    let tryInsertGlobalMetadata (ctx: SqliteContext) (key: string) (value: string) = ()

    let insertError (ctx: SqliteContext) (errorMessage: string) (data: byte array) =
        use ms = new MemoryStream(data)

        ({ ErrorMessage = errorMessage
           DataTimestamp = DateTime.UtcNow
           DataBlob = BlobField.FromStream ms }
        : InsertError)
        |> fun ie -> ctx.Insert(InsertError.TableName(), ie)
