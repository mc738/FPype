namespace FPype.Infrastructure.Configuration

open FPype.Configuration
open FPype.Infrastructure.Configuration.Common
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results

[<AutoOpen>]
module Impl =

 
    let buildNewStore
        (ctx: MySqlContext)
        (subscription: string)
        (path: string)
        (additionActions: string list)
        =
        // No need to check serials when creating a fresh store.
        // Get tip serial however
        Fetch.subscriptionByReference t subscription
        |> FetchResult.toResult
        |> Result.map (fun sub ->
            // A transaction is used to "freeze" the database.
        // This might not be the best idea (?) but it is to ensure no updates happen while building the config
        ctx.ExecuteInTransaction(fun t ->
                
            let tip = Events.selectGlobalTip
            
            let metadata =
                [ "subscription_id", subscription
                  "serial_tip", string tip ] |> Map.ofList

            let cfg = ConfigurationStore.Initialize(path, additionActions, metadata)
            
            // Add tables
            
            let tables =
                Operations.selectTableModelRecords t [ "WHERE subscription_id = @0" ] [ sub.Id ]
                |> List.map (fun tm ->
                    Operations.selectTableModelVersionRecords t [ "WHERE table_model_id = @0" ] [ tm.Id ]
                    |> List.map (fun tv ->
                        let tcs =
                            Operations.selectTableColumnRecords t [ "WHERE table_model_version = @0" ] [ tv.Id ]
                        
                        
                        
                        ())
                    
                    
                    
                    )
            
            cfg.AddTableVersion
            
            
            ()))
            
        
        

    
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