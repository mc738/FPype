namespace FPype.Infrastructure.Configuration

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
            | PipelineArgAdded data ->
                Pipelines.StoreOperations.addPipelineArg ctx store subscription data.Reference
            | TableAdded data ->
                Tables.StoreOperations.addTable ctx store subscription data.Reference
            | TableVersionAdded data ->
                Tables.StoreOperations.addTableVersion ctx store subscription data.Reference
            | TableColumnAdded data -> 
                Tables.StoreOperations.addTableColumn ctx store subscription data.Reference
            | QueryAdded data ->
                Queries.StoreOperations.addQuery ctx store subscription data.Reference
            | QueryVersionAdded data ->
                Queries.StoreOperations.addQueryVersion ctx store subscription data.Reference
            | ResourceAdded data ->
                Resources.StoreOperations.addResource ctx store subscription data.Reference
            | ResourceVersionAdded data -> failwith "todo"
            | TableObjectMapperAdded data -> failwith "todo"
            | TableObjectMapperVersionAdded data -> failwith "todo"
            | ObjectTableMapperAdded data -> failwith "todo"
            | ObjectTableMapperVersionAdded data -> failwith "todo"


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

                    let tip = Events.selectGlobalTip

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

    let buildStoreFromSerial () = ()



    let createConfigurationStore
        (ctx: MySqlContext)
        (subscriptionId: string)
        (path: string)
        (additionActions: string list)
        =
        let metadata = [ "subscription_id", subscriptionId ] |> Map.ofList

        let cfg = ConfigurationStore.Initialize(path, additionActions, metadata)

        // Get pipelines
        // Set


        ()
