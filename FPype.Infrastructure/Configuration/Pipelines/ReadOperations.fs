namespace FPype.Infrastructure.Configuration.Pipelines

open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module ReadOperations =

    open FPype.Infrastructure.Configuration.Common
    open FPype.Infrastructure.Core
    open FPype.Infrastructure.Core.Persistence
    open Freql.MySql
    open FsToolbox.Core.Results

    let latestPipelineVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (pipelineReference: string)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline ctx pipelineReference)
        |> FetchResult.merge (fun (ur, sr, pr) pvr -> ur, sr, pr, pvr) (fun (_, _, pr) ->
            Fetch.pipelineLatestVersion ctx pr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, pr, pvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur pr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, pr, pvr))
        // Map
        |> Result.map (fun (ur, sr, pr, pvr) ->
            let typeMap =
                Operations.selectActionTypeRecords ctx [] []
                |> List.map (fun atr -> atr.Id, atr.Name)
                |> Map.ofList

            match Fetch.pipelineActions ctx pvr.Id with
            | FetchResult.Success ars ->
                ({ PipelineReference = pr.Reference
                   VersionReference = pvr.Reference
                   Name = pr.Name
                   Description = pvr.Description
                   Version = pvr.Version
                   CreatedOn = pvr.CreatedOn 
                   Actions =
                     ars
                     |> List.choose (fun ar ->
                         typeMap.TryFind ar.ActionTypeId
                         |> Option.map (fun atn ->
                             ({ Reference = ar.Reference
                                Name = ar.Name
                                ActionType = atn
                                ActionData = ar.ActionJson
                                Hash = ar.Hash
                                Step = ar.Step }
                             : PipelineActionDetails))) }
                : PipelineVersionDetails)
                |> Some
            | FetchResult.Failure fr -> None)
        |> optionalToFetchResult "Latest pipeline version"

    let specificPipelineVersion
        (ctx: MySqlContext)
        (logger: ILogger)
        (userReference: string)
        (pipelineReference: string)
        (version: int)
        =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline ctx pipelineReference)
        |> FetchResult.merge (fun (ur, sr, pr) pvr -> ur, sr, pr, pvr) (fun (_, _, pr) ->
            Fetch.pipelineVersion ctx pr.Id version)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, pr, pvr) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur pr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, pr, pvr))
        // Map
        |> Result.map (fun (ur, sr, pr, pvr) ->
            let typeMap =
                Operations.selectActionTypeRecords ctx [] []
                |> List.map (fun atr -> atr.Id, atr.Name)
                |> Map.ofList

            match Fetch.pipelineActions ctx pvr.Id with
            | FetchResult.Success ars ->
                ({ PipelineReference = pr.Reference
                   VersionReference = pvr.Reference
                   Name = pr.Name
                   Description = pvr.Description
                   Version = pvr.Version
                   CreatedOn = pvr.CreatedOn 
                   Actions =
                     ars
                     |> List.choose (fun ar ->
                         typeMap.TryFind ar.ActionTypeId
                         |> Option.map (fun atn ->
                             ({ Reference = ar.Reference
                                Name = ar.Name
                                ActionType = atn
                                ActionData = ar.ActionJson
                                Hash = ar.Hash
                                Step = ar.Step }
                             : PipelineActionDetails))) }
                : PipelineVersionDetails)
                |> Some
            | FetchResult.Failure fr -> None)
        |> optionalToFetchResult "Specific pipeline version"

    let pipelineVersions (ctx: MySqlContext) (logger: ILogger) (userReference: string) (pipelineReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline ctx pipelineReference)
        |> FetchResult.merge (fun (ur, sr, pr) pvr -> ur, sr, pr, pvr) (fun (_, _, pr) ->
            Fetch.pipelineVersionsByPipelineId ctx pr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, pr, pvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.userSubscriptionMatches ur pr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, pr, pvrs))
        // Map
        |> Result.map (fun (ur, sr, pr, pvrs) ->
            pvrs
            |> List.map (fun pvr ->
                ({ PipelineReference = pr.Reference
                   VersionReference = pvr.Reference
                   Name = pr.Name
                   Description = pvr.Description
                   Version = pvr.Version }
                : PipelineVersionOverview)))
        |> FetchResult.fromResult

    let pipelines (ctx: MySqlContext) (logger: ILogger) (userReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.merge (fun (ur, sr) prs -> ur, sr, prs) (fun (_, sr) ->
            Fetch.pipelinesBySubscriptionId ctx sr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, prs) ->
            let verifiers =
                [ Verification.userIsActive ur; Verification.subscriptionIsActive sr ]

            VerificationResult.verify verifiers (ur, sr, prs))
        // Map
        |> Result.map (fun (ur, sr, prs) ->
            prs
            |> List.map (fun pr ->
                ({ Reference = pr.Reference
                   Name = pr.Name }
                : PipelineOverview)))
        |> FetchResult.fromResult

    let pipeline (ctx: MySqlContext) (logger: ILogger) (userReference: string) (pipelineReference: string) =
        Fetch.user ctx userReference
        |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById ctx ur.Id)
        |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline ctx pipelineReference)
        |> FetchResult.merge (fun (ur, sr, pr) pvrs -> ur, sr, pr, pvrs) (fun (_, _, pr) ->
            Fetch.pipelineVersionsByPipelineId ctx pr.Id)
        |> FetchResult.toResult
        // Verify
        |> Result.bind (fun (ur, sr, pr, pvrs) ->
            let verifiers =
                [ Verification.userIsActive ur
                  Verification.subscriptionIsActive sr
                  Verification.subscriptionMatches sr pr.SubscriptionId ]

            VerificationResult.verify verifiers (ur, sr, pr, pvrs))
        // Map
        |> Result.map (fun (ur, sr, pr, pvrs) ->
            let typeMap =
                Operations.selectActionTypeRecords ctx [] []
                |> List.map (fun atr -> atr.Id, atr.Name)
                |> Map.ofList
                
            // TODO what to do if this fails?
            let versions =
                pvrs
                |> List.choose (fun pvr ->
                    match Fetch.pipelineActions ctx pvr.Id with
                    | FetchResult.Success ars ->
                        ({ PipelineReference = pr.Reference
                           VersionReference = pvr.Reference
                           Name = pr.Name
                           Description = pvr.Description
                           Version = pvr.Version
                           CreatedOn = pvr.CreatedOn 
                           Actions =
                             ars
                             |> List.choose (fun ar ->
                                 typeMap.TryFind ar.ActionTypeId
                                 |> Option.map (fun atn ->
                                     ({ Reference = ar.Reference
                                        Name = ar.Name
                                        ActionType = atn
                                        ActionData = ar.ActionJson
                                        Hash = ar.Hash
                                        Step = ar.Step }
                                     : PipelineActionDetails))) }
                        : PipelineVersionDetails)
                        |> Some
                    | FetchResult.Failure fr -> None)
            
            ({ Reference = pr.Reference
               Name = pr.Name
               Versions = versions }
            : PipelineDetails))
        |> FetchResult.fromResult