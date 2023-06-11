namespace FPype.Infrastructure.Configuration.TableObjectMappers

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module CreateOperations =
    
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let tableObjectMapper (ctx: MySqlContext) (logger: ILogger) (userReference: string) (mapper: NewTableObjectMapper) =
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
                let timestamp = getTimestamp ()
                let hash = mapper.Version.MapperData.GetSHA256Hash()
                
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
                   Hash = hash
                   CreatedOn = timestamp }
                : Parameters.NewTableObjectMapperVersion)
                |> Operations.insertTableObjectMapperVersion t
                |> ignore
                
                [ ({ Reference = mapper.Reference
                     MapperName = mapper.Name }
                  : Events.TableObjectMapperAddedEvent)
                  |> Events.TableObjectMapperAdded
                  ({ Reference = mapper.Version.Reference
                     MapperReference = mapper.Reference
                     Version = 1
                     Hash = hash
                     CreatedOnDateTime = timestamp }
                  : Events.TableObjectMapperVersionAddedEvent)
                  |> Events.TableObjectMapperVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create table object mapper"

    let tableObjectMapperVersion (ctx: MySqlContext) (logger: ILogger) (userReference: string) (mapperReference: string) (version: NewTableObjectMapperVersion) =
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
                let timestamp = getTimestamp ()
                let hash = version.MapperData.GetSHA256Hash()
                let versionNumber = mvr.Version + 1

                ({ Reference = version.Reference
                   TableObjectMapperId = mr.Id
                   Version = versionNumber
                   MapperJson = version.MapperData 
                   Hash = hash
                   CreatedOn = timestamp }
                : Parameters.NewTableObjectMapperVersion)
                |> Operations.insertTableObjectMapperVersion t
                |> ignore
                
                [ ({ Reference = version.Reference
                     MapperReference = mr.Reference
                     Version = versionNumber
                     Hash = hash
                     CreatedOnDateTime = timestamp }
                  : Events.TableObjectMapperVersionAddedEvent)
                  |> Events.TableObjectMapperVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create table object mapper version"