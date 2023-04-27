namespace FPype.Infrastructure.Configuration.TableObjectMappers

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestTableObjectMapperVersion (ctx: MySqlContext) (userReference: string) (mapperReference: string) =
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
                  Verification.userSubscriptionMatches ur mr.SubscriptionId ]

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

    let specificTableObjectMapperVersion (ctx: MySqlContext) (userReference: string) (mapperReference: string) (version: int) =
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
                  Verification.userSubscriptionMatches ur mr.SubscriptionId ]

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


