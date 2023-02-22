namespace FPype.Configuration.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.Sqlite

/// Module generated on 22/02/2023 22:24:28 (utc) via Freql.Sqlite.Tools.
[<RequireQualifiedAccess>]
module Records =
    /// A record representing a row in the table `action_types`.
    type ActionType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE action_types (
	name TEXT NOT NULL,
	CONSTRAINT action_types_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              action_types.`name`
        FROM action_types
        """
    
        static member TableName() = "action_types"
    
    /// A record representing a row in the table `object_mappers`.
    type ObjectMapper =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("mapper")>] Mapper: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              Mapper = BlobField.Empty() }
    
        static member CreateTableSql() = """
        CREATE TABLE object_mappers (
	name TEXT NOT NULL,
	mapper BLOB NOT NULL,
	CONSTRAINT object_mappers_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              object_mappers.`name`,
              object_mappers.`mapper`
        FROM object_mappers
        """
    
        static member TableName() = "object_mappers"
    
    /// A record representing a row in the table `pipeline_actions`.
    type PipelineAction =
        { [<JsonPropertyName("actionId")>] ActionId: string
          [<JsonPropertyName("pipelineId")>] PipelineId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionType")>] ActionType: string
          [<JsonPropertyName("actionBlob")>] ActionBlob: BlobField
          [<JsonPropertyName("step")>] Step: int64 }
    
        static member Blank() =
            { ActionId = String.Empty
              PipelineId = String.Empty
              Name = String.Empty
              ActionType = String.Empty
              ActionBlob = BlobField.Empty()
              Step = 0L }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_actions (
	action_id TEXT NOT NULL,
	pipeline_id TEXT NOT NULL,
	name TEXT NOT NULL,
	action_type TEXT NOT NULL,
	action_blob BLOB NOT NULL,
	step INTEGER NOT NULL,
	CONSTRAINT pipeline_actions_PK PRIMARY KEY (action_id),
	CONSTRAINT pipeline_actions_UN UNIQUE (pipeline_id,step),
	CONSTRAINT pipeline_actions_FK FOREIGN KEY (pipeline_id) REFERENCES pipelines(id),
	CONSTRAINT pipeline_actions_FK_1 FOREIGN KEY (action_type) REFERENCES action_types(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_actions.`action_id`,
              pipeline_actions.`pipeline_id`,
              pipeline_actions.`name`,
              pipeline_actions.`action_type`,
              pipeline_actions.`action_blob`,
              pipeline_actions.`step`
        FROM pipeline_actions
        """
    
        static member TableName() = "pipeline_actions"
    
    /// A record representing a row in the table `pipeline_args`.
    type PipelineArg =
        { [<JsonPropertyName("pipelineId")>] PipelineId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: int64
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { PipelineId = String.Empty
              Name = String.Empty
              Required = 0L
              DefaultValue = None }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_args (
	pipeline_id TEXT NOT NULL,
	name TEXT NOT NULL,
	required INTEGER NOT NULL,
	default_value TEXT,
	CONSTRAINT pipeline_args_FK FOREIGN KEY (pipeline_id) REFERENCES pipelines(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_args.`pipeline_id`,
              pipeline_args.`name`,
              pipeline_args.`required`,
              pipeline_args.`default_value`
        FROM pipeline_args
        """
    
        static member TableName() = "pipeline_args"
    
    /// A record representing a row in the table `pipelines`.
    type Pipeline =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("description")>] Description: string }
    
        static member Blank() =
            { Id = String.Empty
              Name = String.Empty
              Description = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE pipelines (
	id TEXT NOT NULL,
	name TEXT NOT NULL,
	description TEXT NOT NULL,
	CONSTRAINT pipelines_PK PRIMARY KEY (id)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipelines.`id`,
              pipelines.`name`,
              pipelines.`description`
        FROM pipelines
        """
    
        static member TableName() = "pipelines"
    
    /// A record representing a row in the table `queries`.
    type Queries =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("queryBlob")>] QueryBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              QueryBlob = BlobField.Empty() }
    
        static member CreateTableSql() = """
        CREATE TABLE queries (
	name TEXT NOT NULL,
	query_blob BLOB NOT NULL,
	CONSTRAINT queries_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              queries.`name`,
              queries.`query_blob`
        FROM queries
        """
    
        static member TableName() = "queries"
    
    /// A record representing a row in the table `table_columns`.
    type TableColumn =
        { [<JsonPropertyName("tableName")>] TableName: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandler")>] ImportHandler: BlobField option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { TableName = String.Empty
              Name = String.Empty
              DataType = String.Empty
              Optional = true
              ImportHandler = None
              ColumnIndex = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE table_columns (
	table_name TEXT NOT NULL,
	name TEXT NOT NULL,
	data_type TEXT NOT NULL,
	optional INTEGER NOT NULL,
	import_handler BLOB,
	column_index INTEGER NOT NULL,
	CONSTRAINT table_columns_PK PRIMARY KEY (table_name,name),
	CONSTRAINT table_columns_UN UNIQUE (table_name,column_index),
	CONSTRAINT table_columns_FK FOREIGN KEY (table_name) REFERENCES table_models(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              table_columns.`table_name`,
              table_columns.`name`,
              table_columns.`data_type`,
              table_columns.`optional`,
              table_columns.`import_handler`,
              table_columns.`column_index`
        FROM table_columns
        """
    
        static member TableName() = "table_columns"
    
    /// A record representing a row in the table `table_models`.
    type TableModel =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE table_models (
	name TEXT NOT NULL,
	CONSTRAINT table_models_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              table_models.`name`
        FROM table_models
        """
    
        static member TableName() = "table_models"
    

/// Module generated on 22/02/2023 22:24:28 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `action_types`.
    type NewActionType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `object_mappers`.
    type NewObjectMapper =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("mapper")>] Mapper: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              Mapper = BlobField.Empty() }
    
    
    /// A record representing a new row in the table `pipeline_actions`.
    type NewPipelineAction =
        { [<JsonPropertyName("actionId")>] ActionId: string
          [<JsonPropertyName("pipelineId")>] PipelineId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionType")>] ActionType: string
          [<JsonPropertyName("actionBlob")>] ActionBlob: BlobField
          [<JsonPropertyName("step")>] Step: int64 }
    
        static member Blank() =
            { ActionId = String.Empty
              PipelineId = String.Empty
              Name = String.Empty
              ActionType = String.Empty
              ActionBlob = BlobField.Empty()
              Step = 0L }
    
    
    /// A record representing a new row in the table `pipeline_args`.
    type NewPipelineArg =
        { [<JsonPropertyName("pipelineId")>] PipelineId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: int64
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { PipelineId = String.Empty
              Name = String.Empty
              Required = 0L
              DefaultValue = None }
    
    
    /// A record representing a new row in the table `pipelines`.
    type NewPipeline =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("description")>] Description: string }
    
        static member Blank() =
            { Id = String.Empty
              Name = String.Empty
              Description = String.Empty }
    
    
    /// A record representing a new row in the table `queries`.
    type NewQueries =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("queryBlob")>] QueryBlob: BlobField }
    
        static member Blank() =
            { Name = String.Empty
              QueryBlob = BlobField.Empty() }
    
    
    /// A record representing a new row in the table `table_columns`.
    type NewTableColumn =
        { [<JsonPropertyName("tableName")>] TableName: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandler")>] ImportHandler: BlobField option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { TableName = String.Empty
              Name = String.Empty
              DataType = String.Empty
              Optional = true
              ImportHandler = None
              ColumnIndex = 0 }
    
    
    /// A record representing a new row in the table `table_models`.
    type NewTableModel =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
/// Module generated on 22/02/2023 22:24:28 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Operations =

    let buildSql (lines: string list) = lines |> String.concat Environment.NewLine

    /// Select a `Records.ActionType` from the table `action_types`.
    /// Internally this calls `context.SelectSingleAnon<Records.ActionType>` and uses Records.ActionType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectActionTypeRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectActionTypeRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ActionType.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ActionType>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ActionType>` and uses Records.ActionType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectActionTypeRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectActionTypeRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ActionType.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ActionType>(sql, parameters)
    
    let insertActionType (context: SqliteContext) (parameters: Parameters.NewActionType) =
        context.Insert("action_types", parameters)
    
    /// Select a `Records.ObjectMapper` from the table `object_mappers`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectMapper>` and uses Records.ObjectMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectMapperRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectMapperRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectMapper.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectMapper>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectMapper>` and uses Records.ObjectMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectMapperRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectMapperRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectMapper.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectMapper>(sql, parameters)
    
    let insertObjectMapper (context: SqliteContext) (parameters: Parameters.NewObjectMapper) =
        context.Insert("object_mappers", parameters)
    
    /// Select a `Records.PipelineAction` from the table `pipeline_actions`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineAction>` and uses Records.PipelineAction.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineActionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineActionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineAction.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineAction>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineAction>` and uses Records.PipelineAction.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineActionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineActionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineAction.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineAction>(sql, parameters)
    
    let insertPipelineAction (context: SqliteContext) (parameters: Parameters.NewPipelineAction) =
        context.Insert("pipeline_actions", parameters)
    
    /// Select a `Records.PipelineArg` from the table `pipeline_args`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineArg>` and uses Records.PipelineArg.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineArgRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineArgRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineArg.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineArg>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineArg>` and uses Records.PipelineArg.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineArgRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineArgRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineArg.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineArg>(sql, parameters)
    
    let insertPipelineArg (context: SqliteContext) (parameters: Parameters.NewPipelineArg) =
        context.Insert("pipeline_args", parameters)
    
    /// Select a `Records.Pipeline` from the table `pipelines`.
    /// Internally this calls `context.SelectSingleAnon<Records.Pipeline>` and uses Records.Pipeline.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Pipeline.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Pipeline>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Pipeline>` and uses Records.Pipeline.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Pipeline.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Pipeline>(sql, parameters)
    
    let insertPipeline (context: SqliteContext) (parameters: Parameters.NewPipeline) =
        context.Insert("pipelines", parameters)
    
    /// Select a `Records.Queries` from the table `queries`.
    /// Internally this calls `context.SelectSingleAnon<Records.Queries>` and uses Records.Queries.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueriesRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueriesRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Queries.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Queries>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Queries>` and uses Records.Queries.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueriesRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueriesRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Queries.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Queries>(sql, parameters)
    
    let insertQueries (context: SqliteContext) (parameters: Parameters.NewQueries) =
        context.Insert("queries", parameters)
    
    /// Select a `Records.TableColumn` from the table `table_columns`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableColumn>` and uses Records.TableColumn.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableColumnRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableColumnRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableColumn.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableColumn>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableColumn>` and uses Records.TableColumn.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableColumnRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableColumnRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableColumn.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableColumn>(sql, parameters)
    
    let insertTableColumn (context: SqliteContext) (parameters: Parameters.NewTableColumn) =
        context.Insert("table_columns", parameters)
    
    /// Select a `Records.TableModel` from the table `table_models`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableModel>` and uses Records.TableModel.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModel.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableModel>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableModel>` and uses Records.TableModel.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModel.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableModel>(sql, parameters)
    
    let insertTableModel (context: SqliteContext) (parameters: Parameters.NewTableModel) =
        context.Insert("table_models", parameters)
    