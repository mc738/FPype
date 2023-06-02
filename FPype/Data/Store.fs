namespace FPype.Data

open FPype.Core.Logging
open FPype.Data.Models
open Freql.Core.Common.Types
open FsToolbox.Core
open FsToolbox.Extensions

module Store =

    open System
    open System.Globalization
    open System.IO
    open Microsoft.Data.Sqlite
    open Freql.Sqlite
    open Freql.Core.Common.Types
    open FsToolbox.Extensions
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.ModelExtensions.Sqlite

    module Internal =

        let stateTableSql =
            """
        CREATE TABLE __state (
            name TEXT NOT NULL,
            value TEXT NOT NULL,
            CONSTRAINT __state_PK PRIMARY KEY (name)
        );
        """

        let runStateTableSql =
            """
        CREATE TABLE __run_state (
            step TEXT NOT NULL,
            result TEXT NOT NULL,
            start_utc TEXT NOT NULL,
            end_utc TEXT NOT NULL,
            serial INTEGER
        );
        """

        let dataSourcesTableSql =
            """
        CREATE TABLE __data_sources (
            name TEXT NOT NULL,
            type TEXT NOT NULL,
            uri TEXT NOT NULL,
            collection_name TEXT NOT NULL,
            CONSTRAINT __data_sources_PK PRIMARY KEY (name)
        );
        """

        let artifactsTableSql =
            """
        CREATE TABLE __artifacts (
            name TEXT NOT NULL,
            bucket TEXT NOT NULL,
            type TEXT NOT NULL,
            data BLOB NOT NULL,
            CONSTRAINT __artifacts_PK PRIMARY KEY (name)
        );
        """

        let importErrorsTableSql =
            """
        CREATE TABLE __import_errors (
            step TEXT NOT NULL,
            action_type TEXT NOT NULL,
            error TEXT NOT NULL,
            value TEXT NOT NULL
        );
        """

        let logTableSql =
            """
        CREATE TABLE __log (
            step TEXT NOT NULL,
            action_type TEXT NOT NULL,
            message TEXT NOT NULL,
            is_error INTEGER NOT NULL,
            is_warning INTEGER NOT NULL,
            timestamp_utc TEXT NOT NULL
        );
        """

        let resourcesTableSql =
            """
        CREATE TABLE __resources (
            name TEXT NOT NULL,
            type TEXT NOT NULL,
            data BLOB NOT NULL,
            hash TEXT NOT NULL,
            CONSTRAINT __resources_PK PRIMARY KEY (name)
        );
        """

        let cacheTableSql =
            """
            CREATE TABLE __cache (
            item_key TEXT NOT NULL,
            item_value BLOB NOT NULL,
            hash TEXT NOT NULL,
            created_on TEXT NOT NULL,
            expires_on TEXT NOT NULL,
            CONSTRAINT __cache_PK PRIMARY KEY (item_key)
        );
        """

        let tableSchemasSql =
            """
            CREATE TABLE __table_schemas (
                name TEXT NOT NULL,
                schema_blob BLOB NOT NULL,
                hash TEXT NOY NULL
            );
            """

    [<RequireQualifiedAccess>]
    module StateNames =

        let id = "__id"

        let computerName = "__computer_name"

        let userName = "__user_name"

        let basePath = "__base_path"

        let importsPath = "__imports_path"

        let exportsPath = "__exports_path"

        let tmpPath = "__tmp_value"

        let storePath = "__store_path"

        let initializedTimestamp = "__initialized_timestamp"

    (*
        let contextTableSql =
            """
        CREATE TABLE __log (
            step TEXT NOT NULL,
            message TEXT NOT NULL,
            is_error INTEGER NOT NULL,
            is_warning INTEGER NOT NULL,
            timestamp_utc TEXT NOT NULL
        );
        """
        *)
    let initialize (ctx: SqliteContext) =
        [ Internal.stateTableSql
          Internal.runStateTableSql
          Internal.dataSourcesTableSql
          Internal.artifactsTableSql
          Internal.importErrorsTableSql
          Internal.logTableSql
          Internal.resourcesTableSql
          Internal.cacheTableSql
          Internal.tableSchemasSql ]
        |> List.map ctx.ExecuteSqlNonQuery
        |> ignore

    type DataSource =
        { Name: string
          Type: string
          Uri: string
          CollectionName: string }

    type Artifact =
        { Name: string
          Bucket: string
          Type: string
          Data: BlobField }

    type Resource =
        { Name: string
          Type: string
          Data: BlobField
          Hash: string }

    type CacheItem =
        { ItemKey: string
          ItemValue: BlobField
          Hash: string
          CreatedOn: DateTime
          ExpiresOn: DateTime }

    type ActionResult =
        { Step: string
          Result: string
          StartUtc: DateTime
          EndUtc: DateTime
          Serial: int64 }

    type ImportError =
        { Step: string
          ActionType: string
          Error: string
          Value: string }

    type LogItem =
        { Step: string
          ActionType: string
          Message: string
          IsError: bool
          IsWarning: bool
          TimestampUtc: DateTime }

    type TableSchema =
        { Name: string
          SchemaBlob: BlobField
          Hash: string }

    type StateValue = { Name: string; Value: string }

    let updateStateValue (ctx: SqliteContext) (name: string) (newValue: string) =
        ctx.ExecuteVerbatimNonQueryAnon("UPDATE SET value = @0 WHERE name = @1;", [ box newValue; box name ])

    let addStateValue (ctx: SqliteContext) (value: StateValue) = ctx.Insert("__state", value)

    let getState (ctx: SqliteContext) = ctx.Select<StateValue>("__state")

    let getStateValue (ctx: SqliteContext) (key: string) =
        ctx.SelectSingleAnon<StateValue>("SELECT * FROM __state WHERE name = @0", [ key ])

    let addDataSource (ctx: SqliteContext) (source: DataSource) = ctx.Insert("__data_sources", source)

    let getDataSource (ctx: SqliteContext) (name: string) =
        ctx.SelectSingleAnon<DataSource>(
            "SELECT name, type, uri, collection_name FROM __data_sources WHERE name = @0;",
            [ box name ]
        )

    let getDataSourcesByCollectionName (ctx: SqliteContext) (name: string) =
        ctx.SelectAnon<DataSource>(
            "SELECT name, type, uri, collection_name FROM __data_sources WHERE collection_name = @0;",
            [ box name ]
        )

    let addArtifact (ctx: SqliteContext) (artifact: Artifact) = ctx.Insert("__artifacts", artifact)

    let getArtifact (ctx: SqliteContext) (name: string) =
        ctx.SelectSingleAnon<Artifact>(
            "SELECT name, bucket, type, data FROM __artifacts WHERE name = @0;",
            [ box name ]
        )

    let getArtifactBucket (ctx: SqliteContext) (name: string) =
        ctx.SelectAnon<Artifact>("SELECT name, bucket, type, data FROM __artifacts WHERE bucket = @0;", [ box name ])

    let addResource (ctx: SqliteContext) (name: string) (resourceType: string) (data: byte array) =
        use ms = new MemoryStream(data)
        let hash = ms.GetSHA256Hash()

        ({ Name = name
           Type = resourceType
           Data = BlobField.FromBytes ms
           Hash = hash }
        : Resource)
        |> fun r -> ctx.Insert("__resources", r)

    let getResource (ctx: SqliteContext) (name: string) =
        ctx.SelectSingleAnon<Resource>("SELECT name, type, data, hash FROM __resources WHERE name = @0;", [ box name ])

    let deleteCacheItem (ctx: SqliteContext) (key: string) =
        ctx.ExecuteVerbatimNonQueryAnon("DELETE FROM __cache WHERE item_key = @0;", [ key ])
        |> ignore

    let getCacheItem (ctx: SqliteContext) (key: string) =
        ctx.SelectSingleAnon<CacheItem>(
            "SELECT item_key, item_value, hash, created_on, expires_on FROM __cache WHERE name = @0",
            [ key ]
        )
        |> Option.bind (fun ci ->
            match DateTime.UtcNow <= ci.ExpiresOn with
            | true -> Some ci
            | false ->
                // If the item has expired then delete it and return none.
                deleteCacheItem ctx key |> ignore
                None)

    let addCacheItem (ctx: SqliteContext) (key: string) (value: byte array) (ttl: int) =
        let now = DateTime.UtcNow

        use ms = new MemoryStream(value)
        let hash = ms.GetSHA256Hash()

        match getCacheItem ctx key with
        | Some ci ->
            // If the item already exists then cache delete it.
            deleteCacheItem ctx key |> ignore
        | None -> ()

        ({ ItemKey = key
           ItemValue = BlobField.FromBytes ms
           Hash = hash
           CreatedOn = now
           ExpiresOn = now.AddMinutes(ttl) })
        |> fun p -> ctx.Insert("__cache", p)

    let clearCache (ctx: SqliteContext) =
        ctx.ExecuteVerbatimNonQueryAnon("DELETE FROM __cache", []) |> ignore

    let addResult (ctx: SqliteContext) (result: ActionResult) = ctx.Insert("__run_state", result)

    let addImportError (ctx: SqliteContext) (error: ImportError) = ctx.Insert("__import_errors", error)

    let addLogItem (ctx: SqliteContext) (item: LogItem) = ctx.Insert("__log", item)

    let addTableSchema (ctx: SqliteContext) (table: TableModel) =
        let schema = table.GetSchemaJson()
        use ms = new MemoryStream(schema.ToUtf8Bytes())

        ({ Name = table.Name
           SchemaBlob = BlobField.FromStream ms
           Hash = schema.GetSHA256Hash() }
        : TableSchema)
        |> fun ts -> ctx.Insert("__table_schemas", ts)

    let getTableSchema (ctx: SqliteContext) (name: string) =
        ctx.SelectSingleAnon<TableSchema>(
            "SELECT name, schema_blob, hash FROM __table_schemas WHERE name = @0;",
            [ box name ]
        )

    (*
    let rec typeName bt notNull =
        let nn = if notNull then " NOT NULL" else ""

        match bt with
        | BaseType.Boolean -> "INTEGER", nn
        | BaseType.Byte -> "INTEGER", nn
        | BaseType.Char -> "TEXT", nn
        | BaseType.Decimal -> "INTEGER", nn
        | BaseType.Double -> "INTEGER", nn
        | BaseType.Float -> "INTEGER", nn
        | BaseType.Int -> "INTEGER", nn
        | BaseType.Short -> "INTEGER", nn
        | BaseType.Long -> "INTEGER", nn
        | BaseType.String -> "TEXT", nn
        | BaseType.DateTime -> "TEXT", nn
        | BaseType.Guid -> "TEXT", nn
        | BaseType.Option t -> typeName t false

    let createTable (ctx: SqliteContext) (tableName: string) (columns: TableColumn list) =
        let columnText =
            columns
            |> List.map (fun c ->
                let tn = typeName c.Type true |> fun (a, b) -> $"{a}{b}"

                $"{c.Name} {tn}")
            |> String.concat ","

        String.concat "" [ $"CREATE TABLE IF NOT EXISTS {tableName} ("; columnText; ")" ]
        |> ctx.ExecuteSqlNonQuery
        |> fun _ -> columns

    let insert (ctx: SqliteContext) (model: TableModel) =
        let (columns, parameters) =
            model.Columns
            |> List.mapi (fun i c -> $"{c.Name}", $"@{i}")
            |> List.fold (fun (accC, accP) (c, p) -> accC @ [ c ], accP @ [ p ]) ([], [])
            |> fun (c, p) -> String.concat "," c, String.concat "," p

        let sql =
            String.concat "" [ $"INSERT INTO {model.Name} ("; columns; ")"; " VALUES ("; parameters; ")" ]

        ctx.ExecuteInTransaction(fun t -> model.Rows |> List.map (fun r -> t.ExecuteVerbatimNonQueryAnon(sql, r.Box())))

    let mapper (columns: TableColumn list) (reader: SqliteDataReader) =
        let rec handler t i =
            match t with
            | BaseType.Boolean -> reader.GetBoolean i |> Value.Boolean
            | BaseType.Byte -> reader.GetByte i |> Value.Byte
            | BaseType.Char -> reader.GetChar i |> Value.Char
            | BaseType.Decimal -> reader.GetDecimal i |> Value.Decimal
            | BaseType.Double -> reader.GetDouble i |> Value.Double
            | BaseType.Float -> reader.GetFloat i |> Value.Float
            | BaseType.Int -> reader.GetInt32 i |> Value.Int
            | BaseType.Short -> reader.GetInt16 i |> Value.Short
            | BaseType.Long -> reader.GetInt64 i |> Value.Long
            | BaseType.String -> reader.GetString i |> Value.String
            | BaseType.DateTime -> reader.GetDateTime i |> Value.DateTime
            | BaseType.Guid -> reader.GetGuid i |> Value.Guid
            | BaseType.Option it ->
                match reader.IsDBNull i with
                | true -> None |> Value.Option
                | false -> handler it i |> Some |> Value.Option

        [ while reader.Read() do
              columns
              |> List.map (fun c ->
                  let i = reader.GetOrdinal c.Name
                  handler c.Type i)
              |> TableRow.FromValues ]

    let select (ctx: SqliteContext) (model: TableModel) =
        let names = model.Columns |> List.map (fun n -> n.Name) |> String.concat ","

        let sql = [ "SELECT"; names; "FROM"; model.Name ] |> String.concat " "

        ctx.Bespoke<TableRow>(sql, [], mapper model.Columns)

    let conditionalSelect (ctx: SqliteContext) (model: TableModel) (conditions: string) (parameters: obj list) =
        let names = model.Columns |> List.map (fun n -> n.Name) |> String.concat ","

        let sql = [ "SELECT"; names; "FROM"; model.Name; conditions ] |> String.concat " "

        ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)

    let bespokeSelect (ctx: SqliteContext) (model: TableModel) (sql: string) (parameters: obj list) =
        ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)
    *)

    type PipelineLogger =
        { Handler: LogItem -> unit }

        static member Default() = { Handler = fun _ -> () }

        static member ConsoleLogger() =
            { Handler =
                fun li ->
                    if li.IsError then
                        ConsoleIO.printError $"[{li.TimestampUtc}] {li.Step} ({li.ActionType}) - {li.Message}"
                    else if li.IsWarning then
                        ConsoleIO.printWarning $"[{li.TimestampUtc}] {li.Step} ({li.ActionType}) - {li.Message}"
                    else
                        printfn $"[{li.TimestampUtc}] {li.Step} ({li.ActionType}) - {li.Message}" }

    type PipelineStore(ctx: SqliteContext, basePath: string, id: string, logger: PipelineLogger) =

        static member Open(basePath: string, id: string, ?logger: PipelineLogger) =
            let storePath = Path.Combine(basePath, id, "store.db")

            SqliteContext.Open(storePath)
            |> (fun ctx -> PipelineStore(ctx, basePath, id, logger |> Option.defaultValue (PipelineLogger.Default())))

        static member Create(basePath, id, logger: PipelineLogger) =
            let path = Path.Combine(basePath, id)

            match Directory.Exists path with
            | true -> ()
            | false -> Directory.CreateDirectory path |> ignore

            let storePath = Path.Combine(path, "store.db")

            let ctx = SqliteContext.Create storePath
            initialize ctx
            let store = PipelineStore(ctx, path, id, logger)
            store.AddStateValue(StateNames.id, id)
            store.SetBasePath(path)
            store.SetStorePath(storePath)
            store.SetComputerName()
            store.SetUserName()
            store

        static member Initialize(basePath: string, id: string, ?logger: PipelineLogger) =

            let store =
                PipelineStore.Create(basePath, id, logger |> Option.defaultValue (PipelineLogger.Default()))

            // Should some of these not allow

            // Initialization chain, the last step should always be setting the initialized timestamp.
            // This indicates it was completed successfully.
            // Starts with Ok () just for looks rather than any practical purpose (but possibly this could change.
            Ok()
            |> Result.bind (fun _ -> store.CreateImportDirectory())
            |> Result.bind (fun _ -> store.CreateExportDirectory())
            |> Result.bind (fun _ -> store.CreateTmpDirectory())
            |> Result.map (fun _ ->
                store.AddStateValue(StateNames.initializedTimestamp, DateTime.UtcNow.ToString())

                store)

        member pd.Id = id

        member ps.BasePath = basePath

        member ps.StorePath = Path.Combine(ps.BasePath, "store.db")

        member ps.DefaultImportsPath = Path.Combine(ps.BasePath, "imports")

        member ps.DefaultExportPath = Path.Combine(ps.BasePath, "exports")

        member ps.DefaultTmpPath = Path.Combine(basePath, "tmp")

        member ps.Close() = ctx.Close()

        member ps.AddStateValue(name, value) =
            addStateValue ctx { Name = name; Value = value }

        member ps.UpdateStateValue(name, value) = updateStateValue ctx name value

        member ps.GetState() = getState ctx

        member ps.GetStateValue(key) =
            getStateValue ctx key |> Option.map (fun sv -> sv.Value)

        member ps.StateValueExists(key) =
            match ps.GetStateValue key with
            | Some _ -> true
            | None -> false

        member ps.GetStateValueAsValue(key, baseType: BaseType, ?format: string) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match format with
                | Some f -> Value.FromString(str, baseType, f)
                | None -> Value.FromString(str, baseType))

        member ps.GetStateValueAsDateTime(key, ?format: string) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match format with
                | Some f ->
                    match
                        DateTime.TryParseExact(str, f, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)
                    with
                    | true, v -> Some v
                    | false, _ -> None
                | None ->
                    match DateTime.TryParse(str) with
                    | true, v -> Some v
                    | false, _ -> None)

        member ps.GetStateValueAsGuid(key, ?format: string) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match format with
                | Some f ->
                    match Guid.TryParseExact(str, f) with
                    | true, v -> Some v
                    | false, _ -> None
                | None ->
                    match Guid.TryParse(str) with
                    | true, v -> Some v
                    | false, _ -> None)

        member ps.GetStateValueAsBool(key) =
            ps.GetStateValue key
            |> Option.map (fun str -> [ "true"; "1"; "yes"; "ok" ] |> List.contains (str.ToLower()))

        member ps.GetStateValueAsInt(key) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match Int32.TryParse(str) with
                | true, v -> Some v
                | false, _ -> None)

        member ps.GetStateValueAsDouble(key) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match Double.TryParse(str) with
                | true, v -> Some v
                | false, _ -> None)

        member ps.GetStateValueAsSingle(key) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match Single.TryParse(str) with
                | true, v -> Some v
                | false, _ -> None)

        member ps.GetStateValueAsDecimal(key) =
            ps.GetStateValue key
            |> Option.bind (fun str ->
                match Decimal.TryParse(str) with
                | true, v -> Some v
                | false, _ -> None)

        /// <summary>
        /// This returns an optional string. Use ps.Id to get a non-optional version.
        /// This is useful to check the id that is stored (such as for consistency checks).
        /// </summary>
        member ps.GetId() = ps.GetStateValue(StateNames.id)

        member ps.GetComputerName() =
            ps.GetStateValue(StateNames.computerName)

        member ps.SetComputerName(computerName, ?allowOverride: bool) =
            match ps.GetComputerName(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.computerName, computerName) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.computerName, computerName)

        member ps.SetComputerName() =
            ps.SetComputerName(Environment.MachineName)

        member ps.GetUserName() = ps.GetStateValue(StateNames.userName)

        member ps.SetUserName(userName, ?allowOverride: bool) =
            match ps.GetUserName(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.userName, userName) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.userName, userName)

        member ps.SetUserName() = ps.SetUserName(Environment.UserName)

        member ps.GetBasePath() = ps.GetStateValue(StateNames.basePath)

        member ps.SetBasePath(basePath, ?allowOverride: bool) =
            match ps.GetBasePath(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.basePath, basePath) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.basePath, basePath)

        member ps.GetImportsPath() =
            ps.GetStateValue(StateNames.importsPath)

        member ps.SetImportsPath(importsPath, ?allowOverride: bool) =
            match ps.GetImportsPath(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.importsPath, importsPath) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.importsPath, importsPath)

        member ps.GetExportsPath() =
            ps.GetStateValue(StateNames.exportsPath)

        member ps.SetExportsPath(exportsPath, ?allowOverride: bool) =
            match ps.GetExportsPath(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.exportsPath, exportsPath) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.exportsPath, exportsPath)

        member ps.GetTmpPath() = ps.GetStateValue(StateNames.tmpPath)

        member ps.SetTmpPath(tmpPath, ?allowOverride: bool) =
            match ps.GetTmpPath(), allowOverride |> Option.defaultValue false with
            | Some _, true -> ps.UpdateStateValue(StateNames.tmpPath, tmpPath) |> ignore
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(StateNames.tmpPath, tmpPath)

        /// <summary>
        /// This is not really needed. ps.StorePath returns a non option version (the value should exist).
        /// However this does indicate if the store has been initialized.
        /// </summary>
        member ps.GetStorePath() = ps.GetStateValue(StateNames.storePath)

        member ps.SetStorePath(storePath) =
            match ps.GetStorePath() with
            | Some _ ->
                // NOTE - the store path can only be set once (normally in initialization). There is no override allowed.
                ()
            | None -> ps.AddStateValue(StateNames.storePath, storePath)

        member ps.CreateDirectory(name: string, ?relative: bool) =
            match relative |> Option.defaultValue true with
            | true -> ps.GetBasePath() |> Option.map (fun bp -> Path.Combine(bp, name))
            | false -> Some name
            |> Option.map (fun p ->
                match Directory.Exists p with
                | true -> ()
                | false -> Directory.CreateDirectory(p) |> ignore

                p)

        member ps.CreateImportDirectory() =
            try
                ps.CreateDirectory("imports") |> Option.iter ps.SetImportsPath |> Ok
            with ex ->
                Error $"Failed to create imports directory. Error - {ex.Message}"

        member ps.CreateExportDirectory() =
            try
                ps.CreateDirectory("exports") |> Option.iter ps.SetExportsPath |> Ok
            with ex ->
                Error $"Failed to create exports directory. Error - {ex.Message}"

        member ps.CreateTmpDirectory() =
            try
                ps.CreateDirectory("tmp") |> Option.iter ps.SetTmpPath |> Ok
            with ex ->
                Error $"Failed to create tmp directory. Error - {ex.Message}"

        member ps.ClearTmp() =
            match ps.GetTmpPath() with
            | Some p -> Directory.GetFiles(p) |> Seq.iter File.Delete
            | None -> ()

        member ps.IsInitialized() =
            match ps.GetStateValue(StateNames.initializedTimestamp) with
            | Some _ -> true
            | None -> false

        member ps.AddDataSource(name, sourceType: DataSourceType, uri, collectionName) =
            ({ Name = name
               Type = sourceType.Serialize()
               Uri = uri
               CollectionName = collectionName }
            : DataSource)
            |> addDataSource ctx

        member ps.GetDataSource(name) = getDataSource ctx name

        member ps.GetSourcesByCollection(collectionName) =
            getDataSourcesByCollectionName ctx collectionName

        member ps.AddArtifact(name, bucket, artifactType, data: byte array) =
            use ms = new MemoryStream(data)

            ({ Name = name
               Bucket = bucket
               Type = artifactType
               Data = BlobField.FromStream ms }
            : Artifact)
            |> addArtifact ctx

        member ps.GetArtifact(name) = getArtifact ctx name

        member ps.GetArtifactBucket(name) = getArtifactBucket ctx name

        member ps.AddResource(name, resourceType, data: byte array) = addResource ctx name resourceType data

        member ps.GetResourceEntity(name) = getResource ctx name

        member ps.GetResource(name) =
            getResource ctx name |> Option.map (fun r -> r.Data.ToBytes())

        member ps.AddCacheItem(key: string, value: byte array, ?ttl: int) =
            defaultArg ttl 1000000 |> addCacheItem ctx key value

        member ps.GetCacheItemEntity(key: string) = getCacheItem ctx key

        member ps.GetCacheItem(key: string) =
            getCacheItem ctx key |> Option.map (fun r -> r.ItemValue.ToBytes())

        member ps.DeleteCacheItem(key: string) = deleteCacheItem ctx key

        member ps.ClearCache() = clearCache ctx

        member ps.AddResult(step, result, startUtc, endUtc, serial) =
            ({ Step = step
               Result = result
               StartUtc = startUtc
               EndUtc = endUtc
               Serial = serial }
            : ActionResult)
            |> addResult ctx

        member ps.AddImportError(step, actionType, error, value) =
            ({ Step = step
               ActionType = actionType
               Error = error
               Value = value }
            : ImportError)
            |> addImportError ctx

        member ps.AddVariable(name: string, value: string, ?allowOverride: bool) =
            let n = $"%%{name}%%"

            match ps.GetStateValue n, allowOverride |> Option.defaultValue false with
            | Some v, true when v <> value -> ps.UpdateStateValue(n, value) |> ignore
            | Some _, true
            | Some _, false -> ()
            | None, _ -> ps.AddStateValue(n, value)

        member ps.SubstituteValues(path: string, ?otherReplacements: (string * string) list) =
            // Get the state values as a map to cut down on database calls.
            let stateMap = ps.GetState() |> List.map (fun sv -> sv.Name, sv.Value) |> Map.ofList

            [ "%IMPORTS%",
              stateMap.TryFind StateNames.importsPath
              |> Option.defaultValue ps.DefaultImportsPath
              "%EXPORTS%",
              stateMap.TryFind StateNames.exportsPath
              |> Option.defaultValue ps.DefaultExportPath
              "%TMP%", stateMap.TryFind StateNames.tmpPath |> Option.defaultValue ps.DefaultTmpPath
              "%ID%", stateMap.TryFind StateNames.id |> Option.defaultValue ps.Id
              "%COMPUTER_NAME%",
              stateMap.TryFind StateNames.computerName
              |> Option.defaultValue Environment.MachineName
              "%USER_NAME%", stateMap.TryFind StateNames.userName |> Option.defaultValue Environment.UserName
              // Variables can be stored as state values (with names starting with %).
              // These are used for expanding paths too
              yield!
                  stateMap
                  |> Map.toList
                  |> List.choose (fun (k, v) ->
                      match k.StartsWith('%') with
                      | true -> Some(k, v)
                      | false -> None)
              match otherReplacements with
              | Some orv -> yield! orv
              | None -> () ]
            |> path.ReplaceMultiple

        member ps.CreateTable(name, columns) =
            let model =
                ({ Name = name
                   Columns = columns
                   Rows = [] }
                : TableModel)

            ps.CreateTable model

        member ps.CreateTable(model: TableModel) =

            match getTableSchema ctx model.Name with
            | Some ts ->
                match Strings.equalOrdinalIgnoreCase ts.Hash (model.GetSchemaHash()) with
                | true -> ()
                | false ->
                    // Trying to add a table that area
                    // TODO What to do? error?
                    ()
            | None -> addTableSchema ctx model

            model.SqliteCreateTable(ctx) |> ignore

            model

        member ps.InsertRows(table: TableModel) =
            match getTableSchema ctx table.Name with
            | Some ts ->
                match Strings.equalOrdinalIgnoreCase ts.Hash (table.GetSchemaHash()) with
                | true -> ()
                | false ->
                    // Trying to add a table that area
                    // TODO What to do? error?
                    ()
            | None -> addTableSchema ctx table

            table.SqliteInsert ctx

        member ps.SelectRawRows(table: TableModel) = table.SqliteSelect ctx

        member ps.SelectAndAppendRows(table: TableModel) =
            ps.SelectRawRows table |> table.AppendRows

        member ps.SelectRows(table: TableModel) =
            ps.SelectRawRows table |> fun r -> { table with Rows = r }

        member ps.SelectRows(table: TableModel, condition, parameters) =
            table.SqliteConditionalSelect(ctx, condition, parameters)
            |> fun r -> { table with Rows = r }

        member ps.BespokeSelectRows(table: TableModel, sql, parameters) =
            table.SqliteBespokeSelect(ctx, sql, parameters)
            |> fun r -> { table with Rows = r }

        member ps.BespokeSelectAndAppendRows(table: TableModel, sql, parameters) =
            table.SqliteBespokeSelect(ctx, sql, parameters) |> table.AppendRows

        member ps.Log(step, actionType, message) =
            let item =
                ({ Step = step
                   ActionType = actionType
                   Message = message
                   IsError = false
                   IsWarning = false
                   TimestampUtc = DateTime.UtcNow }
                : LogItem)

            addLogItem ctx item
            logger.Handler item

        member ps.LogError(step, actionType, message) =
            let item =
                ({ Step = step
                   ActionType = actionType
                   Message = message
                   IsError = true
                   IsWarning = false
                   TimestampUtc = DateTime.UtcNow }
                : LogItem)

            addLogItem ctx item
            logger.Handler item

        member ps.LogWarning(step, actionType, message) =
            let item =
                ({ Step = step
                   ActionType = actionType
                   Message = message
                   IsError = false
                   IsWarning = true
                   TimestampUtc = DateTime.UtcNow }
                : LogItem)

            addLogItem ctx item
            logger.Handler item
