namespace FPype.Infrastructure.Configuration.Tables

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Core.Results

    let table (ctx: MySqlContext) (userReference: string) (table: NewTable) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr) ->
                let verifiers =
                    [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

                VerificationResult.verify verifiers (ur, sr))
            // Create
            |> Result.map (fun (ur, sr) ->

                let tableId =
                    ({ Reference = table.Reference
                       SubscriptionId = sr.Id
                       Name = table.Name }
                    : Parameters.NewTableModel)
                    |> Operations.insertTableModel t
                    |> int

                let versionId =
                    ({ Reference = table.Version.Reference
                       TableModelId = tableId
                       Version = 1
                       CreatedOn = timestamp () }
                    : Parameters.NewTableModelVersion)
                    |> Operations.insertTableModelVersion t
                    |> int

                table.Version.Columns
                |> List.iter (fun c ->
                    ({ Reference = c.Reference
                       TableVersionId = versionId
                       Name = c.Name
                       DataType = c.Type.Serialize()
                       Optional = c.Optional
                       ImportHandlerJson = c.ImportHandlerData
                       ColumnIndex = c.Index }
                    : Parameters.NewTableColumn)
                    |> Operations.insertTableColumn t
                    |> ignore)))
        |> toActionResult "Create table"

    let tableVersion (ctx: MySqlContext) (userReference: string) (tableReference: string) (version: NewTableVersion) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) tr -> ur, sr, tr) (Fetch.table t tableReference)
            |> FetchResult.merge (fun (ur, sr, tr) tvr -> ur, sr, tr, tvr) (fun (_, _, pr) ->
                Fetch.tableLatestVersion t pr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, tr, tvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur tr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, tr, tvr))
            // Create
            |> Result.map (fun (ur, sr, tr, tvr) ->

                let versionId =
                    ({ Reference = version.Reference
                       TableModelId = tr.Id
                       Version = tvr.Version + 1
                       CreatedOn = timestamp () }
                    : Parameters.NewTableModelVersion)
                    |> Operations.insertTableModelVersion t
                    |> int

                version.Columns
                |> List.iter (fun c ->
                    ({ Reference = c.Reference
                       TableVersionId = versionId
                       Name = c.Name
                       DataType = c.Type.Serialize()
                       Optional = c.Optional
                       ImportHandlerJson = c.ImportHandlerData
                       ColumnIndex = c.Index }
                    : Parameters.NewTableColumn)
                    |> Operations.insertTableColumn t
                    |> ignore)))
        |> toActionResult "Create table version"

    let tableColumn (ctx: MySqlContext) (userReference: string) (versionReference: string) (column: NewTableColumn) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) tvr -> ur, sr, tvr) (Fetch.tableVersionByReference t versionReference)
            |> FetchResult.merge (fun (ur, sr, tvr) tr -> ur, sr, tr, tvr) (fun (_, _, tvr) ->
                Fetch.tableById t tvr.TableModelId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, tr, tvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur tr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, tr, tvr))
            |> Result.map (fun (ur, sr, pr, pvr) ->
                ({ Reference = column.Reference
                   TableVersionId = pvr.Id
                   Name = column.Name
                   DataType = column.Type.Serialize()
                   Optional = column.Optional
                   ImportHandlerJson = column.ImportHandlerData
                   ColumnIndex = column.Index }
                : Parameters.NewTableColumn)
                |> Operations.insertTableColumn t
                |> ignore))
        |> toActionResult "Create table column"
