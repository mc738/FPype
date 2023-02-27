namespace FPype.Configuration

open System.IO
open FPype.Configuration.Persistence
open FPype.Data.Models
open FPype.Data.Store
open Freql.Sqlite

type ConfigurationStore(ctx: SqliteContext) =

    static member Initialize(path) =
        match File.Exists path with
        | true -> SqliteContext.Open path |> ConfigurationStore
        | false ->
            let ctx = SqliteContext.Create path

            [ Records.ActionType.CreateTableSql()
              Records.Pipeline.CreateTableSql()
              Records.PipelineVersion.CreateTableSql()
              Records.PipelineAction.CreateTableSql()
              Records.PipelineArg.CreateTableSql()
              Records.Query.CreateTableSql()
              Records.QueryVersion.CreateTableSql()
              Records.Resource.CreateTableSql()
              Records.ResourceVersion.CreateTableSql()
              Records.PipelineResource.CreateTableSql()
              Records.TableModel.CreateTableSql()
              Records.TableModelVersion.CreateTableSql()
              Records.TableColumn.CreateTableSql()
              Records.TableObjectMapper.CreateTableSql()
              Records.TableObjectMapperVersion.CreateTableSql()
              Records.ObjectTableMapper.CreateTableSql()
              Records.ObjectTableMapperVersion.CreateTableSql() ]
            |> List.iter (fun sql -> ctx.ExecuteSqlNonQuery sql |> ignore)

            Actions.names
            |> List.iter (fun n -> ({ Name = n }: Parameters.NewActionType) |> Operations.insertActionType ctx)

            ConfigurationStore ctx

    static member Load(path) =
        SqliteContext.Open path |> ConfigurationStore

    member pc.GetTable(tableName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Tables.tryCreateTableModel ctx tableName

    member pc.GetQuery(queryName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Queries.get ctx queryName
        
    member pc.AddQuery(name, query) =
        Queries.add ctx name query

    member pc.CreateActions(pipelineId, ?version: ItemVersion) =
        ItemVersion.FromOptional version
        |> Actions.createActions ctx pipelineId
        |> Result.mapError (fun msg -> $"Could not create actions: {msg}")

    member pc.GetTableObjectMapper(name, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> TableObjectMappers.load ctx name

    member pc.AddTableObjectMapper(name, mapper) =
        TableObjectMappers.addRaw ctx name mapper

//member pc.GetPipelineResources(pipeline: string, ?version: Version) =
