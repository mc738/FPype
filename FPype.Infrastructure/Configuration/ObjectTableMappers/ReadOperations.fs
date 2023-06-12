namespace FPype.Infrastructure.Configuration.ObjectTableMappers

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestObjectTableMapperVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (mapperReference: string)
        =
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
        (logger: ILogger)
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

    let tableObjectMapperVersions (ctx: MySqlContext) (logger: ILogger) (userReference: string) (mapperReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.tableObjectMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvrs -> ur, sr, mr, mvrs) (fun (_, _, mr) ->
            Fetch.objectTableMapperVersionByMapperId ctx mr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, mr, mvrs))
        // Map
        |> Result.map (fun (ur, sr, mr, mvrs) ->
            mvrs
            |> List.map (fun mvr ->
                ({ MapperReference = mr.Reference
                   Reference = mvr.Reference
                   Name = mr.Name
                   Version = mvr.Version }
                : ObjectTableMapperVersionOverview)))
        |> FetchResult.fromResult
    
    let tableObjectMappers (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) mrs -> ur, sr, mrs) (fun (_, sr) ->
            Fetch.objectTableMappersBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mrs) ->
            let verifiers =
                [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

            VerificationResult.verify verifiers (ur, sr, mrs))
        // Map
        |> Result.map (fun (ur, sr, mrs) ->
            mrs
            |> List.map (fun mr ->
                ({ Reference = mr.Reference
                   Name = mr.Name }
                : ObjectTableMapperOverview)))
        |> FetchResult.fromResult