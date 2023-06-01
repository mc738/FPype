namespace FPype.Infrastructure.Configuration

open FPype.Configuration

[<AutoOpen>]
module Impl =
    
    
    let createConfigurationStore (subscriptionId: string) (path: string) (additionActions: string list) =
        let metadata =
            [
                "subscription_id", subscriptionId
            ]
            |> Map.ofList
        
        let cfg = ConfigurationStore.Initialize(path, additionActions, metadata)
        
        Tables.ReadOperations.
        
        
        ()

