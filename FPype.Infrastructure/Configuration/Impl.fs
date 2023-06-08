﻿namespace FPype.Infrastructure.Configuration

open System
open FPype.Infrastructure.Configuration.Common.Events
open FPype.Infrastructure.Core.Persistence

[<AutoOpen>]
module Impl =

    open Armarium
    open FPype.Configuration
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Core.Results

    module Internal =

        let handleEvent
            (ctx: MySqlContext)
            (fileRepo: FileRepository)
            (readArgs: FileReadOperationArguments)
            (subscription: Records.Subscription)
            (event: ConfigurationEvent)
            (store: ConfigurationStore)

            =
            match event with
            | PipelineAdded data -> Pipelines.StoreOperations.addPipeline ctx store subscription data.Reference
            | PipelineVersionAdded data ->
                Pipelines.StoreOperations.addPipelineVersion ctx store subscription data.Reference
            | PipelineActionAdded data ->
                Pipelines.StoreOperations.addPipelineAction ctx store subscription data.Reference
            | PipelineResourceAdded data ->
                Pipelines.StoreOperations.addPipelineResource ctx store subscription data.Reference
            | PipelineArgAdded data -> Pipelines.StoreOperations.addPipelineArg ctx store subscription data.Reference
            | TableAdded data -> Tables.StoreOperations.addTable ctx store subscription data.Reference
            | TableVersionAdded data -> Tables.StoreOperations.addTableVersion ctx store subscription data.Reference
            | TableColumnAdded data -> Tables.StoreOperations.addTableColumn ctx store subscription data.Reference
            | QueryAdded data -> Queries.StoreOperations.addQuery ctx store subscription data.Reference
            | QueryVersionAdded data -> Queries.StoreOperations.addQueryVersion ctx store subscription data.Reference
            | ResourceAdded data -> Resources.StoreOperations.addResource ctx store subscription data.Reference
            | ResourceVersionAdded data ->
                Resources.StoreOperations.addResourceVersion ctx store fileRepo readArgs subscription data.Reference
            | TableObjectMapperAdded data ->
                TableObjectMappers.StoreOperations.addTableObjectMapper ctx store subscription data.Reference
            | TableObjectMapperVersionAdded data ->
                TableObjectMappers.StoreOperations.addTableObjectMapperVersion ctx store subscription data.Reference
            | ObjectTableMapperAdded data ->
                ObjectTableMappers.StoreOperations.addObjectTableMapper ctx store subscription data.Reference
            | ObjectTableMapperVersionAdded data ->
                ObjectTableMappers.StoreOperations.addObjectTableMapperVersion ctx store subscription data.Reference

    let buildNewStore
        (ctx: MySqlContext)
        (fileRepo: FileRepository)
        (readArgs: FileReadOperationArguments)
        (subscription: string)
        (path: string)
        (failOnError: bool)
        (additionActions: string list)
        =
        // No need to check serials when creating a fresh store.
        // Get tip serial however
        Fetch.subscriptionByReference ctx subscription
        |> FetchResult.toActionResult
        |> ActionResult.bind (fun sub ->
            // A transaction is used to "freeze" the database.
            // This might not be the best idea (?) but it is to ensure no updates happen while building the config

            let result =
                ctx.ExecuteInTransaction(fun t ->

                    let tip = Events.selectGlobalTip t

                    let metadata =
                        [ "subscription_id", subscription; "serial_tip", string tip ] |> Map.ofList

                    let cfg = ConfigurationStore.Initialize(path, additionActions, metadata)

                    Tables.StoreOperations.addAllTableVersions t failOnError sub cfg
                    |> ActionResult.bind (Queries.StoreOperations.addAllQueryVersions t failOnError sub)
                    |> ActionResult.bind (
                        Resources.StoreOperations.addAllResourceVersions t fileRepo readArgs failOnError sub
                    )
                    |> ActionResult.bind (
                        TableObjectMappers.StoreOperations.addAllTableObjectMapperVersions t failOnError sub
                    )
                    |> ActionResult.bind (
                        ObjectTableMappers.StoreOperations.addAllObjectTableMapperVersions t failOnError sub
                    )
                    |> ActionResult.bind (Pipelines.StoreOperations.addAllPipelineVersions t failOnError sub))

            match result with
            | Ok ar -> ar
            | Error e ->
                ({ Message = e
                   DisplayMessage = "Failed to create store"
                   Exception = None }
                : FailureResult)
                |> ActionResult.Failure)

    let buildStoreFromSerial
        (ctx: MySqlContext)
        (fileRepo: FileRepository)
        (readArgs: FileReadOperationArguments)
        (subscription: string)
        (path: string)
        (failOnError: bool)
        (additionActions: string list)
        =
        Fetch.subscriptionByReference ctx subscription
        |> FetchResult.toActionResult
        |> ActionResult.bind (fun sub ->
            let cfg = ConfigurationStore.Load(path)
            
            let cfgSubscriptionId = cfg.GetMetadataItem "subscription_id" |> Option.map (fun sid -> sid.ItemValue)
            
            // NOTE Should this default to 0?
            let cfgTip = cfg.GetMetadataItem "serial_tip"
                         |> Option.bind (fun st -> match Int32.TryParse st.ItemValue with | true, v -> Some v | false, _ -> None)
                         |> Option.defaultValue 0
            
            match cfgSubscriptionId with
            | Some sid when sid = sub.Reference ->
                let events = Events.selectEventRecords ctx sub.Id cfgTip
                
                
                // Set the new serial tip
                cfg.AddMetadataItem("serial_tip", string 0, true)
                
                ActionResult.Success ()
            | Some sid ->
                ({ Message = $"Configuration store subscription (`{sid}`) does not match requested subscription `{sub.Reference}`"
                   DisplayMessage = "Subscription mismatch"
                   Exception = None }: FailureResult)
                |> ActionResult.Failure
            | None ->
                ({ Message = $"Configuration store subscription has no subscription id set"
                   DisplayMessage = "Subscription missing"
                   Exception = None }: FailureResult)
                |> ActionResult.Failure)
