namespace FPype.Configuration

open FPype.Data.Models
open FPype.Data.Store
open Freql.Sqlite

type ConfigurationStore(ctx: SqliteContext) =

    static member Load(path) =
        SqliteContext.Open path |> ConfigurationStore

    member pc.GetTable(tableName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Tables.tryCreateTableModel ctx tableName

    member pc.GetQuery(queryName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Queries.get ctx queryName

    member pc.CreateActions(pipelineId, ?version: ItemVersion) =
        ItemVersion.FromOptional version
        |> Actions.createActions ctx pipelineId
        |> Result.mapError (fun msg -> $"Could not create actions: {msg}")

    member pc.GetTableObjectMapper(name, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> TableObjectMappers.load ctx name
