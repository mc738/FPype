﻿namespace FPype.Infrastructure.Configuration.Pipelines

open System.Text
open FsToolbox.Core

[<RequireQualifiedAccess>]
module CreateOperations =

    open System
    open FPype.Infrastructure.Core
    open FsToolbox.Core.Results
    open FsToolbox.Extensions
    open Freql.MySql
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common

    let pipeline (ctx: MySqlContext) (userReference: string) (pipeline: NewPipeline) =
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
                let typeMap =
                    Operations.selectActionTypeRecords t [] []
                    |> List.map (fun atr -> atr.Name, atr.Id)
                    |> Map.ofList

                let pipelineId =
                    ({ Reference = pipeline.Name
                       SubscriptionId = sr.Id
                       Name = pipeline.Name }
                    : Parameters.NewPipeline)
                    |> Operations.insertPipeline t
                    |> int

                let versionId =
                    ({ Reference = createReference ()
                       PipelineId = pipelineId
                       Version = 1
                       Description = pipeline.Version.Description
                       CreatedOn = timestamp () }
                    : Parameters.NewPipelineVersion)
                    |> Operations.insertPipelineVersion t
                    |> int

                pipeline.Version.Actions
                |> List.iter (fun a ->
                    match typeMap.TryFind(a.ActionType.ToLower()) with
                    | Some atId ->
                        ({ Reference = a.Reference
                           PipelineVersionId = versionId
                           Name = a.Name
                           ActionTypeId = atId
                           ActionJson = a.ActionData
                           Hash = a.ActionData.GetSHA256Hash()
                           Step = 1 }
                        : Parameters.NewPipelineAction)
                        |> Operations.insertPipelineAction t
                        |> ignore
                    | None ->
                        // TODO what to do if action type not found?
                        ())))
        |> toActionResult "Create pipeline"

    let pipelineVersion
        (ctx: MySqlContext)
        (userReference: string)
        (pipelineReference: string)
        (version: NewPipelineVersion)
        =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain (fun (ur, sr) pr -> ur, sr, pr) (Fetch.pipeline t pipelineReference)
            |> FetchResult.merge (fun (ur, sr, pr) pvr -> ur, sr, pr, pvr) (fun (_, _, pr) ->
                Fetch.pipelineLatestVersion t pr.Id)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, pr, pvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur pr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr))
            // Create
            |> Result.map (fun (ur, sr, pr, pvr) ->
                let typeMap =
                    Operations.selectActionTypeRecords t [] []
                    |> List.map (fun atr -> atr.Name, atr.Id)
                    |> Map.ofList

                let versionId =
                    ({ Reference = version.Reference
                       PipelineId = pr.Id
                       Version = pvr.Version + 1
                       Description = version.Description
                       CreatedOn = timestamp () }
                    : Parameters.NewPipelineVersion)
                    |> Operations.insertPipelineVersion t

                version.Actions
                |> List.iter (fun a ->
                    match typeMap.TryFind(a.ActionType.ToLower()) with
                    | Some atId ->
                        ({ Reference = a.Reference
                           PipelineVersionId = int versionId
                           Name = a.Name
                           ActionTypeId = atId
                           ActionJson = a.ActionData
                           Hash = a.ActionData.GetSHA256Hash()
                           Step = 1 }
                        : Parameters.NewPipelineAction)
                        |> Operations.insertPipelineAction t
                        |> ignore
                    | None ->
                        // TODO what to do if action type not found?
                        ())))
        |> toActionResult "Create pipeline version"

    let pipelineActions
        (ctx: MySqlContext)
        (userReference: string)
        (versionReference: string)
        (action: NewPipelineAction)
        =
        ctx.ExecuteInTransaction(fun t ->
            // Fetch
            Fetch.user t userReference
            |> FetchResult.merge (fun ur sr -> ur, sr) (fun ur -> Fetch.subscriptionById t ur.Id)
            |> FetchResult.chain
                (fun (ur, sr) pvr -> ur, sr, pvr)
                (Fetch.pipelineVersionByReference t versionReference)
            |> FetchResult.merge (fun (ur, sr, pvr) pr -> ur, sr, pr, pvr) (fun (_, _, pvr) ->
                Fetch.pipelineById t pvr.PipelineId)
            |> FetchResult.toResult
            // Verify
            |> Result.bind (fun (ur, sr, pr, pvr) ->
                let verifiers =
                    [ Verification.userIsActive ur
                      Verification.subscriptionIsActive sr
                      Verification.userSubscriptionMatches ur pr.SubscriptionId ]

                VerificationResult.verify verifiers (ur, sr, pr, pvr))
            |> Result.map (fun (ur, sr, pr, pvr) ->
                match Operations.selectActionTypeRecord t [ "WHERE name = @0" ] [ action.ActionType.ToLower() ] with
                | Some atr ->
                    ({ Reference = action.Reference
                       PipelineVersionId = pvr.Id
                       Name = action.Name
                       ActionTypeId = atr.Id
                       ActionJson = action.ActionData
                       Hash = ""
                       Step = action.Step }
                    : Parameters.NewPipelineAction)
                    |> Operations.insertPipelineAction t
                    |> ignore
                | None ->
                    // TODO how to handling missing action type?
                    ()))
        |> toActionResult "Create pipeline action"