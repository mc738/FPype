namespace FPype.Infrastructure.Configuration.TableObjectMappers

[<RequireQualifiedAccess>]
module CreateOperations =
    
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let query (ctx: MySqlContext) (userReference: string) (mapper: NewTableObjectMapper) =
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

                let mapperId =
                    ({ Reference = mapper.Reference
                       SubscriptionId = sr.Id
                       Name = mapper.Name }
                    : Parameters.NewTableObjectMapper)
                    |> Operations.insertTableObjectMapper t
                    |> int

                ({ Reference = mapper.Version.Reference
                   TableObjectMapperId = mapperId
                   Version = 1
                   MapperJson = mapper.Version.MapperData 
                   Hash = mapper.Version.MapperData.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewTableObjectMapperVersion)
                |> Operations.insertTableObjectMapperVersion t
                |> ignore))
        |> toActionResult "Create table object mapper"

    let queryVersion (ctx: MySqlContext) (userReference: string) (mapperReference: string) (version: NewTableObjectMapperVersion) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) mr -> ur, sr, mr) (Fetch.tableObjectMapper t mapperReference)
            |> FetchResult.merge (fun (ur, sr, mr) mvr -> ur, sr, mr, mvr) (fun (_, _, mr) ->
                Fetch.tableObjectMapperLatestVersion t mr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, mr, mvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur mr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, mr, mvr))
            // Create
            |> Result.map (fun (ur, sr, mr, mvr) ->

                ({ Reference = version.Reference
                   TableObjectMapperId = mr.Id
                   Version = mvr.Version + 1
                   MapperJson = version.MapperData 
                   Hash = version.MapperData.GetSHA256Hash()
                   CreatedOn = timestamp () }
                : Parameters.NewTableObjectMapperVersion)
                |> Operations.insertTableObjectMapperVersion t
                |> ignore))
        |> toActionResult "Create table object mapper version"