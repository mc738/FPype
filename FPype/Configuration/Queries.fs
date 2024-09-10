namespace FPype.Configuration

[<RequireQualifiedAccess>]
module Queries =

    open System.IO
    open System.Text.Json
    open Freql.Core.Common.Types
    open Freql.Sqlite
    open FsToolbox.Core
    open FsToolbox.Extensions
    open FPype.Configuration.Persistence

    type NewQueryVersion =
        { Id: IdType
          Name: string
          Version: ItemVersion
          Query: string }

    let getLatestVersion (ctx: SqliteContext) (queryName: string) =
        Operations.selectQueryVersionRecord
            ctx
            [ "WHERE query_versions.`query_name` = @0"; "ORDER BY version DESC LIMIT 1;" ]
            [ queryName ]

    let getVersion (ctx: SqliteContext) (queryName: string) (version: int) =
        Operations.selectQueryVersionRecord
            ctx
            [ "WHERE query_versions.`query_name` = @0 AND query_versions.version = @1;" ]
            [ queryName; version ]

    let get (ctx: SqliteContext) (queryName: string) (version: ItemVersion) =
        match version with
        | ItemVersion.Latest -> getLatestVersion ctx queryName
        | ItemVersion.Specific v -> getVersion ctx queryName v
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

    let getVersionId (ctx: SqliteContext) (query: string) (version: int) =
        ctx.Bespoke(
            "SELECT id FROM query_versions WHERE query_name = @0 AND version = @1 LIMIT 1;",
            [ query; version ],
            fun reader ->
                [ while reader.Read() do
                      reader.GetString(0) ]
        )
        |> List.tryHead

    let addLatestVersion (ctx: SqliteContext) (id: IdType) (name: string) (query: string) =
        use ms = new MemoryStream(query.ToUtf8Bytes())

        let version =
            match latestVersion ctx name with
            | Some v -> v + 1
            | None ->
                // ASSUMPTION if no version exists the query is new.
                ({ Name = name }: Parameters.NewQuery) |> Operations.insertQuery ctx
                1

        let hash = ms.GetSHA256Hash()

        ({ Id = id.Get()
           QueryName = name
           Version = version
           QueryBlob = BlobField.FromBytes ms
           Hash = hash
           CreatedOn = timestamp () }
        : Parameters.NewQueryVersion)
        |> Operations.insertQueryVersion ctx

    let addLatestTransaction (ctx: SqliteContext) (id: IdType) (name: string) (query: string) =
        ctx.ExecuteInTransaction(fun t -> addLatestVersion t id name query)

    let addSpecificVersion (ctx: SqliteContext) (id: IdType) (name: string) (query: string) (version: int) =
        match getVersionId ctx query version with
        | Some _ -> Error $"Version `{version}` of query `{query}` already exists."
        | None ->
            match Operations.selectQueryRecord ctx [ "WHERE name = @0;" ] [ name ] with
            | Some _ -> ()
            | None -> ({ Name = name }: Parameters.NewQuery) |> Operations.insertQuery ctx

            use ms = new MemoryStream(query.ToUtf8Bytes())

            let hash = ms.GetSHA256Hash()

            ({ Id = id.Get()
               QueryName = name
               Version = version
               QueryBlob = BlobField.FromBytes ms
               Hash = hash
               CreatedOn = timestamp () }
            : Parameters.NewQueryVersion)
            |> Operations.insertQueryVersion ctx
            |> Ok

    let addSpecificVersionTransaction (ctx: SqliteContext) (id: IdType) (name: string) (query: string) (version: int) =
        ctx.ExecuteInTransactionV2(fun t -> addSpecificVersion t id name query version)

    let addVersion (ctx: SqliteContext) (query: NewQueryVersion) =
        match query.Version with
        | ItemVersion.Latest -> addLatestVersion ctx query.Id query.Name query.Query |> Ok
        | ItemVersion.Specific v -> addSpecificVersion ctx query.Id query.Name query.Query v

    let addVersionTransaction (ctx: SqliteContext) (query: NewQueryVersion) =
        ctx.ExecuteInTransactionV2(fun t -> addVersion t query)

    let add (ctx: SqliteContext) (queryName: string) =
        ({ Name = queryName }: Parameters.NewQuery) |> Operations.insertQuery ctx

    let addTransaction (ctx: SqliteContext) (queryName: string) =
        ctx.ExecuteInTransaction(fun t -> add t queryName)
