﻿namespace FPype.Infrastructure.Configuration.Tables

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
        Fetch.table ctx tableReference
        |> FetchResult.toResult
        |> Result.bind (fun t ->
            let verifiers =
                [ Verification.subscriptionMatches subscription t.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers t)
        |> Result.bind (fun t ->

            match store.AddTable(t.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add table `{t.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addTableVersion
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.tableVersionByReference ctx versionReference
        |> FetchResult.merge (fun tv t -> t, tv) (fun tv -> Fetch.tableById ctx tv.TableModelId)
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
                    ({ Id = IdType.Specific c.Reference
                       Name = c.Name
                       DataType = c.DataType
                       Optional = c.Optional
                       ImportHandler = c.ImportHandlerJson }
                    : Tables.NewColumn))

            match
                store.AddTableVersion(IdType.Specific tv.Reference, t.Name, columns, ItemVersion.Specific tv.Version)
            with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add table version `{tv.Reference}` ({t.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addTableColumn
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (columnReference: string)
        =
        Fetch.tableColumnByReference ctx columnReference
        |> FetchResult.merge (fun tc tv -> tv, tc) (fun tc -> Fetch.tableVersionById ctx tc.TableVersionId)
        |> FetchResult.merge (fun (tv, tc) t -> t, tv, tc) (fun (tv, tc) -> Fetch.tableById ctx tv.TableModelId)
        |> FetchResult.toResult
        |> Result.bind (fun (t, tv, tc) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription t.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (t, tv, tc))
        |> Result.bind (fun (t, tv, tc) ->
            let result =
                store.AddTableColumn(
                    tv.Reference,
                    ({ Id = IdType.Specific tc.Reference
                       Name = tc.Name
                       DataType = tc.DataType
                       Optional = tc.Optional
                       ImportHandler = tc.ImportHandlerJson }
                    : Tables.NewColumn)
                )

            match result with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add table column `{tc.Reference}` ({tc.Name}) to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult