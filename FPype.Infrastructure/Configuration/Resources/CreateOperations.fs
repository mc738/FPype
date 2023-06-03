namespace FPype.Infrastructure.Configuration.Resources

[<RequireQualifiedAccess>]
module CreateOperations =
    
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let resource (ctx: MySqlContext) (userReference: string) (resource: NewResource) =
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

                let resourceId =
                    ({ Reference = resource.Reference
                       SubscriptionId = sr.Id
                       Name = resource.Name }
                    : Parameters.NewResource)
                    |> Operations.insertResource t
                    |> int

                ({ Reference = resource.Version.Reference
                   ResourceId = resourceId
                   Version = 1
                   ResourceType = resource.Version.Type
                   ResourcePath =  resource.Version.Path
                   Hash = resource.Version.Hash
                   CreatedOn = timestamp () }
                : Parameters.NewResourceVersion)
                |> Operations.insertResourceVersion t
                |> ignore))
        |> toActionResult "Create resource"

    let resourceVersion (ctx: MySqlContext) (userReference: string) (resourceReference: string) (version: NewResourceVersion) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource t resourceReference)
            |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
                Fetch.resourceLatestVersion t rr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, rr, rvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur rr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, rr, rvr))
            // Create
            |> Result.map (fun (ur, sr, rr, rvr) ->

                ({ Reference = version.Reference
                   ResourceId = rr.Id
                   Version = rvr.Version + 1
                   ResourceType = version.Type
                   ResourcePath = version.Path
                   Hash = version.Hash
                   CreatedOn = timestamp () }
                : Parameters.NewResourceVersion)
                |> Operations.insertResourceVersion t
                |> ignore))
        |> toActionResult "Create resource version"
