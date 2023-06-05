namespace FPype.Configuration

open System
open System.IO
open FPype.Configuration.Persistence
open FPype.Data.Models
open FPype.Data.Store
open Freql.Sqlite

type ConfigurationStore(ctx: SqliteContext) =

    static member Initialize(path, ?additionActions: string list, ?metadata: Map<string, string>) =
        match File.Exists path with
        | true ->
            let cfg = SqliteContext.Open path |> ConfigurationStore
            // Sync action types to make sure any new ones exist.
            cfg.SyncActionTypes()
            cfg

        | false ->
            use ctx = SqliteContext.Create path

            [ Records.MetadataItem.CreateTableSql()
              Records.ActionType.CreateTableSql()
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

            match additionActions with
            | Some aa -> Actions.names @ aa
            | None -> Actions.names
            |> List.iter (fun n -> ({ Name = n }: Parameters.NewActionType) |> Operations.insertActionType ctx)

            metadata
            |> Option.iter (fun md ->
                md
                |> Map.iter (fun k v ->
                    ({ ItemKey = k; ItemValue = v }: Parameters.NewMetadataItem)
                    |> Operations.insertMetadataItem ctx))

            ConfigurationStore ctx

    static member Load(path) =
        SqliteContext.Open path |> ConfigurationStore

    member pc.SyncActionTypes(?additionActions: string list) =
        let types = Actions.getAllTypes ctx

        match additionActions with
        | Some aa -> Actions.names @ aa
        | None -> Actions.names
        |> List.iter (fun an ->
            match
                types
                |> List.exists (fun at -> String.Equals(at.Name, an, StringComparison.OrdinalIgnoreCase))
            with
            | true -> ()
            | false -> Actions.addType ctx an)

    member pc.AddMetadataItem(key: string, value: string, ?allowUpdate: bool) =
        match pc.GetMetadataItem key with
        | Some v ->
            match allowUpdate |> Option.defaultValue false with
            | true ->
                ctx.ExecuteVerbatimNonQueryAnon(
                    "UPDATE __metadata SET item_value = @0 WHERE item_key = @1",
                    [ value; key ]
                )
                |> ignore
            | false -> ()
        | None ->
            ({ ItemKey = key; ItemValue = value }: Parameters.NewMetadataItem)
            |> Operations.insertMetadataItem ctx

    member pc.GetMetadataItem(key: string) =
        Operations.selectMetadataItemRecord ctx [ "WHERE item_key = @0" ] [ key ]

    member pc.GetMetadata() =
        Operations.selectMetadataItemRecords ctx [] []
        |> List.map (fun md -> md.ItemKey, md.ItemValue)
        |> Map.ofList

    member pc.GetTable(tableName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Tables.tryCreateTableModel ctx tableName

    /// <summary>
    /// Add a table but not a version (or columns) - essentially a placeholder.
    /// This is mostly for internal use.
    /// </summary>
    /// <param name="tableName">The table name</param>
    member pc.AddTable(tableName) = Tables.addTransaction ctx tableName

    member pc.AddTableVersion(id, tableName, columns, ?version) =
        ({ Id = id
           Name = tableName
           Version = ItemVersion.FromOptional version
           Columns = columns }
        : Tables.NewTableVersion)
        |> Tables.addVersionTransaction ctx

    /// <summary>
    /// Add a new table column. This is mostly for internal use
    /// </summary>
    /// <param name="versionReference">The table version reference</param>
    /// <param name="column">The new column</param>
    member pc.AddTableColumn(versionReference, column) =
        Tables.addColumnTransaction ctx versionReference column

    member pc.GetQuery(queryName, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> Queries.get ctx queryName

    /// <summary>
    /// Add a query but not a version (or columns) - essentially a placeholder.
    /// This is mostly for internal use.
    /// </summary>
    /// <param name="queryName">The query name</param>
    member pc.AddQuery(queryName) = Queries.addTransaction ctx queryName

    member pc.AddQueryVersion(id, name, query, ?version: ItemVersion) =
        ({ Id = id
           Name = name
           Version = ItemVersion.FromOptional version
           Query = query }
        : Queries.NewQueryVersion)
        |> Queries.addVersionTransaction ctx

    member pc.CreateActions(pipelineId, ?version: ItemVersion, ?additionActions: Actions.ActionCollection) =
        ItemVersion.FromOptional version
        |> Actions.createActions ctx pipelineId additionActions
        |> Result.mapError (fun msg -> $"Could not create actions: {msg}")

    member pc.GetTableObjectMapper(name, ?version: ItemVersion) =
        ItemVersion.FromOptional version |> TableObjectMappers.load ctx name

    /// <summary>
    /// Add a mapper but not a version (or columns) - essentially a placeholder.
    /// This is mostly for internal use.
    /// </summary>
    /// <param name="mapperName">The mapper name</param>
    member pc.AddTableObjectMapper(mapperName: string) =
        TableObjectMappers.addTransaction ctx mapperName

    member pc.AddTableObjectMapperVersion(id, name, mapper, ?version) =
        ({ Id = id
           Name = name
           Version = ItemVersion.FromOptional version
           Mapper = mapper }
        : TableObjectMappers.NewTableObjectMapperVersion)
        |> TableObjectMappers.addRawVersionTransaction ctx

    member pc.GetTableObjectMapper(name, ?version: ItemVersion) = failwith "TODO"
    //ItemVersion.FromOptional version |> TableObjectMappers.load ctx name

    /// <summary>
    /// Add a mapper but not a version (or columns) - essentially a placeholder.
    /// This is mostly for internal use.
    /// </summary>
    /// <param name="mapperName">The mapper name</param>
    member pc.AddObjectTableMapper(mapperName: string) =
        ObjectTableMappers.addTransaction ctx mapperName

    member pc.AddObjectTableMapperVersion(id, name, tableVersionId: string, mapper, ?version) =
        ({ Id = id
           Name = name
           TableVersionId = tableVersionId
           Version = ItemVersion.FromOptional version
           Mapper = mapper }
        : ObjectTableMappers.NewObjectTableMapperVersion)
        |> ObjectTableMappers.addRawVersionTransaction ctx

    member pc.AddPipeline(pipelineName: string) =
        Pipelines.addTransaction ctx pipelineName

    member pc.AddPipelineVersion(id, name, description, ?version) =
        ({ Id = id
           Name = name
           Description = description
           Version = ItemVersion.FromOptional version }
        : Pipelines.NewPipelineVersion)
        |> Pipelines.addVersionTransaction ctx

    member pc.GetPipelineVersion(name, ?version: ItemVersion) =
        Pipelines.get ctx name (version |> ItemVersion.FromOptional)

    member pc.GetPipelineResources(pipelineVersionId) =
        Resources.getPipelineResources ctx pipelineVersionId

    member pc.GetResourceVersion(resourceVersionId) =
        Resources.getResourceVersionById ctx resourceVersionId

    member pc.AddResourceFile(id, resource, resourceType, filePath, ?version) =
        match File.Exists filePath with
        | true ->
            use ms = new MemoryStream(File.ReadAllBytes filePath)

            ItemVersion.FromOptional version
            |> Resources.addVersionTransaction ctx id resource resourceType ms
        | false -> Error $"File `{filePath}` does not exist."

    /// <summary>
    /// Add a resource but not a version (or columns) - essentially a placeholder.
    /// This is mostly for internal use.
    /// </summary>
    /// <param name="resourceName">The resource name</param>
    member pc.AddResource(resourceName: string) =
        Resources.addTransaction ctx resourceName

    member pc.AddResourceVersion
        (
            id: IdType,
            resource: string,
            resourceType: string,
            raw: byte array,
            ?version: ItemVersion
        ) =
        use ms = new MemoryStream(raw)

        ItemVersion.FromOptional version
        |> Resources.addVersionTransaction ctx id resource resourceType ms

    member pc.ImportFromFile(path: string) = Import.fromFileTransaction ctx path

//member pc.GetPipelineResources(pipeline: string, ?version: Version) =
