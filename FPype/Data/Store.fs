namespace FPype.Data

open FPype.Data.Models

module Store =

    open System
    open System.IO
    open Microsoft.Data.Sqlite
    open Freql.Sqlite
    open Freql.Core.Common.Types
    open FsToolbox.Extensions
    open FPype.Core.Types
    open Models
    
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
            error TEXT NOT NULL,
            value TEXT NOT NULL
        );
        """

        let logTableSql =
            """
        CREATE TABLE __log (
            step TEXT NOT NULL,
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
          Internal.resourcesTableSql ]
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
    
    type ActionResult =
        { Step: string
          Result: string
          StartUtc: DateTime
          EndUtc: DateTime
          Serial: int64 }

    type ImportError =
        { Step: string
          Error: string
          Value: string }

    type LogItem =
        { Step: string
          Message: string
          IsError: bool
          IsWarning: bool
          TimestampUtc: DateTime }

    type StateValue = { Name: string; Value: string }

    let updateStateValue (ctx: SqliteContext) (name: string) (newValue: string) =
        ctx.ExecuteVerbatimNonQueryAnon("UPDATE  SET value = @0 WHERE name = @1;", [ box newValue; box name ])

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

    let addResource (ctx: SqliteContext) (name: string) (resourceType: string) (raw: MemoryStream) =
        let hash = raw.GetSHA256Hash()
        
        ({
            Name = name
            Type = resourceType
            Data = BlobField.FromBytes raw
            Hash = hash
        }: Resource)
        |> fun r -> ctx.Insert("__resources", r)
        
    let getResource (ctx: SqliteContext) (name: string) =
        ctx.SelectSingleAnon<Resource>(
            "SELECT name, type, data, hash FROM __resources WHERE name = @0;",
            [ box name ]
        )
    
    let addResult (ctx: SqliteContext) (result: ActionResult) = ctx.Insert("__run_state", result)

    let addImportError (ctx: SqliteContext) (error: ImportError) = ctx.Insert("__import_errors", error)

    let addLogItem (ctx: SqliteContext) (item: LogItem) = ctx.Insert("__log", item)

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
                let tn =
                    typeName c.Type true |> fun (a, b) -> $"{a}{b}"

                $"{c.Name} {tn}")
            |> String.concat ","

        String.concat
            ""
            [ $"CREATE TABLE {tableName} ("
              columnText
              ")" ]
        |> ctx.ExecuteSqlNonQuery
        |> fun _ -> columns

    let insert (ctx: SqliteContext) (model: TableModel) =
        let (columns, parameters) =
            model.Columns
            |> List.mapi (fun i c -> $"{c.Name}", $"@{i}")
            |> List.fold (fun (accC, accP) (c, p) -> accC @ [ c ], accP @ [ p ]) ([], [])
            |> fun (c, p) -> String.concat "," c, String.concat "," p

        let sql =
            String.concat
                ""
                [ $"INSERT INTO {model.Name} ("
                  columns
                  ")"
                  " VALUES ("
                  parameters
                  ")" ]

        ctx.ExecuteInTransaction (fun t ->
            model.Rows
            |> List.map (fun r -> t.ExecuteVerbatimNonQueryAnon(sql, r.Box())))

    let mapper (columns: TableColumn list) (reader: SqliteDataReader) =
        let rec handler t i =
            match t with
            | BaseType.Boolean -> reader.GetBoolean i |> Value.Boolean
            | BaseType.Byte -> reader.GetByte i |> Value.Byte
            | BaseType.Char -> reader.GetChar i |> Value.Char
            | BaseType.Decimal -> reader.GetDecimal i |> Value.Decimal
            | BaseType.Double -> reader.GetDouble i |> Value.Double
            | BaseType.Float -> reader.GetDouble i |> Value.Float
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
        let names =
            model.Columns
            |> List.map (fun n -> n.Name)
            |> String.concat ","

        let sql =
            [ "SELECT"; names; "FROM"; model.Name ]
            |> String.concat " "

        ctx.Bespoke<TableRow>(sql, [], mapper model.Columns)

    let conditionalSelect (ctx: SqliteContext) (model: TableModel) (conditions: string) (parameters: obj list) =
        let names =
            model.Columns
            |> List.map (fun n -> n.Name)
            |> String.concat ","

        let sql =
            [ "SELECT"
              names
              "FROM"
              model.Name
              conditions ]
            |> String.concat " "

        ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)

    let bespokeSelect (ctx: SqliteContext) (model: TableModel) (sql: string) (parameters: obj list) =
        ctx.Bespoke<TableRow>(sql, parameters, mapper model.Columns)

    type PipelineStore(ctx: SqliteContext) =

        static member Open(path) =
            SqliteContext.Open(path) |> PipelineStore

        static member Create(path) =
            let ctx = SqliteContext.Create path
            initialize ctx
            PipelineStore ctx

        member ps.AddStateValue(name, value) =
            addStateValue ctx { Name = name; Value = value }

        member ps.UpdateStateValue(name, value) = updateStateValue ctx name value
            
        member ps.GetState() = getState ctx

        member ps.GetStateValue(key) =
            getStateValue ctx key
            |> Option.map (fun sv -> sv.Value)
        
        member ps.AddDataSource(name, sourceType, uri, collectionName) =
            ({ Name = name
               Type = sourceType
               Uri = uri
               CollectionName = collectionName }: DataSource)
            |> addDataSource ctx

        member ps.GetDataSource(name) = getDataSource ctx name

        member ps.GetSourcesByCollection(collectionName) =
            getDataSourcesByCollectionName ctx collectionName

        member ps.AddArtifact(name, bucket, artifactType, data: byte array) =
            use ms = new MemoryStream(data)

            ({ Name = name
               Bucket = bucket
               Type = artifactType
               Data = BlobField.FromStream ms }: Artifact)
            |> addArtifact ctx

        member ps.GetArtifact(name) = getArtifact ctx name

        member ps.AddResult(step, result, startUtc, endUtc, serial) =
            ({ Step = step
               Result = result
               StartUtc = startUtc
               EndUtc = endUtc
               Serial = serial }: ActionResult)
            |> addResult ctx

        member ps.AddImportError(step, error, value) =
            ({ Step = step
               Error = error
               Value = value }: ImportError)
            |> addImportError ctx

        member ps.CreateTable(name, columns) =
            createTable ctx name columns
            |> fun c -> ({ Name = name; Columns = c; Rows = [] }: TableModel)

        member ps.CreateTable(model: TableModel) =
            createTable ctx model.Name model.Columns |> ignore
            model
        
        
        member ps.InsertRows(table: TableModel) = insert ctx table

        member ps.SelectRows(table: TableModel) =
            select ctx table
            |> fun r -> { table with Rows = r }

        member ps.SelectRows(table: TableModel, condition, parameters) =
            conditionalSelect ctx table condition parameters
            |> fun r -> { table with Rows = r }

        member ps.BespokeSelectRows(table: TableModel, sql, parameters) =
            bespokeSelect ctx table sql parameters
            |> fun r -> { table with Rows = r }

        member ps.Log(step, message) =
            ({ Step = step
               Message = message
               IsError = false
               IsWarning = false
               TimestampUtc = DateTime.UtcNow }: LogItem)
            |> addLogItem ctx

        member ps.LogError(step, message) =
            ({ Step = step
               Message = message
               IsError = true
               IsWarning = false
               TimestampUtc = DateTime.UtcNow }: LogItem)
            |> addLogItem ctx

        member ps.LogWarning(step, message) =
            ({ Step = step
               Message = message
               IsError = false
               IsWarning = true
               TimestampUtc = DateTime.UtcNow }: LogItem)
            |> addLogItem ctx