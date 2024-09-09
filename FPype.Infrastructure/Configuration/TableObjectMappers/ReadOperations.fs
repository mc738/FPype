namespace FPype.Infrastructure.Configuration.TableObjectMappers

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestTableObjectMapperVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (mapperReference: string)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.tableObjectMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
            Fetch.tableObjectMapperLatestVersion ctx mr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, mr, mvr))
        // Map
        |> Result.map (fun (ur, sr, mr, mvr) ->

            ({ Reference = mr.Reference
               Name = mr.Name
               Version =
                 { Reference = mvr.Reference
                   Version = mvr.Version
                   MapperData = mvr.MapperJson
                   Hash = mvr.Hash
                   CreatedOn = mvr.CreatedOn } }
            : TableObjectMapperDetails))
        |> FetchResult.fromResult

    let specificTableObjectMapperVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (mapperReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.tableObjectMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
            Fetch.tableObjectMapperVersion ctx mr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, mr, mvr))
        // Map
        |> Result.map (fun (ur, sr, mr, mvr) ->

            ({ Reference = mr.Reference
               Name = mr.Name
               Version =
                 { Reference = mvr.Reference
                   Version = mvr.Version
                   MapperData = mvr.MapperJson
                   Hash = mvr.Hash
                   CreatedOn = mvr.CreatedOn } }
            : TableObjectMapperDetails))
        |> FetchResult.fromResult

    let tableObjectMapperVersions
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (mapperReference: string)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.tableObjectMapper ctx mapperReference)
        |> FetchResult.merge (fun (ur, sr, mr) mvrs -> ur, sr, mr, mvrs) (fun (_, _, mr) ->
            Fetch.tableObjectMapperVersionByMapperId ctx mr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mr, mvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur mr.SubscriptionId
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, mr, mvrs))
        // Map
        |> Result.map (fun (ur, sr, mr, mvrs) ->
            mvrs
            |> List.map (fun mvr ->
                ({ MapperReference = mr.Reference
                   Reference = mvr.Reference
                   Name = mr.Name
                   Version = mvr.Version }
                : TableObjectMapperVersionOverview)))
        |> FetchResult.fromResult

    let tableObjectMappers (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) mrs -> ur, sr, mrs) (fun (_, sr) ->
            Fetch.tableObjectMappersBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, mrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                  Verification.isNotSystemSubscription sr
                  Verification.isNotSystemUser ur ]

            VerificationResult.verify verifiers (ur, sr, mrs))
        // Map
        |> Result.map (fun (ur, sr, mrs) ->
            mrs
            |> List.map (fun mr ->
                ({ Reference = mr.Reference
                   Name = mr.Name }
                : TableObjectMapperOverview)))
        |> FetchResult.fromResult
