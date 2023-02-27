namespace FPype.Configuration.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.Sqlite

/// Module generated on 26/02/2023 14:30:00 (utc) via Freql.Sqlite.Tools.
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
    
    /// A record representing a row in the table `object_mapper_versions`.
    type ObjectMapperVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("objectMapper")>] ObjectMapper: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("mapper")>] Mapper: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime option }
    
        static member Blank() =
            { Id = String.Empty
              ObjectMapper = String.Empty
              Version = 0
              Mapper = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = None }
    
        static member CreateTableSql() = """
        CREATE TABLE object_mapper_versions (
	id TEXT NOT NULL,
    object_mapper TEXT NOT NULL,
	version INTEGER NOT NULL,
	mapper BLOB NOT NULL,
	hash TEXT NOT NULL,
	created_on INTEGER,
	CONSTRAINT object_mapper_versions_PK PRIMARY KEY (id),
	CONSTRAINT object_mapper_versions_UN UNIQUE (object_mapper,version),
	CONSTRAINT object_mapper_versions_FK FOREIGN KEY (object_mapper) REFERENCES object_mappers(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              object_mapper_versions.`id`,
              object_mapper_versions.`object_mapper`,
              object_mapper_versions.`version`,
              object_mapper_versions.`mapper`,
              object_mapper_versions.`hash`,
              object_mapper_versions.`created_on`
        FROM object_mapper_versions
        """
    
        static member TableName() = "object_mapper_versions"
    
    /// A record representing a row in the table `object_mappers`.
    type ObjectMapper =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE object_mappers (
	name TEXT NOT NULL,
	CONSTRAINT object_mappers_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              object_mappers.`name`
        FROM object_mappers
        """
    
        static member TableName() = "object_mappers"
    
    /// A record representing a row in the table `object_table_mapper`.
    type ObjectTableMapper =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE object_table_mapper (
	name TEXT NOT NULL,
	CONSTRAINT object_table_mapper_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              object_table_mapper.`name`
        FROM object_table_mapper
        """
    
        static member TableName() = "object_table_mapper"
    
    /// A record representing a row in the table `object_table_mapper_version`.
    type ObjectTableMapperVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("objectTableMapper")>] ObjectTableMapper: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("tableModelVersionId")>] TableModelVersionId: string
          [<JsonPropertyName("mapper")>] Mapper: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              ObjectTableMapper = String.Empty
              Version = 0
              TableModelVersionId = String.Empty
              Mapper = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE object_table_mapper_version (
	id TEXT NOT NULL,
	object_table_mapper TEXT NOT NULL,
	version INTEGER NOT NULL,
	table_model_version_id TEXT NOT NULL,
	mapper BLOB NOT NULL,
	hash TEXT NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT object_table_mapper_version_PK PRIMARY KEY (id),
	CONSTRAINT object_table_mapper_version_UN UNIQUE (object_table_mapper,version),
	CONSTRAINT object_table_mapper_version_FK FOREIGN KEY (object_table_mapper) REFERENCES object_table_mapper(name),
	CONSTRAINT object_table_mapper_version_FK_2 FOREIGN KEY (table_model_version_id) REFERENCES table_model_versions(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              object_table_mapper_version.`id`,
              object_table_mapper_version.`object_table_mapper`,
              object_table_mapper_version.`version`,
              object_table_mapper_version.`table_model_version_id`,
              object_table_mapper_version.`mapper`,
              object_table_mapper_version.`hash`,
              object_table_mapper_version.`created_on`
        FROM object_table_mapper_version
        """
    
        static member TableName() = "object_table_mapper_version"
    
    /// A record representing a row in the table `pipeline_actions`.
    type PipelineAction =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionType")>] ActionType: string
          [<JsonPropertyName("actionBlob")>] ActionBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("step")>] Step: int64 }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              Name = String.Empty
              ActionType = String.Empty
              ActionBlob = BlobField.Empty()
              Hash = String.Empty
              Step = 0L }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_actions (
    id TEXT NOT NULL,
    pipeline_version_id TEXT NOT NULL,
	name TEXT NOT NULL,
	action_type TEXT NOT NULL,
	action_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	step INTEGER NOT NULL,
	CONSTRAINT pipeline_actions_PK PRIMARY KEY (id),
	CONSTRAINT pipeline_actions_UN UNIQUE (pipeline_version_id,step),
	CONSTRAINT pipeline_actions_FK FOREIGN KEY (pipeline_version_id) REFERENCES pipeline_versions(id),
	CONSTRAINT pipeline_actions_FK_1 FOREIGN KEY (action_type) REFERENCES action_types(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_actions.`id`,
              pipeline_actions.`pipeline_version_id`,
              pipeline_actions.`name`,
              pipeline_actions.`action_type`,
              pipeline_actions.`action_blob`,
              pipeline_actions.`hash`,
              pipeline_actions.`step`
        FROM pipeline_actions
        """
    
        static member TableName() = "pipeline_actions"
    
    /// A record representing a row in the table `pipeline_args`.
    type PipelineArg =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: int64
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              Name = String.Empty
              Required = 0L
              DefaultValue = None }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_args (
	id TEXT NOT NULL,
	pipeline_version_id TEXT NOT NULL,
	name TEXT NOT NULL,
	required INTEGER NOT NULL,
	default_value TEXT,
	CONSTRAINT pipeline_args_FK FOREIGN KEY (pipeline_version_id) REFERENCES pipeline_versions(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_args.`id`,
              pipeline_args.`pipeline_version_id`,
              pipeline_args.`name`,
              pipeline_args.`required`,
              pipeline_args.`default_value`
        FROM pipeline_args
        """
    
        static member TableName() = "pipeline_args"
    
    /// A record representing a row in the table `pipeline_resources`.
    type PipelineResource =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("resourceVersionId")>] ResourceVersionId: string }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              ResourceVersionId = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_resources (
	id TEXT NOT NULL,
	pipeline_version_id TEXT NOT NULL,
	resource_version_id TEXT NOT NULL,
	CONSTRAINT pipeline_resources_PK PRIMARY KEY (id),
	CONSTRAINT pipeline_resources_UN UNIQUE (pipeline_version_id,resource_version_id),
	CONSTRAINT pipeline_resources_FK FOREIGN KEY (pipeline_version_id) REFERENCES pipeline_versions(id),
	CONSTRAINT pipeline_resources_FK_1 FOREIGN KEY (resource_version_id) REFERENCES resource_versions(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_resources.`id`,
              pipeline_resources.`pipeline_version_id`,
              pipeline_resources.`resource_version_id`
        FROM pipeline_resources
        """
    
        static member TableName() = "pipeline_resources"
    
    /// A record representing a row in the table `pipeline_versions`.
    type PipelineVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipeline")>] Pipeline: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              Pipeline = String.Empty
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE pipeline_versions (
	id TEXT NOT NULL,
	pipeline TEXT NOT NULL,
	version INTEGER NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT pipeline_versions_PK PRIMARY KEY (id),
	CONSTRAINT pipeline_versions_UN UNIQUE (pipeline,version),
	CONSTRAINT pipeline_versions_FK FOREIGN KEY (pipeline) REFERENCES pipelines(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_versions.`id`,
              pipeline_versions.`pipeline`,
              pipeline_versions.`version`,
              pipeline_versions.`created_on`
        FROM pipeline_versions
        """
    
        static member TableName() = "pipeline_versions"
    
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
    type Query =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE queries (
	name TEXT NOT NULL,
	CONSTRAINT queries_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              queries.`name`
        FROM queries
        """
    
        static member TableName() = "queries"
    
    /// A record representing a row in the table `query_versions`.
    type QueryVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("queryName")>] QueryName: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("queryBlob")>] QueryBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              QueryName = String.Empty
              Version = 0
              QueryBlob = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE query_versions (
	id TEXT NOT NULL,
    query_name TEXT NOT NULL,
	version INTEGER NOT NULL,
	query_blob BLOB NOT NULL,
	hash TEXT NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT query_versions_PK PRIMARY KEY (id),
	CONSTRAINT query_versions_UN UNIQUE (query_name,version),
	CONSTRAINT query_versions_FK FOREIGN KEY (query_name) REFERENCES queries(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              query_versions.`id`,
              query_versions.`query_name`,
              query_versions.`version`,
              query_versions.`query_blob`,
              query_versions.`hash`,
              query_versions.`created_on`
        FROM query_versions
        """
    
        static member TableName() = "query_versions"
    
    /// A record representing a row in the table `resource_versions`.
    type ResourceVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("resource")>] Resource: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("rawBlob")>] RawBlob: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              Resource = String.Empty
              Version = 0
              RawBlob = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE resource_versions (
	id TEXT NOT NULL,
	resource TEXT NOT NULL,
	version INTEGER NOT NULL,
	raw_blob TEXT NOT NULL,
	hash TEXT NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT resource_versions_PK PRIMARY KEY (id),
	CONSTRAINT resource_versions_UN UNIQUE (resource,version),
	CONSTRAINT resource_versions_FK FOREIGN KEY (resource) REFERENCES resources(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              resource_versions.`id`,
              resource_versions.`resource`,
              resource_versions.`version`,
              resource_versions.`raw_blob`,
              resource_versions.`hash`,
              resource_versions.`created_on`
        FROM resource_versions
        """
    
        static member TableName() = "resource_versions"
    
    /// A record representing a row in the table `resources`.
    type Resource =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("type")>] Type: string }
    
        static member Blank() =
            { Name = String.Empty
              Type = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE resources (
	name TEXT NOT NULL,
	"type" TEXT NOT NULL,
	CONSTRAINT resources_PK PRIMARY KEY (name)
)
        """
    
        static member SelectSql() = """
        SELECT
              resources.`name`,
              resources.`type`
        FROM resources
        """
    
        static member TableName() = "resources"
    
    /// A record representing a row in the table `table_columns`.
    type TableColumn =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("tableVersionId")>] TableVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandler")>] ImportHandler: BlobField option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { Id = String.Empty
              TableVersionId = String.Empty
              Name = String.Empty
              DataType = String.Empty
              Optional = true
              ImportHandler = None
              ColumnIndex = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE table_columns (
    id TEXT NOT NULL,
    table_version_id TEXT NOT NULL,
	name TEXT NOT NULL,
	data_type TEXT NOT NULL,
	optional INTEGER NOT NULL,
	import_handler BLOB,
	column_index INTEGER NOT NULL,
	CONSTRAINT table_columns_PK PRIMARY KEY (id),
	CONSTRAINT table_columns_UN UNIQUE (table_version_id,column_index),
	CONSTRAINT table_columns_FK FOREIGN KEY (table_version_id) REFERENCES table_model_versions(id)
)
        """
    
        static member SelectSql() = """
        SELECT
              table_columns.`id`,
              table_columns.`table_version_id`,
              table_columns.`name`,
              table_columns.`data_type`,
              table_columns.`optional`,
              table_columns.`import_handler`,
              table_columns.`column_index`
        FROM table_columns
        """
    
        static member TableName() = "table_columns"
    
    /// A record representing a row in the table `table_model_versions`.
    type TableModelVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("tableModel")>] TableModel: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              TableModel = String.Empty
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE table_model_versions (
	id TEXT NOT NULL,
	table_model TEXT NOT NULL,
	version INTEGER NOT NULL,
	created_on TEXT NOT NULL,
	CONSTRAINT table_model_versions_PK PRIMARY KEY (id),
	CONSTRAINT table_model_versions_UN UNIQUE (table_model,version),
	CONSTRAINT table_model_versions_FK FOREIGN KEY (table_model) REFERENCES table_models(name)
)
        """
    
        static member SelectSql() = """
        SELECT
              table_model_versions.`id`,
              table_model_versions.`table_model`,
              table_model_versions.`version`,
              table_model_versions.`created_on`
        FROM table_model_versions
        """
    
        static member TableName() = "table_model_versions"
    
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
    

/// Module generated on 26/02/2023 14:30:00 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `action_types`.
    type NewActionType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `object_mapper_versions`.
    type NewObjectMapperVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("objectMapper")>] ObjectMapper: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("mapper")>] Mapper: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime option }
    
        static member Blank() =
            { Id = String.Empty
              ObjectMapper = String.Empty
              Version = 0
              Mapper = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = None }
    
    
    /// A record representing a new row in the table `object_mappers`.
    type NewObjectMapper =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `object_table_mapper`.
    type NewObjectTableMapper =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `object_table_mapper_version`.
    type NewObjectTableMapperVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("objectTableMapper")>] ObjectTableMapper: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("tableModelVersionId")>] TableModelVersionId: string
          [<JsonPropertyName("mapper")>] Mapper: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              ObjectTableMapper = String.Empty
              Version = 0
              TableModelVersionId = String.Empty
              Mapper = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `pipeline_actions`.
    type NewPipelineAction =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionType")>] ActionType: string
          [<JsonPropertyName("actionBlob")>] ActionBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("step")>] Step: int64 }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              Name = String.Empty
              ActionType = String.Empty
              ActionBlob = BlobField.Empty()
              Hash = String.Empty
              Step = 0L }
    
    
    /// A record representing a new row in the table `pipeline_args`.
    type NewPipelineArg =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: int64
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              Name = String.Empty
              Required = 0L
              DefaultValue = None }
    
    
    /// A record representing a new row in the table `pipeline_resources`.
    type NewPipelineResource =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: string
          [<JsonPropertyName("resourceVersionId")>] ResourceVersionId: string }
    
        static member Blank() =
            { Id = String.Empty
              PipelineVersionId = String.Empty
              ResourceVersionId = String.Empty }
    
    
    /// A record representing a new row in the table `pipeline_versions`.
    type NewPipelineVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("pipeline")>] Pipeline: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              Pipeline = String.Empty
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
    
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
    type NewQuery =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `query_versions`.
    type NewQueryVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("queryName")>] QueryName: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("queryBlob")>] QueryBlob: BlobField
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              QueryName = String.Empty
              Version = 0
              QueryBlob = BlobField.Empty()
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `resource_versions`.
    type NewResourceVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("resource")>] Resource: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("rawBlob")>] RawBlob: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              Resource = String.Empty
              Version = 0
              RawBlob = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `resources`.
    type NewResource =
        { [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("type")>] Type: string }
    
        static member Blank() =
            { Name = String.Empty
              Type = String.Empty }
    
    
    /// A record representing a new row in the table `table_columns`.
    type NewTableColumn =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("tableVersionId")>] TableVersionId: string
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandler")>] ImportHandler: BlobField option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { Id = String.Empty
              TableVersionId = String.Empty
              Name = String.Empty
              DataType = String.Empty
              Optional = true
              ImportHandler = None
              ColumnIndex = 0 }
    
    
    /// A record representing a new row in the table `table_model_versions`.
    type NewTableModelVersion =
        { [<JsonPropertyName("id")>] Id: string
          [<JsonPropertyName("tableModel")>] TableModel: string
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = String.Empty
              TableModel = String.Empty
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `table_models`.
    type NewTableModel =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
/// Module generated on 26/02/2023 14:30:00 (utc) via Freql.Tools.
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
    
    /// Select a `Records.ObjectMapperVersion` from the table `object_mapper_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectMapperVersion>` and uses Records.ObjectMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectMapperVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectMapperVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectMapperVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectMapperVersion>` and uses Records.ObjectMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectMapperVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectMapperVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectMapperVersion>(sql, parameters)
    
    let insertObjectMapperVersion (context: SqliteContext) (parameters: Parameters.NewObjectMapperVersion) =
        context.Insert("object_mapper_versions", parameters)
    
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
    
    /// Select a `Records.ObjectTableMapper` from the table `object_table_mapper`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectTableMapper>` and uses Records.ObjectTableMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapper.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectTableMapper>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectTableMapper>` and uses Records.ObjectTableMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapper.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectTableMapper>(sql, parameters)
    
    let insertObjectTableMapper (context: SqliteContext) (parameters: Parameters.NewObjectTableMapper) =
        context.Insert("object_table_mapper", parameters)
    
    /// Select a `Records.ObjectTableMapperVersion` from the table `object_table_mapper_version`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectTableMapperVersion>` and uses Records.ObjectTableMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectTableMapperVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectTableMapperVersion>` and uses Records.ObjectTableMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectTableMapperVersion>(sql, parameters)
    
    let insertObjectTableMapperVersion (context: SqliteContext) (parameters: Parameters.NewObjectTableMapperVersion) =
        context.Insert("object_table_mapper_version", parameters)
    
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
    
    /// Select a `Records.PipelineResource` from the table `pipeline_resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineResource>` and uses Records.PipelineResource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineResourceRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineResourceRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineResource.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineResource>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineResource>` and uses Records.PipelineResource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineResourceRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineResourceRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineResource.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineResource>(sql, parameters)
    
    let insertPipelineResource (context: SqliteContext) (parameters: Parameters.NewPipelineResource) =
        context.Insert("pipeline_resources", parameters)
    
    /// Select a `Records.PipelineVersion` from the table `pipeline_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineVersion>` and uses Records.PipelineVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineVersion>` and uses Records.PipelineVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineVersion>(sql, parameters)
    
    let insertPipelineVersion (context: SqliteContext) (parameters: Parameters.NewPipelineVersion) =
        context.Insert("pipeline_versions", parameters)
    
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
    
    /// Select a `Records.Query` from the table `queries`.
    /// Internally this calls `context.SelectSingleAnon<Records.Query>` and uses Records.Query.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Query.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Query>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Query>` and uses Records.Query.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Query.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Query>(sql, parameters)
    
    let insertQuery (context: SqliteContext) (parameters: Parameters.NewQuery) =
        context.Insert("queries", parameters)
    
    /// Select a `Records.QueryVersion` from the table `query_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.QueryVersion>` and uses Records.QueryVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.QueryVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.QueryVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.QueryVersion>` and uses Records.QueryVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.QueryVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.QueryVersion>(sql, parameters)
    
    let insertQueryVersion (context: SqliteContext) (parameters: Parameters.NewQueryVersion) =
        context.Insert("query_versions", parameters)
    
    /// Select a `Records.ResourceVersion` from the table `resource_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.ResourceVersion>` and uses Records.ResourceVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ResourceVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ResourceVersion>` and uses Records.ResourceVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ResourceVersion>(sql, parameters)
    
    let insertResourceVersion (context: SqliteContext) (parameters: Parameters.NewResourceVersion) =
        context.Insert("resource_versions", parameters)
    
    /// Select a `Records.Resource` from the table `resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Resource>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Resource>(sql, parameters)
    
    let insertResource (context: SqliteContext) (parameters: Parameters.NewResource) =
        context.Insert("resources", parameters)
    
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
    
    /// Select a `Records.TableModelVersion` from the table `table_model_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableModelVersion>` and uses Records.TableModelVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelVersionRecord (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModelVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableModelVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableModelVersion>` and uses Records.TableModelVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelVersionRecords (context: SqliteContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModelVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableModelVersion>(sql, parameters)
    
    let insertTableModelVersion (context: SqliteContext) (parameters: Parameters.NewTableModelVersion) =
        context.Insert("table_model_versions", parameters)
    
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
    