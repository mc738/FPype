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
