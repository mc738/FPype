namespace FPype.Infrastructure.Configuration.Tables

open FPype.Configuration
open FPype.Infrastructure.Configuration.Common
open FPype.Infrastructure.Core
open FPype.Infrastructure.Core.Persistence
open Freql.MySql
open FsToolbox.Core.Results

[<RequireQualifiedAccess>]
module StoreOperations =

    let addTable
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (tableReference: string)
        =

        ()

    let addTableVersion
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.tableVersionByReference ctx versionReference
        |> FetchResult.merge (fun tv t -> t, tv) (fun tv -> Fetch.tableById ctx tv.Id)
        |> FetchResult.merge (fun (t, tv) tc -> t, tv, tc) (fun (_, tv) -> Fetch.tableColumns ctx tv.Id)
        |> FetchResult.toResult
        |> Result.bind (fun (t, tv, tc) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription t.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (t, tv, tc))
        |> Result.bind (fun (t, tv, tc) ->

            let columns =
                tc
                |> List.map (fun c ->
                    ({ Name = c.Name
                       DataType = c.DataType
                       Optional = c.Optional
                       ImportHandler = c.ImportHandlerJson }
                    : Tables.NewColumn))

            match store.AddTable(IdType.Specific tv.Reference, t.Name, columns, ItemVersion.Specific tv.Version) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add table `{t}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult
