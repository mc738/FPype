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

    let insertMetadata (ctx: SqliteContext) (id: string) (key: string) (value: string) =
        ({ ItemId = id
           ItemKey = key
           ItemValue = value }
        : Metadata)
        |> fun md -> ctx.Insert(Metadata.TableName(), md)

    let insertGlobalMetadata (ctx:SqliteContext) (key: string) (value: string) =
        insertMetadata ctx (Metadata.GlobalItemId()) key value
        
    let insertError (ctx: SqliteContext) (errorMessage: string) (data: byte array) =
        use ms = new MemoryStream(data)

        ({ ErrorMessage = errorMessage
           DataTimestamp = DateTime.UtcNow
           DataBlob = BlobField.FromStream ms }
        : InsertError)
        |> fun ie -> ctx.Insert(InsertError.TableName(), ie)
