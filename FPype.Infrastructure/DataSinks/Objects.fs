namespace FPype.Infrastructure.DataSinks

open System
open FPype.Configuration
open Freql.Core.Common.Types

[<RequireQualifiedAccess>]
module Objects =

    open System.IO
    open Freql.Sqlite
    open FsToolbox.Core.Results


    [<AutoOpen>]
    module private Internal =

        [<CLIMutable>]
        type ObjectSinkItem =
            { Id: string
              ReceivedTimestamp: DateTime
              RawBlob: BlobField
              Hash: string }

            static member TableName() = "object_sink"

            static member CreateTableSql() =
                """
            CREATE TABLE IF NOT EXISTS object_sink (
                id TEXT NOT NULL,
                received_timestamp TEXT NOT NULL,
                raw_data BLOB NOT NULL,
                hash TEXT NOT NULL
            )
            """

        let createDataSinkTables (ctx: SqliteContext) =
            [ ObjectSinkItem.CreateTableSql()
              Metadata.CreateTableSql()
              InsertError.CreatedTableSql() ]
            |> List.map ctx.ExecuteSqlNonQuery
            |> ignore

    let initialize (id: string) (subscriptionId: string) (path: string) =
        try
            let dir = Path.Combine(path, subscriptionId, id)

            Directory.CreateDirectory dir |> ignore
            let fullPath = Path.Combine(dir, $"{id}.db")

            match File.Exists fullPath with
            | true -> Ok()
            | false ->

                use ctx = SqliteContext.Create(fullPath)

                createDataSinkTables ctx |> Ok
        with exn ->
            ({ Message = $"Error creating object table: {exn.Message}"
               DisplayMessage = $"Error creating object table"
               Exception = Some exn }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult

    let insertObject
        (ctx: SqliteContext)
        (idType: IdType option)
        (metadata: Map<string, string>)
        (rawObject: byte array)
        =

        try
                



            Ok()
        with exn ->
            ({ Message = $"Unhandled exception while inserting insert rows: {exn.Message}"
               DisplayMessage = "Failed to insert rows"
               Exception = Some exn }
            : FailureResult)
            |> Error
