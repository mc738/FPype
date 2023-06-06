namespace FPype.Infrastructure.Configuration

open Armarium
open FPype.Configuration
open FPype.Infrastructure.Configuration.Common
open FPype.Infrastructure.Configuration.Tables
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open Freql.Sqlite
open FsToolbox.Core.Results

[<AutoOpen>]
module Impl =

    module Internal =

        let addTable
            (ctx: MySqlContext)
            (cfg: ConfigurationStore)
            (subscription: Records.Subscription)
            (tm: Records.TableModel)
            =
            Operations.selectTableModelVersionRecords ctx [ "WHERE table_model_id = @0" ] [ tm.Id ]
            |> List.map (fun tv ->
                Tables.StoreOperations.addTableVersion ctx cfg subscription tv.Reference

                let tcs =
                    Operations.selectTableColumnRecords ctx [ "WHERE table_model_version = @0" ] [ tv.Id ]
                    |> List.map (fun tc ->
                        ({ Id = IdType.Specific tc.Reference
                           Name = tc.Name
                           DataType = tc.DataType
                           Optional = tc.Optional
                           ImportHandler = tc.ImportHandlerJson }
                        : Tables.NewColumn))

                cfg.AddTableVersion(IdType.Specific tv.Reference, tm.Name, tcs, ItemVersion.Specific tv.Version))

        let addQuery
            (ctx: MySqlContext)
            (cfg: ConfigurationStore)
            (subscription: Records.Subscription)
            (tm: Records.TableModel)
            =
            Operations.selectQueryVersionRecords ctx [ "WHERE query_id = @0" ] [ tm.Id ]
            |> List.map (fun qv ->
                // Double fetch?
                Queries.StoreOperations.addQueryVersion ctx cfg subscription qv.Reference)

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

                    // Add tables
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
                    ))

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
