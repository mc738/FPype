﻿namespace FPype.Infrastructure.DataSinks

open System
open FPype.Data.ModelExtensions

[<AutoOpen>]
module Common =

    open FPype.Data.Models

    [<RequireQualifiedAccess>]
    type DataSinkModelType =
        | Table of TableSchema
        | Object

    [<RequireQualifiedAccess>]
    type DataSinkType =
        | Push
        | Pull

    type DataSinkSettings =
        { Id: string
          SubscriptionId: string
          StorePath: string
          ModelType: DataSinkModelType
          Type: DataSinkModelType }


    type Metadata =
        { ItemId: string
          ItemKey: string
          ItemValue: string }

        static member CreateTableSql() =
            """
        CREATE TABLE __metadata (
            item_id TEXT NOT NULL
            item_key TEXT NOT NULL
            item_value TEXT NOT NULL
            CONSTRAINT __metadata_pk PRIMARY KEY (item_id, item_key)
        );
        """

    type ReadRequest =
        { RequestId: string
          Requester: string
          RequestTimestamp: DateTime
          WasSuccessful: bool }

        static member CreateTableSql() =
            """
        CREATE TABLE __read_requests (
            request_id TEXT NOT NULL,
            requester TEXT NOT NULL,
            request_timestamp TEXT NOT NULL,
            was_successful INTEGER NOT NULL,
            CONSTRAINT __resources_PK PRIMARY KEY (request_id)
        );
        """
