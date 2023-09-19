namespace FPype.Infrastructure.Configuration.Queries

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module CreateOperations =

    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common
    open Freql.MySql
    open FsToolbox.Extensions
    open FsToolbox.Core.Results

    let rawQuery (ctx: MySqlContext) (logger: ILogger) (userReference: string) (query: NewRawQuery) =
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
                let hash = query.Version.RawQuery.GetSHA256Hash()

                let queryId =
                    ({ Reference = query.Reference
                       SubscriptionId = sr.Id
                       Name = query.Name }
                    : Parameters.NewQuery)
                    |> Operations.insertQuery t
                    |> int

                ({ Reference = query.Version.Reference
                   QueryId = queryId
                   Version = 1
                   RawQuery = query.Version.RawQuery
                   Hash = hash
                   IsSerialized = false 
                   CreatedOn = timestamp }
                : Parameters.NewQueryVersion)
                |> Operations.insertQueryVersion t
                |> ignore

                [ ({ Reference = query.Reference
                     QueryName = query.Name }
                  : Events.QueryAddedEvent)
                  |> Events.QueryAdded
                  ({ Reference = query.Version.Reference
                     QueryReference = query.Reference
                     Version = 1
                     Hash = hash
                     IsSerialized = false 
                     CreatedOnDateTime = timestamp }
                  : Events.QueryVersionAddedEvent)
                  |> Events.QueryVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create raw query"

    let serializedQuery (ctx: MySqlContext) (logger: ILogger) (userReference: string) (query: NewSerializedQuery) =
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
                let serializedQuery = query.Version.SerializedQuery.Serialize()
                
                let hash = serializedQuery.GetSHA256Hash()

                let queryId =
                    ({ Reference = query.Reference
                       SubscriptionId = sr.Id
                       Name = query.Name }
                    : Parameters.NewQuery)
                    |> Operations.insertQuery t
                    |> int

                ({ Reference = query.Version.Reference
                   QueryId = queryId
                   Version = 1
                   RawQuery = serializedQuery
                   Hash = hash
                   IsSerialized = true 
                   CreatedOn = timestamp }
                : Parameters.NewQueryVersion)
                |> Operations.insertQueryVersion t
                |> ignore

                [ ({ Reference = query.Reference
                     QueryName = query.Name }
                  : Events.QueryAddedEvent)
                  |> Events.QueryAdded
                  ({ Reference = query.Version.Reference
                     QueryReference = query.Reference
                     Version = 1
                     Hash = hash
                     IsSerialized = true
                     CreatedOnDateTime = timestamp }
                  : Events.QueryVersionAddedEvent)
                  |> Events.QueryVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create raw query"
    
    let rawQueryVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (queryReference: string)
        (version: NewRawQueryVersion)
        =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query t queryReference)
            |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
                Fetch.queryLatestVersion t qr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, qr, qvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur qr.SubscriptionId
                      // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                      Verification.isNotSystemSubscription sr
                      Verification.isNotSystemUser ur ]

                VerificationResult.verify verifiers (ur, sr, qr, qvr))
            // Create
            |> Result.map (fun (ur, sr, qr, qvr) ->
                let timestamp = getTimestamp ()
                let hash = version.RawQuery.GetSHA256Hash()
                let versionNumber = qvr.Version + 1

                ({ Reference = version.Reference
                   QueryId = qr.Id
                   Version = versionNumber
                   RawQuery = version.RawQuery
                   Hash = hash
                   IsSerialized = false 
                   CreatedOn = timestamp }
                : Parameters.NewQueryVersion)
                |> Operations.insertQueryVersion t
                |> ignore

                [ ({ Reference = version.Reference
                     QueryReference = qr.Reference
                     Version = versionNumber
                     Hash = hash
                     IsSerialized = false 
                     CreatedOnDateTime = timestamp }
                  : Events.QueryVersionAddedEvent)
                  |> Events.QueryVersionAdded ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp))
        |> toActionResult "Create raw query version"
        
    let serializedQueryVersion
            (ctx: MySqlContext)
            (logger: ILogger)
            (userReference: string)
            (queryReference: string)
            (version: NewSerializedQueryVersion)
            =
            ctx.ExecuteInTransaction(fun t ->
                // Fetch
                Fetch.user t userReference
                |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
                |> FetchResult.chain (fun (ur, sr) qr -> ur, sr, qr) (Fetch.query t queryReference)
                |> FetchResult.merge (fun (ur, sr, qr) qvr -> ur, sr, qr, qvr) (fun (_, _, qr) ->
                    Fetch.queryLatestVersion t qr.Id)
                |> FetchResult.toResult
                // Verify
                |> Result.bind (fun (ur, sr, qr, qvr) ->
                    let verifiers =
                        [ Verification.userIsActive ur
                          Verification.subscriptionIsActive sr
                          Verification.userSubscriptionMatches ur qr.SubscriptionId
                          // SECURITY These might not strictly be needed but a a good cover for regressions and ensure system users can not perform this operation.
                          Verification.isNotSystemSubscription sr
                          Verification.isNotSystemUser ur ]
    
                    VerificationResult.verify verifiers (ur, sr, qr, qvr))
                // Create
                |> Result.map (fun (ur, sr, qr, qvr) ->
                    let timestamp = getTimestamp ()
                    let serializedQuery = version.SerializedQuery.Serialize()
                    let hash = serializedQuery.GetSHA256Hash()
                    let versionNumber = qvr.Version + 1
    
                    ({ Reference = version.Reference
                       QueryId = qr.Id
                       Version = versionNumber
                       RawQuery = serializedQuery
                       Hash = hash
                       IsSerialized = false 
                       CreatedOn = timestamp }
                    : Parameters.NewQueryVersion)
                    |> Operations.insertQueryVersion t
                    |> ignore
    
                    [ ({ Reference = version.Reference
                         QueryReference = qr.Reference
                         Version = versionNumber
                         Hash = hash
                         IsSerialized = true
                         CreatedOnDateTime = timestamp }
                      : Events.QueryVersionAddedEvent)
                      |> Events.QueryVersionAdded ]
                    |> Events.addEvents t logger sr.Id ur.Id timestamp))
            |> toActionResult "Create serialized query version"
