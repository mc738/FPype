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
            | PipelineActionAdded data -> failwith "todo"
            | PipelineResourceAdded data -> failwith "todo"
            | PipelineArgAdded data -> failwith "todo"
            | TableAdded data -> failwith "todo"
            | TableVersionAdded data -> failwith "todo"
            | TableColumnAdded data -> failwith "todo"
            | QueryAdded data -> failwith "todo"
            | QueryVersionAdded data -> failwith "todo"
            | ResourceAdded data -> failwith "todo"
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
