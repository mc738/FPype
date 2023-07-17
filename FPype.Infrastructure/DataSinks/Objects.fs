namespace FPype.Infrastructure.DataSinks

open System
open Freql.Core.Common.Types

[<RequireQualifiedAccess>]
module Objects =

    open System.IO
    open Freql.Sqlite
    open FsToolbox.Core.Results


    [<AutoOpen>]
    module private Internal =

        type ObjectSinkItem =
            { Id: string
              ReceivedTimestamp: DateTime
              RawBlob: BlobField
              Hash: string }

            static member CreateTableSql() =
                """
            CREATE TABLE IF NOT EXISTS object_sink (
                id TEXT NOT NULL,
                received_timestamp TEXT NOT NULL,
                raw_data BLOB NOT NULL,
                hash TEXT NOT NULL
            )
            """

        ()

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
            ({ Message = $"Error creating `({schema.Name})` table: {exn.Message}"
               DisplayMessage = $"Error creating `({schema.Name})` table"
               Exception = Some exn }
            : FailureResult)
            |> Error
        |> ActionResult.fromResult


    ()
