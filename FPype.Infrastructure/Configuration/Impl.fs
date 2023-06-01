namespace FPype.Infrastructure.Configuration

open FPype.Configuration
open Freql.MySql
open FsToolbox.Core.Results

[<AutoOpen>]
module Impl =


    let createConfigurationStore
        (ctx: MySqlContext)
        (subscriptionId: string)
        (path: string)
        (additionActions: string list)
        =
        let metadata = [ "subscription_id", subscriptionId ] |> Map.ofList

        let cfg = ConfigurationStore.Initialize(path, additionActions, metadata)

        Tables.ReadOperations.Internal.allTablesForSubscription ctx subscriptionId
        |> List.fold (fun (r1: Result<unit, string>) fr ->
            match r1 with
            | Ok _ ->
                // TODO sort
                fr
                |> FetchResult.map (fun tvds ->
                    tvds
                    |> List.fold (fun (result: Result<unit, string>) tvd ->
                        match result with
                        | Ok _ ->
                            let columns =
                                tvd.Columns
                                |> List.map (fun c ->
                                    ({ Name = c.Name
                                       DataType = c.Type.Serialize()
                                       Optional = c.Optional
                                       ImportHandler = c.ImportHandlerData }
                                    : FPype.Configuration.Tables.NewColumn))

                            cfg.AddTable(IdType.Specific tvd.Reference, tvd.Name, columns, ItemVersion.Specific tvd.Version)
                        | Error e -> Error e) (Ok ()))
                |> ignore
                
                Ok ()
                    
                
            | Error e -> Error e) (Ok ())
