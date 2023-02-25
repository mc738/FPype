namespace FPype.Configuration
open FPype.Data.Models
open FPype.Data.Store
open Freql.Sqlite

type ConfigurationStore(ctx: SqliteContext) =

    static member Load(path) =
        SqliteContext.Open path |> ConfigurationStore

    member pc.GetTable(tableName) =
        Tables.getTable ctx tableName
        |> Option.map (fun t ->
            Tables.createColumns ctx t.Name
            |> Result.map (fun tc ->
                ({ Name = t.Name
                   Columns = tc
                   Rows = [] }: TableModel)))
        |> Option.defaultValue (Error $"Table `{tableName}` not found")

    member pc.GetQuery(queryName) = Queries.getQuery ctx queryName

    member pc.CreateActions(pipelineId) =
        Actions.createActions ctx pipelineId
        |> flattenResultList
        |> Result.mapError (fun msg -> $"Could not create actions: {msg}")

    member pc.GetObjectMapper(name) = ObjectMappers.load ctx name