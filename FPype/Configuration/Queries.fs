namespace FPype.Configuration

[<RequireQualifiedAccess>]
module Queries =

    open System
    open System.IO
    open System.Text.Json
    open Freql.Core.Common.Types
    open Freql.Sqlite
    open FsToolbox.Core
    open FsToolbox.Extensions
    open FPype.Configuration.Persistence

    let getLatestVersion (ctx: SqliteContext) (queryName: string) =
        Operations.selectQueryVersionRecord
            ctx
            [ "WHERE query_versions.`query_name` = @0"; "ORDER BY version DESC LIMIT 1;" ]
            [ queryName ]

    let getVersion (ctx: SqliteContext) (queryName: string) (version: int) =
        Operations.selectQueryVersionRecord
            ctx
            [ "WHERE query_versions.`query_name` = @0 AND query_versions.version = @0" ]
            [ queryName; version ]

    let get (ctx: SqliteContext) (queryName: string) (version: ItemVersion) =
        match version with
        | Latest -> getLatestVersion ctx queryName
        | Specific v -> getVersion ctx queryName v
        |> Option.map (fun qr -> qr.QueryBlob |> blobToString)

    let tryCreate (ctx: SqliteContext) (json: JsonElement) =
        match Json.tryGetStringProperty "query" json, Json.tryGetIntProperty "version" json with
        | Some q, Some v -> ItemVersion.Specific v |> get ctx q
        | Some q, None -> ItemVersion.Latest |> get ctx q
        | None, _ -> None

    let latestVersion (ctx: SqliteContext) (name: string) =
        ctx.Bespoke(
            "SELECT version FROM query_versions WHERE query_name = @0 ORDER BY version DESC LIMIT 1;",
            [ name ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetInt32(0) ]
        )
        |> List.tryHead

    let add (ctx: SqliteContext) (name: string) (query: string) =
        use ms = new MemoryStream(query.ToUtf8Bytes())

        let version =
            match latestVersion ctx name with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION if no version exists the query is new.
                ({ Name = name }: Parameters.NewQuery) |> Operations.insertQuery ctx
                1

        let hash = ms.GetSHA256Hash()

        ({ Id = createId ()
           QueryName = name
           Version = version
           QueryBlob = BlobField.FromBytes ms
           Hash = hash
           CreatedOn = timestamp () }: Parameters.NewQueryVersion)
        |> Operations.insertQueryVersion ctx

    let addTransaction (ctx: SqliteContext) (name: string) (query: string) =
        ctx.ExecuteInTransaction(fun t -> add t name query)