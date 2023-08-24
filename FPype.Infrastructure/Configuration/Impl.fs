namespace FPype.Infrastructure.Configuration

open System
open System.IO
open FPype.Data.Store
open FPype.Infrastructure.Configuration.Common.Events
open FPype.Infrastructure.Core.Persistence
open Microsoft.Extensions.Logging

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
            (logger: ILogger)
            (fileRepo: FileRepository)
            (readArgs: FileReadOperationArguments)
            (subscription: Records.Subscription)
            (event: ConfigurationEvent)
            (store: ConfigurationStore)
            =
            match event with
            | PipelineAdded data -> Pipelines.StoreOperations.addPipeline ctx logger store subscription data.Reference
            | PipelineVersionAdded data ->
                Pipelines.StoreOperations.addPipelineVersion ctx logger store subscription data.Reference
            | PipelineActionAdded data ->
                Pipelines.StoreOperations.addPipelineAction ctx logger store subscription data.Reference
            | PipelineResourceAdded data ->
                Pipelines.StoreOperations.addPipelineResource ctx logger store subscription data.Reference
            | PipelineArgAdded data -> Pipelines.StoreOperations.addPipelineArg ctx logger store subscription data.Reference
            | TableAdded data -> Tables.StoreOperations.addTable ctx logger store subscription data.Reference
            | TableVersionAdded data -> Tables.StoreOperations.addTableVersionExcludingColumns ctx logger store subscription data.Reference
            | TableColumnAdded data -> Tables.StoreOperations.addTableColumn ctx logger store subscription data.Reference
            | QueryAdded data -> Queries.StoreOperations.addQuery ctx logger store subscription data.Reference
            | QueryVersionAdded data -> Queries.StoreOperations.addQueryVersion ctx logger store subscription data.Reference
            | ResourceAdded data -> Resources.StoreOperations.addResource ctx logger store subscription data.Reference
            | ResourceVersionAdded data ->
                Resources.StoreOperations.addResourceVersion ctx logger store fileRepo readArgs subscription data.Reference
            | TableObjectMapperAdded data ->
                TableObjectMappers.StoreOperations.addTableObjectMapper ctx store subscription data.Reference
            | TableObjectMapperVersionAdded data ->
                TableObjectMappers.StoreOperations.addTableObjectMapperVersion ctx logger store subscription data.Reference
            | ObjectTableMapperAdded data ->
                ObjectTableMappers.StoreOperations.addObjectTableMapper ctx logger store subscription data.Reference
            | ObjectTableMapperVersionAdded data ->
                ObjectTableMappers.StoreOperations.addObjectTableMapperVersion ctx logger store subscription data.Reference

    let buildNewStore
        (ctx: MySqlContext)
        (logger: ILogger)
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

            logger.LogInformation($"Build fresh configuration store for subscription {sub.Id}")
            
            let result =
                ctx.ExecuteInTransaction(fun t ->

                    let tip = Events.selectGlobalTip t

                    let cfg = ConfigurationStore.Initialize(path, additionActions, subscriptionId = subscription, serialTip = tip)
                    
                    Tables.StoreOperations.addAllTableVersions t logger failOnError sub cfg
                    |> ActionResult.bind (Queries.StoreOperations.addAllQueryVersions t logger failOnError sub)
                    |> ActionResult.bind (
                        Resources.StoreOperations.addAllResourceVersions t logger fileRepo readArgs failOnError sub
                    )
                    |> ActionResult.bind (
                        TableObjectMappers.StoreOperations.addAllTableObjectMapperVersions t logger failOnError sub
                    )
                    |> ActionResult.bind (
                        ObjectTableMappers.StoreOperations.addAllObjectTableMapperVersions t logger failOnError sub
                    )
                    |> ActionResult.bind (Pipelines.StoreOperations.addAllPipelineVersions t logger failOnError sub))

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
        (logger: ILogger)
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

            let cfgSubscriptionId = cfg.GetSubscriptionId()
            
            // NOTE Should this default to 0?
            let cfgTip =
                cfg.GetSerialTip()
                |> Option.defaultValue 0

            logger.LogInformation($"Building configuration store for subscription {sub.Reference}. Current serial: {cfgTip}")
            
            match cfgSubscriptionId with
            | Some sid when sid = sub.Reference ->
                let eventRecords = Events.selectEventRecords ctx sub.Id cfgTip

                match eventRecords.IsEmpty with
                | true ->
                    logger.LogInformation("No events to handle, configuration store is up-to-date.")
                    ActionResult.Success cfg
                | false ->
                    let events = Events.deserializeRecords eventRecords
                    let newTip = eventRecords |> List.maxBy (fun er -> er.Id) |> (fun er -> er.Id)

                    logger.LogInformation($"Events to handle: {events.Length}")
                    
                    events
                    |> List.fold
                        (fun (r: ActionResult<unit>) er ->
                            match er, r with
                            | FetchResult.Success e, ActionResult.Success _ ->
                                match Internal.handleEvent ctx logger fileRepo readArgs sub e cfg with
                                | ActionResult.Success _ -> ActionResult.Success()
                                | ActionResult.Failure f when failOnError ->
                                    // NOTE This uses the display message because the log is not fully internal (potentially).
                                    logger.LogWarning($"Failed to handle event. Reason: {f.DisplayMessage}. Skipping")
                                    ActionResult.Success()
                                | ActionResult.Failure f -> ActionResult.Failure f
                            | FetchResult.Failure f, _ ->
                                match failOnError with
                                | true -> ActionResult.Failure f
                                | false ->
                                    // NOTE This uses the display message because the log is not fully internal (potentially).
                                    logger.LogWarning($"Failed to deserialize event. Reason: {f.DisplayMessage}. Skipping")
                                    ActionResult.Success()
                            | _, ActionResult.Failure f -> r)
                        (ActionResult.Success())
                    |> ActionResult.map (fun _ ->
                        cfg.SetSerialTip(newTip)
                        cfg)
            | Some sid ->
                let msg = $"Configuration store subscription (`{sid}`) does not match requested subscription `{sub.Reference}`"
                logger.LogError(msg)
                ({ Message = msg
                   DisplayMessage = "Subscription mismatch"
                   Exception = None }
                : FailureResult)
                |> ActionResult.Failure
            | None ->
                let msg = "Configuration store subscription has no subscription id set"
                logger.LogError(msg)
                ({ Message = msg
                   DisplayMessage = "Subscription missing"
                   Exception = None }
                : FailureResult)
                |> ActionResult.Failure)

    let buildStore
        (ctx: MySqlContext)
        (logger: ILogger)
        (fileRepo: FileRepository)
        (readArgs: FileReadOperationArguments)
        (subscription: string)
        (path: string)
        (failOnError: bool)
        (additionActions: string list)
        =
            match File.Exists path with
            | true ->
                buildStoreFromSerial ctx logger fileRepo readArgs subscription path failOnError additionActions
            | false ->
                buildNewStore ctx logger fileRepo readArgs subscription path failOnError additionActions
        