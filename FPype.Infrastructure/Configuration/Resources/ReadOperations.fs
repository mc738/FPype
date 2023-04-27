namespace FPype.Infrastructure.Configuration.Resources

[<RequireQualifiedAccess>]
module ReadOperations =


    open FPype.Core.Types
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestResourceVersion (ctx: MySqlContext) (userReference: string) (resourceReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource ctx resourceReference)
        |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
            Fetch.resourceLatestVersion ctx rr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rr, rvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur rr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, rr, rvr))
        // Map
        |> Result.map (fun (ur, sr, rr, rvr) ->

            ({ Reference = rr.Reference
               Name = rr.Name
               Version =
                 { Reference = rvr.Reference
                   Version = rvr.Version
                   Type = rvr.ResourceType
                   Path = rvr.ResourcePath
                   Hash = rvr.Hash
                   CreatedOn = rvr.CreatedOn } }
            : ResourceDetails))
        |> FetchResult.fromResult

    let specificResourceVersion (ctx: MySqlContext) (userReference: string) (resourceReference: string) (version: int) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) rr -> ur, sr, rr) (Fetch.resource ctx resourceReference)
        |> FetchResult.merge (fun (ur, sr, rr) rvr -> ur, sr, rr, rvr) (fun (_, _, rr) ->
            Fetch.resourceVersion ctx rr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, rr, rvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur rr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, rr, rvr))
        // Map
        |> Result.map (fun (ur, sr, rr, rvr) ->

            ({ Reference = rr.Reference
               Name = rr.Name
               Version =
                 { Reference = rvr.Reference
                   Version = rvr.Version
                   Type = rvr.ResourceType
                   Path = rvr.ResourcePath
                   Hash = rvr.Hash
                   CreatedOn = rvr.CreatedOn } }
            : ResourceDetails))
        |> FetchResult.fromResult
