namespace FPype.Infrastructure.Configuration.ObjectTableMappers

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let query (ctx: MySqlContext) (userReference: string) (mapper: NewObjectTableMapper) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) tvr -> ur, sr, tvr) (Fetch.tableVersionByReference t mapper.Version.TableModelReference)
            |> FetchResult.merge (fun (ur, sr, tvr) tr -> ur, sr, tr, tvr) (fun (_, _, tvr) -> Fetch.tableById t tvr.Id)
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

                let mapperId =
                    ({ Reference = mapper.Reference
                       SubscriptionId = sr.Id
                       Name = mapper.Name }
                    : Parameters.NewObjectTableMapper)
                    |> Operations.insertObjectTableMapper t
                    |> int

                ({ Reference = mapper.Version.Reference
                   ObjectTableMapperId = mapperId
                   Version = 1
                   TableModelVersionId = tvr.Id
                   MapperJson = mapper.Version.MapperData
                   Hash = mapper.Version.MapperData.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewObjectTableMapperVersion)
                |> Operations.insertObjectTableMapperVersion t
                |> ignore))
        |> toActionResult "Create object table mapper"

    let queryVersion
        (ctx: MySqlContext)
        (userReference: string)
        (mapperReference: string)
        (version: NewObjectTableMapperVersion)
        =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.objectTableMapper t mapperReference)
            |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
                Fetch.objectTableMapperLatestVersion t mr.Id)
            |> FetchResult.chain
                (fun (ur, sr, mr, mvr) tr -> ur, sr, mr, mvr, tr)
                (Fetch.tableVersionByReference t version.TableModelReference)
            |> FetchResult.merge
                (fun (ur, sr, mr, mvr, tvr) tr -> ur, sr, mr, mvr, tr, tvr)
                (fun (ur, sr, mr, mvr, tvr) -> Fetch.tableById t tvr.TableModelId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, mr, mvr, tr, tvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur mr.SubscriptionId
                      Verification.userSubscriptionMatches ur tr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, mr, mvr, tr, tvr))
            // Create
            |> Result.map (fun (ur, sr, mr, mvr, tr, tvr) ->

                ({ Reference = version.Reference
                   ObjectTableMapperId = mr.Id
                   Version = mvr.Version + 1
                   TableModelVersionId = tvr.Id
                   MapperJson = version.MapperData
                   Hash = version.MapperData.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewObjectTableMapperVersion)
                |> Operations.insertObjectTableMapperVersion t
                |> ignore))
        |> toActionResult "Create object table mapper version"
