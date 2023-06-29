namespace FPype.Infrastructure.Core.Persistence

open System
open System.Text.Json.Serialization
open Freql.Core.Common
open Freql.MySql

/// Module generated on 29/06/2023 18:25:09 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Records =
    /// A record representing a row in the table `cfg_action_types`.
    type ActionType =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_action_types` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_action_types_UN` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_action_types.`id`,
              cfg_action_types.`name`
        FROM cfg_action_types
        """
    
        static member TableName() = "cfg_action_types"
    
    /// A record representing a row in the table `cfg_events`.
    type ConfigurationEvent =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("eventType")>] EventType: string
          [<JsonPropertyName("eventTimestamp")>] EventTimestamp: DateTime
          [<JsonPropertyName("eventData")>] EventData: string
          [<JsonPropertyName("userId")>] UserId: int
          [<JsonPropertyName("batchReference")>] BatchReference: string }
    
        static member Blank() =
            { Id = 0
              SubscriptionId = 0
              EventType = String.Empty
              EventTimestamp = DateTime.UtcNow
              EventData = String.Empty
              UserId = 0
              BatchReference = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_events` (
  `id` int NOT NULL AUTO_INCREMENT,
  `subscription_id` int NOT NULL,
  `event_type` varchar(50) NOT NULL,
  `event_timestamp` datetime NOT NULL,
  `event_data` text NOT NULL,
  `user_id` int NOT NULL,
  `batch_reference` varchar(36) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `cfg_events_FK` (`subscription_id`),
  KEY `cfg_events_FK_1` (`user_id`),
  CONSTRAINT `cfg_events_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`),
  CONSTRAINT `cfg_events_FK_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=27 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_events.`id`,
              cfg_events.`subscription_id`,
              cfg_events.`event_type`,
              cfg_events.`event_timestamp`,
              cfg_events.`event_data`,
              cfg_events.`user_id`,
              cfg_events.`batch_reference`
        FROM cfg_events
        """
    
        static member TableName() = "cfg_events"
    
    /// A record representing a row in the table `cfg_object_table_mapper_versions`.
    type ObjectTableMapperVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("objectTableMapperId")>] ObjectTableMapperId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("tableModelVersionId")>] TableModelVersionId: int
          [<JsonPropertyName("mapperJson")>] MapperJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              ObjectTableMapperId = 0
              Version = 0
              TableModelVersionId = 0
              MapperJson = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_object_table_mapper_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `object_table_mapper_id` int NOT NULL,
  `version` int NOT NULL,
  `table_model_version_id` int NOT NULL,
  `mapper_json` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `hash` varchar(100) NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_object_table_mapper_versions_UN` (`reference`),
  UNIQUE KEY `cfg_object_table_mapper_versions_UN_1` (`object_table_mapper_id`,`version`),
  KEY `cfg_object_table_mapper_versions_FK_1` (`table_model_version_id`),
  CONSTRAINT `cfg_object_table_mapper_versions_FK` FOREIGN KEY (`object_table_mapper_id`) REFERENCES `cfg_object_table_mappers` (`id`),
  CONSTRAINT `cfg_object_table_mapper_versions_FK_1` FOREIGN KEY (`table_model_version_id`) REFERENCES `cfg_table_model_versions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_object_table_mapper_versions.`id`,
              cfg_object_table_mapper_versions.`reference`,
              cfg_object_table_mapper_versions.`object_table_mapper_id`,
              cfg_object_table_mapper_versions.`version`,
              cfg_object_table_mapper_versions.`table_model_version_id`,
              cfg_object_table_mapper_versions.`mapper_json`,
              cfg_object_table_mapper_versions.`hash`,
              cfg_object_table_mapper_versions.`created_on`
        FROM cfg_object_table_mapper_versions
        """
    
        static member TableName() = "cfg_object_table_mapper_versions"
    
    /// A record representing a row in the table `cfg_object_table_mappers`.
    type ObjectTableMapper =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_object_table_mappers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_object_table_mappers_UN` (`reference`),
  UNIQUE KEY `cfg_object_table_mappers_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_object_table_mappers_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_object_table_mappers.`id`,
              cfg_object_table_mappers.`reference`,
              cfg_object_table_mappers.`subscription_id`,
              cfg_object_table_mappers.`name`
        FROM cfg_object_table_mappers
        """
    
        static member TableName() = "cfg_object_table_mappers"
    
    /// A record representing a row in the table `cfg_pipeline_actions`.
    type PipelineAction =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionTypeId")>] ActionTypeId: int
          [<JsonPropertyName("actionJson")>] ActionJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("step")>] Step: int }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              PipelineVersionId = 0
              Name = String.Empty
              ActionTypeId = 0
              ActionJson = String.Empty
              Hash = String.Empty
              Step = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_pipeline_actions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `pipeline_version_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `action_type_id` int NOT NULL,
  `action_json` mediumtext NOT NULL,
  `hash` varchar(100) NOT NULL,
  `step` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_pipeline_actions_UN` (`reference`),
  UNIQUE KEY `cfg_pipeline_actions_UN_1` (`pipeline_version_id`,`name`),
  UNIQUE KEY `cfg_pipeline_actions_UN_2` (`pipeline_version_id`,`step`),
  KEY `cfg_pipeline_actions_FK_1` (`action_type_id`),
  CONSTRAINT `cfg_pipeline_actions_FK` FOREIGN KEY (`pipeline_version_id`) REFERENCES `cfg_pipeline_versions` (`id`),
  CONSTRAINT `cfg_pipeline_actions_FK_1` FOREIGN KEY (`action_type_id`) REFERENCES `cfg_pipeline_versions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_pipeline_actions.`id`,
              cfg_pipeline_actions.`reference`,
              cfg_pipeline_actions.`pipeline_version_id`,
              cfg_pipeline_actions.`name`,
              cfg_pipeline_actions.`action_type_id`,
              cfg_pipeline_actions.`action_json`,
              cfg_pipeline_actions.`hash`,
              cfg_pipeline_actions.`step`
        FROM cfg_pipeline_actions
        """
    
        static member TableName() = "cfg_pipeline_actions"
    
    /// A record representing a row in the table `cfg_pipeline_args`.
    type PipelineArg =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: bool
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              PipelineVersionId = 0
              Name = String.Empty
              Required = false
              DefaultValue = None }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_pipeline_args` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `pipeline_version_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `required` tinyint(1) NOT NULL,
  `default_value` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_pipeline_args_UN` (`reference`),
  UNIQUE KEY `cfg_pipeline_args_UN_1` (`pipeline_version_id`,`name`),
  CONSTRAINT `cfg_pipeline_args_FK` FOREIGN KEY (`pipeline_version_id`) REFERENCES `cfg_pipeline_versions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_pipeline_args.`id`,
              cfg_pipeline_args.`reference`,
              cfg_pipeline_args.`pipeline_version_id`,
              cfg_pipeline_args.`name`,
              cfg_pipeline_args.`required`,
              cfg_pipeline_args.`default_value`
        FROM cfg_pipeline_args
        """
    
        static member TableName() = "cfg_pipeline_args"
    
    /// A record representing a row in the table `cfg_pipeline_resources`.
    type PipelineResource =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("resourceVersionId")>] ResourceVersionId: int }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              PipelineVersionId = 0
              ResourceVersionId = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_pipeline_resources` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `pipeline_version_id` int NOT NULL,
  `resource_version_id` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_pipeline_resources_UN` (`reference`),
  UNIQUE KEY `cfg_pipeline_resources_UN_1` (`pipeline_version_id`,`resource_version_id`),
  KEY `cfg_pipeline_resources_FK_1` (`resource_version_id`),
  CONSTRAINT `cfg_pipeline_resources_FK` FOREIGN KEY (`pipeline_version_id`) REFERENCES `cfg_pipeline_versions` (`id`),
  CONSTRAINT `cfg_pipeline_resources_FK_1` FOREIGN KEY (`resource_version_id`) REFERENCES `cfg_resource_versions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_pipeline_resources.`id`,
              cfg_pipeline_resources.`reference`,
              cfg_pipeline_resources.`pipeline_version_id`,
              cfg_pipeline_resources.`resource_version_id`
        FROM cfg_pipeline_resources
        """
    
        static member TableName() = "cfg_pipeline_resources"
    
    /// A record representing a row in the table `cfg_pipeline_versions`.
    type PipelineVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineId")>] PipelineId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("description")>] Description: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              PipelineId = 0
              Version = 0
              Description = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_pipeline_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `pipeline_id` int NOT NULL,
  `version` int NOT NULL,
  `description` varchar(500) NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_pipeline_versions_UN` (`reference`),
  UNIQUE KEY `cfg_pipeline_versions_UN_1` (`pipeline_id`,`version`),
  CONSTRAINT `cfg_pipeline_versions_FK` FOREIGN KEY (`pipeline_id`) REFERENCES `cfg_pipelines` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_pipeline_versions.`id`,
              cfg_pipeline_versions.`reference`,
              cfg_pipeline_versions.`pipeline_id`,
              cfg_pipeline_versions.`version`,
              cfg_pipeline_versions.`description`,
              cfg_pipeline_versions.`created_on`
        FROM cfg_pipeline_versions
        """
    
        static member TableName() = "cfg_pipeline_versions"
    
    /// A record representing a row in the table `cfg_pipelines`.
    type Pipeline =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_pipelines` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_pipelines_UN` (`reference`),
  UNIQUE KEY `cfg_pipelines_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_pipelines_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_pipelines.`id`,
              cfg_pipelines.`reference`,
              cfg_pipelines.`subscription_id`,
              cfg_pipelines.`name`
        FROM cfg_pipelines
        """
    
        static member TableName() = "cfg_pipelines"
    
    /// A record representing a row in the table `cfg_queries`.
    type Query =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_queries` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_queries_UN` (`reference`),
  UNIQUE KEY `cfg_queries_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_queries_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_queries.`id`,
              cfg_queries.`reference`,
              cfg_queries.`subscription_id`,
              cfg_queries.`name`
        FROM cfg_queries
        """
    
        static member TableName() = "cfg_queries"
    
    /// A record representing a row in the table `cfg_query_versions`.
    type QueryVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("queryId")>] QueryId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("rawQuery")>] RawQuery: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("isSerialized")>] IsSerialized: bool
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              QueryId = 0
              Version = 0
              RawQuery = String.Empty
              Hash = String.Empty
              IsSerialized = false
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_query_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `query_id` int NOT NULL,
  `version` int NOT NULL,
  `raw_query` text NOT NULL,
  `hash` varchar(100) NOT NULL,
  `is_serialized` tinyint(1) NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_query_versions_UN` (`reference`),
  UNIQUE KEY `cfg_query_versions_UN_1` (`query_id`,`version`),
  CONSTRAINT `cfg_query_versions_FK` FOREIGN KEY (`query_id`) REFERENCES `cfg_queries` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_query_versions.`id`,
              cfg_query_versions.`reference`,
              cfg_query_versions.`query_id`,
              cfg_query_versions.`version`,
              cfg_query_versions.`raw_query`,
              cfg_query_versions.`hash`,
              cfg_query_versions.`is_serialized`,
              cfg_query_versions.`created_on`
        FROM cfg_query_versions
        """
    
        static member TableName() = "cfg_query_versions"
    
    /// A record representing a row in the table `cfg_resource_versions`.
    type ResourceVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("resourceId")>] ResourceId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("resourceType")>] ResourceType: string
          [<JsonPropertyName("resourcePath")>] ResourcePath: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              ResourceId = 0
              Version = 0
              ResourceType = String.Empty
              ResourcePath = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_resource_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `resource_id` int NOT NULL,
  `version` int NOT NULL,
  `resource_type` varchar(100) NOT NULL,
  `resource_path` varchar(200) NOT NULL,
  `hash` varchar(100) NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_resource_versions_UN` (`reference`),
  UNIQUE KEY `cfg_resource_versions_UN_1` (`resource_id`,`version`),
  CONSTRAINT `cfg_resource_versions_FK` FOREIGN KEY (`resource_id`) REFERENCES `cfg_resources` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_resource_versions.`id`,
              cfg_resource_versions.`reference`,
              cfg_resource_versions.`resource_id`,
              cfg_resource_versions.`version`,
              cfg_resource_versions.`resource_type`,
              cfg_resource_versions.`resource_path`,
              cfg_resource_versions.`hash`,
              cfg_resource_versions.`created_on`
        FROM cfg_resource_versions
        """
    
        static member TableName() = "cfg_resource_versions"
    
    /// A record representing a row in the table `cfg_resources`.
    type Resource =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_resources` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_resources_UN` (`reference`),
  UNIQUE KEY `cfg_resources_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_resources_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_resources.`id`,
              cfg_resources.`reference`,
              cfg_resources.`subscription_id`,
              cfg_resources.`name`
        FROM cfg_resources
        """
    
        static member TableName() = "cfg_resources"
    
    /// A record representing a row in the table `cfg_table_columns`.
    type TableColumn =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableVersionId")>] TableVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandlerJson")>] ImportHandlerJson: string option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              TableVersionId = 0
              Name = String.Empty
              DataType = String.Empty
              Optional = false
              ImportHandlerJson = None
              ColumnIndex = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_table_columns` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `table_version_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  `data_type` varchar(100) NOT NULL,
  `optional` tinyint(1) NOT NULL,
  `import_handler_json` text,
  `column_index` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_table_columns_UN` (`reference`),
  UNIQUE KEY `cfg_table_columns_UN_1` (`table_version_id`,`name`),
  UNIQUE KEY `cfg_table_columns_UN_2` (`table_version_id`,`column_index`),
  CONSTRAINT `cfg_table_columns_FK` FOREIGN KEY (`table_version_id`) REFERENCES `cfg_table_model_versions` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_table_columns.`id`,
              cfg_table_columns.`reference`,
              cfg_table_columns.`table_version_id`,
              cfg_table_columns.`name`,
              cfg_table_columns.`data_type`,
              cfg_table_columns.`optional`,
              cfg_table_columns.`import_handler_json`,
              cfg_table_columns.`column_index`
        FROM cfg_table_columns
        """
    
        static member TableName() = "cfg_table_columns"
    
    /// A record representing a row in the table `cfg_table_model_versions`.
    type TableModelVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableModelId")>] TableModelId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              TableModelId = 0
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_table_model_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `table_model_id` int NOT NULL,
  `version` int NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_table_model_versions_UN` (`reference`),
  UNIQUE KEY `cfg_table_model_versions_UN_1` (`table_model_id`,`version`),
  CONSTRAINT `cfg_table_model_versions_FK` FOREIGN KEY (`table_model_id`) REFERENCES `cfg_table_models` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_table_model_versions.`id`,
              cfg_table_model_versions.`reference`,
              cfg_table_model_versions.`table_model_id`,
              cfg_table_model_versions.`version`,
              cfg_table_model_versions.`created_on`
        FROM cfg_table_model_versions
        """
    
        static member TableName() = "cfg_table_model_versions"
    
    /// A record representing a row in the table `cfg_table_models`.
    type TableModel =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_table_models` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_table_models_UN` (`reference`),
  UNIQUE KEY `cfg_table_models_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_table_models_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_table_models.`id`,
              cfg_table_models.`reference`,
              cfg_table_models.`subscription_id`,
              cfg_table_models.`name`
        FROM cfg_table_models
        """
    
        static member TableName() = "cfg_table_models"
    
    /// A record representing a row in the table `cfg_table_object_mapper_versions`.
    type TableObjectMapperVersion =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableObjectMapperId")>] TableObjectMapperId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("mapperJson")>] MapperJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              TableObjectMapperId = 0
              Version = 0
              MapperJson = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_table_object_mapper_versions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `table_object_mapper_id` int NOT NULL,
  `version` int NOT NULL,
  `mapper_json` mediumtext NOT NULL,
  `hash` varchar(100) NOT NULL,
  `created_on` datetime NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_table_object_mapper_versions_UN` (`reference`),
  UNIQUE KEY `cfg_table_object_mapper_versions_UN_1` (`table_object_mapper_id`,`version`),
  CONSTRAINT `cfg_table_object_mapper_versions_FK` FOREIGN KEY (`table_object_mapper_id`) REFERENCES `cfg_table_object_mappers` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_table_object_mapper_versions.`id`,
              cfg_table_object_mapper_versions.`reference`,
              cfg_table_object_mapper_versions.`table_object_mapper_id`,
              cfg_table_object_mapper_versions.`version`,
              cfg_table_object_mapper_versions.`mapper_json`,
              cfg_table_object_mapper_versions.`hash`,
              cfg_table_object_mapper_versions.`created_on`
        FROM cfg_table_object_mapper_versions
        """
    
        static member TableName() = "cfg_table_object_mapper_versions"
    
    /// A record representing a row in the table `cfg_table_object_mappers`.
    type TableObjectMapper =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
        static member CreateTableSql() = """
        CREATE TABLE `cfg_table_object_mappers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `cfg_table_object_mappers_UN` (`reference`),
  UNIQUE KEY `cfg_table_object_mappers_UN_1` (`subscription_id`,`name`),
  CONSTRAINT `cfg_table_object_mappers_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              cfg_table_object_mappers.`id`,
              cfg_table_object_mappers.`reference`,
              cfg_table_object_mappers.`subscription_id`,
              cfg_table_object_mappers.`name`
        FROM cfg_table_object_mappers
        """
    
        static member TableName() = "cfg_table_object_mappers"
    
    /// A record representing a row in the table `pipeline_runs`.
    type PipelineRunItem =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("queuedOn")>] QueuedOn: DateTime
          [<JsonPropertyName("startedOn")>] StartedOn: DateTime option
          [<JsonPropertyName("completedOn")>] CompletedOn: DateTime option
          [<JsonPropertyName("wasSuccessful")>] WasSuccessful: bool option
          [<JsonPropertyName("basePath")>] BasePath: string
          [<JsonPropertyName("runBy")>] RunBy: int }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              PipelineVersionId = 0
              QueuedOn = DateTime.UtcNow
              StartedOn = None
              CompletedOn = None
              WasSuccessful = None
              BasePath = String.Empty
              RunBy = 0 }
    
        static member CreateTableSql() = """
        CREATE TABLE `pipeline_runs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `subscription_id` int NOT NULL,
  `pipeline_version_id` int NOT NULL,
  `queued_on` datetime NOT NULL,
  `started_on` datetime DEFAULT NULL,
  `completed_on` datetime DEFAULT NULL,
  `was_successful` tinyint(1) DEFAULT NULL,
  `base_path` varchar(500) NOT NULL,
  `run_by` int NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `pipeline_runs_UN` (`reference`),
  KEY `pipeline_runs_FK` (`subscription_id`),
  KEY `pipeline_runs_FK_1` (`pipeline_version_id`),
  KEY `pipeline_runs_FK_2` (`run_by`),
  CONSTRAINT `pipeline_runs_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`),
  CONSTRAINT `pipeline_runs_FK_1` FOREIGN KEY (`pipeline_version_id`) REFERENCES `cfg_pipeline_versions` (`id`),
  CONSTRAINT `pipeline_runs_FK_2` FOREIGN KEY (`run_by`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              pipeline_runs.`id`,
              pipeline_runs.`reference`,
              pipeline_runs.`subscription_id`,
              pipeline_runs.`pipeline_version_id`,
              pipeline_runs.`queued_on`,
              pipeline_runs.`started_on`,
              pipeline_runs.`completed_on`,
              pipeline_runs.`was_successful`,
              pipeline_runs.`base_path`,
              pipeline_runs.`run_by`
        FROM pipeline_runs
        """
    
        static member TableName() = "pipeline_runs"
    
    /// A record representing a row in the table `subscriptions`.
    type Subscription =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("active")>] Active: bool }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              Active = false }
    
        static member CreateTableSql() = """
        CREATE TABLE `subscriptions` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(36) NOT NULL,
  `active` tinyint(1) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `subscriptions_UN` (`reference`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              subscriptions.`id`,
              subscriptions.`reference`,
              subscriptions.`active`
        FROM subscriptions
        """
    
        static member TableName() = "subscriptions"
    
    /// A record representing a row in the table `users`.
    type User =
        { [<JsonPropertyName("id")>] Id: int
          [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("username")>] Username: string
          [<JsonPropertyName("active")>] Active: bool }
    
        static member Blank() =
            { Id = 0
              Reference = String.Empty
              SubscriptionId = 0
              Username = String.Empty
              Active = false }
    
        static member CreateTableSql() = """
        CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `reference` varchar(100) NOT NULL,
  `subscription_id` int NOT NULL,
  `username` varchar(100) NOT NULL,
  `active` tinyint(1) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `users_UN` (`reference`),
  UNIQUE KEY `users_UN_1` (`subscription_id`,`username`),
  CONSTRAINT `users_FK` FOREIGN KEY (`subscription_id`) REFERENCES `subscriptions` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
        """
    
        static member SelectSql() = """
        SELECT
              users.`id`,
              users.`reference`,
              users.`subscription_id`,
              users.`username`,
              users.`active`
        FROM users
        """
    
        static member TableName() = "users"
    

/// Module generated on 29/06/2023 18:25:09 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Parameters =
    /// A record representing a new row in the table `cfg_action_types`.
    type NewActionType =
        { [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_events`.
    type NewConfigurationEvent =
        { [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("eventType")>] EventType: string
          [<JsonPropertyName("eventTimestamp")>] EventTimestamp: DateTime
          [<JsonPropertyName("eventData")>] EventData: string
          [<JsonPropertyName("userId")>] UserId: int
          [<JsonPropertyName("batchReference")>] BatchReference: string }
    
        static member Blank() =
            { SubscriptionId = 0
              EventType = String.Empty
              EventTimestamp = DateTime.UtcNow
              EventData = String.Empty
              UserId = 0
              BatchReference = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_object_table_mapper_versions`.
    type NewObjectTableMapperVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("objectTableMapperId")>] ObjectTableMapperId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("tableModelVersionId")>] TableModelVersionId: int
          [<JsonPropertyName("mapperJson")>] MapperJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              ObjectTableMapperId = 0
              Version = 0
              TableModelVersionId = 0
              MapperJson = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_object_table_mappers`.
    type NewObjectTableMapper =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_pipeline_actions`.
    type NewPipelineAction =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("actionTypeId")>] ActionTypeId: int
          [<JsonPropertyName("actionJson")>] ActionJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("step")>] Step: int }
    
        static member Blank() =
            { Reference = String.Empty
              PipelineVersionId = 0
              Name = String.Empty
              ActionTypeId = 0
              ActionJson = String.Empty
              Hash = String.Empty
              Step = 0 }
    
    
    /// A record representing a new row in the table `cfg_pipeline_args`.
    type NewPipelineArg =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("required")>] Required: bool
          [<JsonPropertyName("defaultValue")>] DefaultValue: string option }
    
        static member Blank() =
            { Reference = String.Empty
              PipelineVersionId = 0
              Name = String.Empty
              Required = false
              DefaultValue = None }
    
    
    /// A record representing a new row in the table `cfg_pipeline_resources`.
    type NewPipelineResource =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("resourceVersionId")>] ResourceVersionId: int }
    
        static member Blank() =
            { Reference = String.Empty
              PipelineVersionId = 0
              ResourceVersionId = 0 }
    
    
    /// A record representing a new row in the table `cfg_pipeline_versions`.
    type NewPipelineVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("pipelineId")>] PipelineId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("description")>] Description: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              PipelineId = 0
              Version = 0
              Description = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_pipelines`.
    type NewPipeline =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_queries`.
    type NewQuery =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_query_versions`.
    type NewQueryVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("queryId")>] QueryId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("rawQuery")>] RawQuery: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("isSerialized")>] IsSerialized: bool
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              QueryId = 0
              Version = 0
              RawQuery = String.Empty
              Hash = String.Empty
              IsSerialized = false
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_resource_versions`.
    type NewResourceVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("resourceId")>] ResourceId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("resourceType")>] ResourceType: string
          [<JsonPropertyName("resourcePath")>] ResourcePath: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              ResourceId = 0
              Version = 0
              ResourceType = String.Empty
              ResourcePath = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_resources`.
    type NewResource =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_table_columns`.
    type NewTableColumn =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableVersionId")>] TableVersionId: int
          [<JsonPropertyName("name")>] Name: string
          [<JsonPropertyName("dataType")>] DataType: string
          [<JsonPropertyName("optional")>] Optional: bool
          [<JsonPropertyName("importHandlerJson")>] ImportHandlerJson: string option
          [<JsonPropertyName("columnIndex")>] ColumnIndex: int }
    
        static member Blank() =
            { Reference = String.Empty
              TableVersionId = 0
              Name = String.Empty
              DataType = String.Empty
              Optional = false
              ImportHandlerJson = None
              ColumnIndex = 0 }
    
    
    /// A record representing a new row in the table `cfg_table_model_versions`.
    type NewTableModelVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableModelId")>] TableModelId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              TableModelId = 0
              Version = 0
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_table_models`.
    type NewTableModel =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `cfg_table_object_mapper_versions`.
    type NewTableObjectMapperVersion =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("tableObjectMapperId")>] TableObjectMapperId: int
          [<JsonPropertyName("version")>] Version: int
          [<JsonPropertyName("mapperJson")>] MapperJson: string
          [<JsonPropertyName("hash")>] Hash: string
          [<JsonPropertyName("createdOn")>] CreatedOn: DateTime }
    
        static member Blank() =
            { Reference = String.Empty
              TableObjectMapperId = 0
              Version = 0
              MapperJson = String.Empty
              Hash = String.Empty
              CreatedOn = DateTime.UtcNow }
    
    
    /// A record representing a new row in the table `cfg_table_object_mappers`.
    type NewTableObjectMapper =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("name")>] Name: string }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Name = String.Empty }
    
    
    /// A record representing a new row in the table `pipeline_runs`.
    type NewPipelineRunItem =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("pipelineVersionId")>] PipelineVersionId: int
          [<JsonPropertyName("queuedOn")>] QueuedOn: DateTime
          [<JsonPropertyName("startedOn")>] StartedOn: DateTime option
          [<JsonPropertyName("completedOn")>] CompletedOn: DateTime option
          [<JsonPropertyName("wasSuccessful")>] WasSuccessful: bool option
          [<JsonPropertyName("basePath")>] BasePath: string
          [<JsonPropertyName("runBy")>] RunBy: int }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              PipelineVersionId = 0
              QueuedOn = DateTime.UtcNow
              StartedOn = None
              CompletedOn = None
              WasSuccessful = None
              BasePath = String.Empty
              RunBy = 0 }
    
    
    /// A record representing a new row in the table `subscriptions`.
    type NewSubscription =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("active")>] Active: bool }
    
        static member Blank() =
            { Reference = String.Empty
              Active = false }
    
    
    /// A record representing a new row in the table `users`.
    type NewUser =
        { [<JsonPropertyName("reference")>] Reference: string
          [<JsonPropertyName("subscriptionId")>] SubscriptionId: int
          [<JsonPropertyName("username")>] Username: string
          [<JsonPropertyName("active")>] Active: bool }
    
        static member Blank() =
            { Reference = String.Empty
              SubscriptionId = 0
              Username = String.Empty
              Active = false }
    
    
/// Module generated on 29/06/2023 18:25:09 (utc) via Freql.Tools.
[<RequireQualifiedAccess>]
module Operations =

    let buildSql (lines: string list) = lines |> String.concat Environment.NewLine

    /// Select a `Records.ActionType` from the table `cfg_action_types`.
    /// Internally this calls `context.SelectSingleAnon<Records.ActionType>` and uses Records.ActionType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectActionTypeRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectActionTypeRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ActionType.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ActionType>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ActionType>` and uses Records.ActionType.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectActionTypeRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectActionTypeRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ActionType.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ActionType>(sql, parameters)
    
    let insertActionType (context: MySqlContext) (parameters: Parameters.NewActionType) =
        context.Insert("cfg_action_types", parameters)
    
    /// Select a `Records.ConfigurationEvent` from the table `cfg_events`.
    /// Internally this calls `context.SelectSingleAnon<Records.ConfigurationEvent>` and uses Records.ConfigurationEvent.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectConfigurationEventRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectConfigurationEventRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ConfigurationEvent.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ConfigurationEvent>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ConfigurationEvent>` and uses Records.ConfigurationEvent.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectConfigurationEventRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectConfigurationEventRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ConfigurationEvent.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ConfigurationEvent>(sql, parameters)
    
    let insertConfigurationEvent (context: MySqlContext) (parameters: Parameters.NewConfigurationEvent) =
        context.Insert("cfg_events", parameters)
    
    /// Select a `Records.ObjectTableMapperVersion` from the table `cfg_object_table_mapper_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectTableMapperVersion>` and uses Records.ObjectTableMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectTableMapperVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectTableMapperVersion>` and uses Records.ObjectTableMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectTableMapperVersion>(sql, parameters)
    
    let insertObjectTableMapperVersion (context: MySqlContext) (parameters: Parameters.NewObjectTableMapperVersion) =
        context.Insert("cfg_object_table_mapper_versions", parameters)
    
    /// Select a `Records.ObjectTableMapper` from the table `cfg_object_table_mappers`.
    /// Internally this calls `context.SelectSingleAnon<Records.ObjectTableMapper>` and uses Records.ObjectTableMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapper.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ObjectTableMapper>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ObjectTableMapper>` and uses Records.ObjectTableMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectObjectTableMapperRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectObjectTableMapperRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ObjectTableMapper.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ObjectTableMapper>(sql, parameters)
    
    let insertObjectTableMapper (context: MySqlContext) (parameters: Parameters.NewObjectTableMapper) =
        context.Insert("cfg_object_table_mappers", parameters)
    
    /// Select a `Records.PipelineAction` from the table `cfg_pipeline_actions`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineAction>` and uses Records.PipelineAction.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineActionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineActionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineAction.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineAction>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineAction>` and uses Records.PipelineAction.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineActionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineActionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineAction.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineAction>(sql, parameters)
    
    let insertPipelineAction (context: MySqlContext) (parameters: Parameters.NewPipelineAction) =
        context.Insert("cfg_pipeline_actions", parameters)
    
    /// Select a `Records.PipelineArg` from the table `cfg_pipeline_args`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineArg>` and uses Records.PipelineArg.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineArgRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineArgRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineArg.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineArg>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineArg>` and uses Records.PipelineArg.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineArgRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineArgRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineArg.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineArg>(sql, parameters)
    
    let insertPipelineArg (context: MySqlContext) (parameters: Parameters.NewPipelineArg) =
        context.Insert("cfg_pipeline_args", parameters)
    
    /// Select a `Records.PipelineResource` from the table `cfg_pipeline_resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineResource>` and uses Records.PipelineResource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineResourceRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineResourceRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineResource.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineResource>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineResource>` and uses Records.PipelineResource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineResourceRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineResourceRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineResource.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineResource>(sql, parameters)
    
    let insertPipelineResource (context: MySqlContext) (parameters: Parameters.NewPipelineResource) =
        context.Insert("cfg_pipeline_resources", parameters)
    
    /// Select a `Records.PipelineVersion` from the table `cfg_pipeline_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineVersion>` and uses Records.PipelineVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineVersion>` and uses Records.PipelineVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineVersion>(sql, parameters)
    
    let insertPipelineVersion (context: MySqlContext) (parameters: Parameters.NewPipelineVersion) =
        context.Insert("cfg_pipeline_versions", parameters)
    
    /// Select a `Records.Pipeline` from the table `cfg_pipelines`.
    /// Internally this calls `context.SelectSingleAnon<Records.Pipeline>` and uses Records.Pipeline.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Pipeline.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Pipeline>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Pipeline>` and uses Records.Pipeline.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Pipeline.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Pipeline>(sql, parameters)
    
    let insertPipeline (context: MySqlContext) (parameters: Parameters.NewPipeline) =
        context.Insert("cfg_pipelines", parameters)
    
    /// Select a `Records.Query` from the table `cfg_queries`.
    /// Internally this calls `context.SelectSingleAnon<Records.Query>` and uses Records.Query.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Query.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Query>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Query>` and uses Records.Query.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Query.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Query>(sql, parameters)
    
    let insertQuery (context: MySqlContext) (parameters: Parameters.NewQuery) =
        context.Insert("cfg_queries", parameters)
    
    /// Select a `Records.QueryVersion` from the table `cfg_query_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.QueryVersion>` and uses Records.QueryVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.QueryVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.QueryVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.QueryVersion>` and uses Records.QueryVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectQueryVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectQueryVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.QueryVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.QueryVersion>(sql, parameters)
    
    let insertQueryVersion (context: MySqlContext) (parameters: Parameters.NewQueryVersion) =
        context.Insert("cfg_query_versions", parameters)
    
    /// Select a `Records.ResourceVersion` from the table `cfg_resource_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.ResourceVersion>` and uses Records.ResourceVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.ResourceVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.ResourceVersion>` and uses Records.ResourceVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.ResourceVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.ResourceVersion>(sql, parameters)
    
    let insertResourceVersion (context: MySqlContext) (parameters: Parameters.NewResourceVersion) =
        context.Insert("cfg_resource_versions", parameters)
    
    /// Select a `Records.Resource` from the table `cfg_resources`.
    /// Internally this calls `context.SelectSingleAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Resource>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Resource>` and uses Records.Resource.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectResourceRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectResourceRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Resource.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Resource>(sql, parameters)
    
    let insertResource (context: MySqlContext) (parameters: Parameters.NewResource) =
        context.Insert("cfg_resources", parameters)
    
    /// Select a `Records.TableColumn` from the table `cfg_table_columns`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableColumn>` and uses Records.TableColumn.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableColumnRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableColumnRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableColumn.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableColumn>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableColumn>` and uses Records.TableColumn.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableColumnRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableColumnRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableColumn.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableColumn>(sql, parameters)
    
    let insertTableColumn (context: MySqlContext) (parameters: Parameters.NewTableColumn) =
        context.Insert("cfg_table_columns", parameters)
    
    /// Select a `Records.TableModelVersion` from the table `cfg_table_model_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableModelVersion>` and uses Records.TableModelVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModelVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableModelVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableModelVersion>` and uses Records.TableModelVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModelVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableModelVersion>(sql, parameters)
    
    let insertTableModelVersion (context: MySqlContext) (parameters: Parameters.NewTableModelVersion) =
        context.Insert("cfg_table_model_versions", parameters)
    
    /// Select a `Records.TableModel` from the table `cfg_table_models`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableModel>` and uses Records.TableModel.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModel.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableModel>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableModel>` and uses Records.TableModel.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableModelRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableModelRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableModel.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableModel>(sql, parameters)
    
    let insertTableModel (context: MySqlContext) (parameters: Parameters.NewTableModel) =
        context.Insert("cfg_table_models", parameters)
    
    /// Select a `Records.TableObjectMapperVersion` from the table `cfg_table_object_mapper_versions`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableObjectMapperVersion>` and uses Records.TableObjectMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableObjectMapperVersionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableObjectMapperVersionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableObjectMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableObjectMapperVersion>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableObjectMapperVersion>` and uses Records.TableObjectMapperVersion.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableObjectMapperVersionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableObjectMapperVersionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableObjectMapperVersion.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableObjectMapperVersion>(sql, parameters)
    
    let insertTableObjectMapperVersion (context: MySqlContext) (parameters: Parameters.NewTableObjectMapperVersion) =
        context.Insert("cfg_table_object_mapper_versions", parameters)
    
    /// Select a `Records.TableObjectMapper` from the table `cfg_table_object_mappers`.
    /// Internally this calls `context.SelectSingleAnon<Records.TableObjectMapper>` and uses Records.TableObjectMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableObjectMapperRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableObjectMapperRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableObjectMapper.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.TableObjectMapper>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.TableObjectMapper>` and uses Records.TableObjectMapper.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectTableObjectMapperRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectTableObjectMapperRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.TableObjectMapper.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.TableObjectMapper>(sql, parameters)
    
    let insertTableObjectMapper (context: MySqlContext) (parameters: Parameters.NewTableObjectMapper) =
        context.Insert("cfg_table_object_mappers", parameters)
    
    /// Select a `Records.PipelineRunItem` from the table `pipeline_runs`.
    /// Internally this calls `context.SelectSingleAnon<Records.PipelineRunItem>` and uses Records.PipelineRunItem.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRunItemRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRunItemRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineRunItem.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.PipelineRunItem>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.PipelineRunItem>` and uses Records.PipelineRunItem.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectPipelineRunItemRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectPipelineRunItemRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.PipelineRunItem.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.PipelineRunItem>(sql, parameters)
    
    let insertPipelineRunItem (context: MySqlContext) (parameters: Parameters.NewPipelineRunItem) =
        context.Insert("pipeline_runs", parameters)
    
    /// Select a `Records.Subscription` from the table `subscriptions`.
    /// Internally this calls `context.SelectSingleAnon<Records.Subscription>` and uses Records.Subscription.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSubscriptionRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectSubscriptionRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Subscription.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.Subscription>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.Subscription>` and uses Records.Subscription.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectSubscriptionRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectSubscriptionRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.Subscription.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.Subscription>(sql, parameters)
    
    let insertSubscription (context: MySqlContext) (parameters: Parameters.NewSubscription) =
        context.Insert("subscriptions", parameters)
    
    /// Select a `Records.User` from the table `users`.
    /// Internally this calls `context.SelectSingleAnon<Records.User>` and uses Records.User.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectUserRecord ctx "WHERE `field` = @0" [ box `value` ]
    let selectUserRecord (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.User.SelectSql() ] @ query |> buildSql
        context.SelectSingleAnon<Records.User>(sql, parameters)
    
    /// Internally this calls `context.SelectAnon<Records.User>` and uses Records.User.SelectSql().
    /// The caller can provide extra string lines to create a query and boxed parameters.
    /// It is up to the caller to verify the sql and parameters are correct,
    /// this should be considered an internal function (not exposed in public APIs).
    /// Parameters are assigned names based on their order in 0 indexed array. For example: @0,@1,@2...
    /// Example: selectUserRecords ctx "WHERE `field` = @0" [ box `value` ]
    let selectUserRecords (context: MySqlContext) (query: string list) (parameters: obj list) =
        let sql = [ Records.User.SelectSql() ] @ query |> buildSql
        context.SelectAnon<Records.User>(sql, parameters)
    
    let insertUser (context: MySqlContext) (parameters: Parameters.NewUser) =
        context.Insert("users", parameters)
    