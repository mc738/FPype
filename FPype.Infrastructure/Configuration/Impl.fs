namespace FPype.Infrastructure.Configuration

open FPype.Configuration
open Freql.MySql
open FsToolbox.Core.Results

[<AutoOpen>]
module Impl =

 
    let buildNewStore () = ()
    
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