namespace FPype.Infrastructure.Configuration.Resources

open Armarium

module StoreOperations =

    open Freql.MySql
    open FsToolbox.Core.Results
    open FPype.Configuration
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core

    let addResource
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (subscription: Records.Subscription)
        (resourceReference: string)
        =
        Fetch.resource ctx resourceReference
        |> FetchResult.toResult
        |> Result.bind (fun r ->
            let verifiers =
                [ Verification.subscriptionMatches subscription r.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers r)
        |> Result.bind (fun r ->

            match store.AddResource(r.Name) with
            | Ok _ -> Ok()
            | Error e ->
                ({ Message = e
                   DisplayMessage = $"Failed to add resource `{r.Name}` to configuration store"
                   Exception = None }
                : FailureResult)
                |> Error)
        |> ActionResult.fromResult

    let addResourceVersion
        (ctx: MySqlContext)
        (store: ConfigurationStore)
        (fileRepo: FileRepository)
        (readArgs: FileReadOperationArguments)
        (subscription: Records.Subscription)
        (versionReference: string)
        =
        Fetch.resourceVersionByReference ctx versionReference
        |> FetchResult.merge (fun rv r -> r, rv) (fun rv -> Fetch.resourceById ctx rv.ResourceId)
        |> FetchResult.toResult
        |> Result.bind (fun (r, rv) ->
            let verifiers =
                [ Verification.subscriptionMatches subscription r.SubscriptionId
                  // This is has likely already been checked.
                  // But can't hurt to check here again just in case.
                  Verification.subscriptionIsActive subscription ]

            VerificationResult.verify verifiers (r, rv))

        |> Result.bind (fun (r, rv) ->
            match fileRepo.ReadAllBytes(rv.ResourcePath, readArgs) with
            | ActionResult.Success raw ->
                match
                    store.AddResourceVersion(
                        IdType.Specific rv.Reference,
                        r.Name,
                        rv.ResourceType,
                        raw,
                        ItemVersion.Specific rv.Version
                    )
                with
                | Ok _ -> Ok()
                | Error e ->
                    ({ Message = e
                       DisplayMessage =
                         $"Failed to add resource version `{rv.Reference}` ({r.Name}) to configuration store"
                       Exception = None }
                    : FailureResult)
                    |> Error
            | ActionResult.Failure f -> Error f)
        |> ActionResult.fromResult
