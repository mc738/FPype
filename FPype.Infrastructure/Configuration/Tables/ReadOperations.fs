namespace FPype.Infrastructure.Configuration.Tables

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    module Internal =

        let allTablesForSubscription (ctx: MySqlContext) (subscriptionId: string) =
            Operations.selectTableModelRecords ctx [ "WHERE subscription_id = @0" ] [ subscriptionId ]
            |> List.map (fun tr ->
                Fetch.tableVersions ctx tr.Id
                |> FetchResult.map (fun tvrs ->
                    tvrs
                    |> List.choose (fun tvr ->
                        match Fetch.tableColumns ctx tvr.Id with
                        | FetchResult.Success tcs ->
                            ({ TableReference = tr.Reference
                               Reference = tvr.Reference
                               Name = tr.Name
                               Version = tvr.Version
                               CreatedOn = tvr.CreatedOn
                               Columns =
                                 tcs
                                 |> List.map (fun c ->
                                     ({ Reference = c.Reference
                                        Name = c.Name
                                        Type = BaseType.String // TODO sort out
                                        Optional = c.Optional
                                        ImportHandlerData = c.ImportHandlerJson
                                        Index = c.ColumnIndex }
                                     : TableColumnDetails)) }
                            : TableVersionDetails)
                            |> Some
                        | FetchResult.Failure fr -> None)))

        let createTableColumnDetails (defaultBaseType: BaseType option) (column: Records.TableColumn) =
            ({ Reference = column.Reference
               Name = column.Name
               Type =
                 // If a default base type if provided that will be used in the (unlikely) event the base type is unknown.
                 // If none is provided and error will be thrown.
                 // However, this should be a rare occurrence.
                 BaseType.FromId(column.DataType, column.Optional)
                 |> Option.defaultWith (fun _ ->
                     match defaultBaseType with
                     | Some dbt -> dbt
                     | None -> failwith $"Unknown base type: `{column.DataType}`")
               Optional = column.Optional
               ImportHandlerData = column.ImportHandlerJson
               Index = column.ColumnIndex }
            : TableColumnDetails)

    let latestTableVersion (ctx: MySqlContext) (logger: ILogger) (userReference: string) (tableReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) tr -> ur, sr, tr) (Fetch.table ctx tableReference)
        |> FetchResult.merge (fun (ur, sr, tr) tvr -> ur, sr, tr, tvr) (fun (_, _, tr) ->
            Fetch.tableLatestVersion ctx tr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, tr, tvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, tr, tvr))
        // Map
        |> Result.map (fun (ur, sr, tr, tvr) ->

            match Fetch.tableColumns ctx tvr.Id with
            | FetchResult.Success tcs ->
                ({ TableReference = tr.Reference
                   Reference = tvr.Reference
                   Name = tr.Name
                   Version = tvr.Version
                   CreatedOn = tvr.CreatedOn
                   Columns = tcs |> List.map (Internal.createTableColumnDetails (Some BaseType.String)) }
                : TableVersionDetails)
                |> Some
            | FetchResult.Failure fr -> None)
        |> optionalToFetchResult "Latest table version"

    let specificTableVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (tableReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) tr -> ur, sr, tr) (Fetch.table ctx tableReference)
        |> FetchResult.merge (fun (ur, sr, tr) tvr -> ur, sr, tr, tvr) (fun (_, _, tr) ->
            Fetch.tableVersion ctx tr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, tr, tvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, tr, tvr))
        // Map
        |> Result.map (fun (ur, sr, tr, tvr) ->

            match Fetch.tableColumns ctx tvr.Id with
            | FetchResult.Success tcs ->
                ({ TableReference = tr.Reference
                   Reference = tvr.Reference
                   Name = tr.Name
                   Version = tvr.Version
                   CreatedOn = tvr.CreatedOn
                   Columns = tcs |> List.map (Internal.createTableColumnDetails (Some BaseType.String)) }
                : TableVersionDetails)
                |> Some
            | FetchResult.Failure fr -> None)
        |> optionalToFetchResult "Specific table version"

    let tableVersions (ctx: MySqlContext) (logger: ILogger) (userReference: string) (tableReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) tr -> ur, sr, tr) (Fetch.table ctx tableReference)
        |> FetchResult.merge (fun (ur, sr, tr) tvr -> ur, sr, tr, tvr) (fun (_, _, tr) ->
            Fetch.tableVersionsByTableId ctx tr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, tr, tvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, tr, tvrs))
        // Map
        |> Result.map (fun (ur, sr, tr, tvrs) ->
            tvrs
            |> List.map (fun tvr ->
                ({ TableReference = tr.Reference
                   Reference = tvr.Reference
                   Name = tr.Name
                   Version = tvr.Version }
                : TableVersionOverview)))
        |> FetchResult.fromResult

    let table (ctx: MySqlContext) (logger: ILogger) (userReference: string) (tableReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) tr -> ur, sr, tr) (Fetch.table ctx tableReference)
        |> FetchResult.merge (fun (ur, sr, tr) tvr -> ur, sr, tr, tvr) (fun (_, _, tr) ->
            Fetch.tableVersionsByTableId ctx tr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, tr, tvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, tr, tvrs))
        // Map
        |> Result.map (fun (ur, sr, tr, tvrs) ->
            tvrs
            |> List.map (fun tvr ->
                let columns =
                    match Fetch.tableColumns ctx tvr.Id with
                    | FetchResult.Success cs -> cs
                    | FetchResult.Failure f ->
                        // TODO log error
                        []

                ({ TableReference = tr.Reference
                   Reference = tvr.Reference
                   Name = tr.Name
                   Version = tvr.Version
                   CreatedOn = tvr.CreatedOn
                   Columns = columns |> List.map (Internal.createTableColumnDetails (Some BaseType.String)) }
                : TableVersionDetails)))
        |> FetchResult.fromResult

    let tables (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) trs -> ur, sr, trs) (fun (_, sr) -> Fetch.tablesBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, trs) ->
            let verifiers =
                [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

            VerificationResult.verify verifiers (ur, sr, trs))
        // Map
        |> Result.map (fun (ur, sr, trs) ->
            trs
            |> List.map (fun tr ->
                ({ Reference = tr.Reference
                   Name = tr.Name }
                : TableOverview)))
        |> FetchResult.fromResult
