namespace FPype.Infrastructure.Configuration.ObjectTableMappers

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestObjectTableMapperVersion (ctx: MySqlContext) (userReference: string) (mapperReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.objectTableMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
            Fetch.objectTableMapperLatestVersion ctx mr.Id)
        |> FetchResult.merge (fun (ur, sr, mr, mvr) tr -> ur, sr, mr, mvr, tr) (fun (ur, sr, mr, mvr) ->
            Fetch.tableVersionById ctx mvr.TableModelVersionId)
        |> FetchResult.merge (fun (ur, sr, mr, mvr, tvr) tr -> ur, sr, mr, mvr, tr, tvr) (fun (ur, sr, mr, mvr, tvr) ->
            Fetch.tableById ctx tvr.TableModelId)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvr, tr, tvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, mr, mvr, tr, tvr))
        // Map
        |> Result.map (fun (ur, sr, mr, mvr, tr, tvr) ->

            ({ Reference = mr.Reference
               Name = mr.Name
               Version =
                 { Reference = mvr.Reference
                   Version = mvr.Version
                   TableModelReference = tvr.Reference
                   MapperData = mvr.MapperJson
                   Hash = mvr.Hash
                   CreatedOn = mvr.CreatedOn } }
            : ObjectTableMapperDetails))
        |> FetchResult.fromResult

    let specificObjectTableMapperVersion
        (ctx: MySqlContext)
        (userReference: string)
        (mapperReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.objectTableMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
            Fetch.objectTableMapperVersion ctx mr.Id version)
        |> FetchResult.merge (fun (ur, sr, mr, mvr) tr -> ur, sr, mr, mvr, tr) (fun (ur, sr, mr, mvr) ->
            Fetch.tableVersionById ctx mvr.TableModelVersionId)
        |> FetchResult.merge (fun (ur, sr, mr, mvr, tvr) tr -> ur, sr, mr, mvr, tr, tvr) (fun (ur, sr, mr, mvr, tvr) ->
            Fetch.tableById ctx tvr.TableModelId)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvr, tr, tvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId
                  Verification.userSubscriptionMatches ur tr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, mr, mvr, tr, tvr))
        // Map
        |> Result.map (fun (ur, sr, mr, mvr, tr, tvr) ->

            ({ Reference = mr.Reference
               Name = mr.Name
               Version =
                 { Reference = mvr.Reference
                   Version = mvr.Version
                   TableModelReference = tvr.Reference 
                   MapperData = mvr.MapperJson
                   Hash = mvr.Hash
                   CreatedOn = mvr.CreatedOn } }
            : ObjectTableMapperDetails))
        |> FetchResult.fromResult
