namespace FPype.Configuration

open System.Text.Json
open FPype.Configuration.Persistence
open Freql.Sqlite
open FsToolbox.Core

module Queries =

    let getQuery (ctx: SqliteContext) (queryName: string) =
        Operations.selectQueriesRecord ctx [ "WHERE name = @0" ] [ queryName ]
        |> Option.map (fun qr -> qr.QueryBlob |> blobToString)

    let tryCreate (ctx: SqliteContext) (json: JsonElement) =
        Json.tryGetStringProperty "query" json |> Option.bind (getQuery ctx)
