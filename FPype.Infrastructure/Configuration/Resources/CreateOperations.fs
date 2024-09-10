namespace FPype.Infrastructure.Configuration.Resources

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Core.Results

    let resource (ctx: MySqlContext) (logger: ILogger) (userReference: string) (resource: NewResource) =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr))
            // Create
            |> Result.map (fun (ur, sr) ->
                let timestamp = getTimestamp ()

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
                   ResourcePath = resource.Version.Path
                   Hash = resource.Version.Hash
                   CreatedOn = timestamp }
                : Parameters.NewResourceVersion)
                |> Operations.insertResourceVersion t
                |> ignore

                [ ({ Reference = resource.Reference
                     ResourceName = resource.Name }
                  : Events.ResourceAddedEvent)
                  |> Events.ResourceAdded
                  ({ Reference = resource.Version.Reference
                     ResourceReference = resource.Reference
                     Version = 1
                     Hash = resource.Version.Hash
                     CreatedOnDateTime = timestamp }
                  : Events.ResourceVersionAddedEvent)
                  |> Events.ResourceVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create resource"

    let resourceVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (resourceReference: string)
        (version: NewResourceVersion)
        =
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
                      Verification.userSubscriptionMatches ur rr.SubscriptionId
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr, rr, rvr))
            // Create
            |> Result.map (fun (ur, sr, rr, rvr) ->
                let timestamp = getTimestamp ()
                let versionNumber = rvr.Version + 1

                ({ Reference = version.Reference
                   ResourceId = rr.Id
                   Version = versionNumber
                   ResourceType = version.Type
                   ResourcePath = version.Path
                   Hash = version.Hash
                   CreatedOn = timestamp }
                : Parameters.NewResourceVersion)
                |> Operations.insertResourceVersion t
                |> ignore

                [ ({ Reference = version.Reference
                     ResourceReference = rr.Reference
                     Version = versionNumber
                     Hash = version.Hash
                     CreatedOnDateTime = timestamp }
                  : Events.ResourceVersionAddedEvent)
                  |> Events.ResourceVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create resource version"
