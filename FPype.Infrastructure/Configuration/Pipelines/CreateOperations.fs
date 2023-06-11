namespace FPype.Infrastructure.Configuration.Pipelines

open System.Text
open FsToolbox.Core
open Microsoft.Extensions.Logging

[<RequireQualifiedAccess>]
module CreateOperations =

    open System
    open FPype.Infrastructure.Core
    open FsToolbox.Core.Results
    open FsToolbox.Extensions
    open Freql.MySql
    open FPype.Infrastructure.Core.Persistence
    open FPype.Infrastructure.Configuration.Common

    let pipeline (ctx: MySqlContext) (logger: ILogger) (userReference: string) (pipeline: NewPipeline) =
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

                let timestamp = getTimestamp ()

                let pipelineId =
                    ({ Reference = pipeline.Name
                       SubscriptionId = sr.Id
                       Name = pipeline.Name }
                    : Parameters.NewPipeline)
                    |> Operations.insertPipeline t
                    |> int

                let versionId =
                    ({ Reference = pipeline.Version.Reference
                       PipelineId = pipelineId
                       Version = 1
                       Description = pipeline.Version.Description
                       CreatedOn = timestamp }
                    : Parameters.NewPipelineVersion)
                    |> Operations.insertPipelineVersion t
                    |> int

                let actionEvents =
                    pipeline.Version.Actions
                    |> List.choose (fun a ->
                        match typeMap.TryFind(a.ActionType.ToLower()) with
                        | Some atId ->
                            let hash = a.ActionData.GetSHA256Hash()
                            let step = a.Step

                            ({ Reference = a.Reference
                               PipelineVersionId = versionId
                               Name = a.Name
                               ActionTypeId = atId
                               ActionJson = a.ActionData
                               Hash = hash
                               Step = a.Step }
                            : Parameters.NewPipelineAction)
                            |> Operations.insertPipelineAction t
                            |> ignore

                            ({ Reference = a.Reference
                               VersionReference = pipeline.Version.Reference
                               ActionName = a.Name
                               ActionType = a.ActionType
                               Hash = hash
                               Step = step }
                            : Events.PipelineActionAddedEvent)
                            |> Events.PipelineActionAdded
                            |> Some
                        | None ->
                            // TODO what to do if action type not found?
                            None)

                [ ({ Reference = pipeline.Reference
                     PipelineName = pipeline.Name }
                  : Events.PipelineAddedEvent)
                  |> Events.PipelineAdded

                  ({ Reference = pipeline.Version.Reference
                     PipelineReference = pipeline.Reference
                     Version = 1
                     Description = pipeline.Version.Description
                     CreatedOnDateTime = timestamp }
                  : Events.PipelineVersionAddedEvent)
                  |> Events.PipelineVersionAdded
                  yield! actionEvents ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create pipeline"

    let pipelineVersion
        (ctx: MySqlContext)
        (logger: ILogger)
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
                let timestamp = getTimestamp ()

                let typeMap =
                    Operations.selectActionTypeRecords t [] []
                    |> List.map (fun atr -> atr.Name, atr.Id)
                    |> Map.ofList

                let versionNumber = pvr.Version + 1

                let versionId =
                    ({ Reference = version.Reference
                       PipelineId = pr.Id
                       Version = versionNumber
                       Description = version.Description
                       CreatedOn = timestamp }
                    : Parameters.NewPipelineVersion)
                    |> Operations.insertPipelineVersion t

                let actionEvents =
                    version.Actions
                    |> List.choose (fun a ->
                        match typeMap.TryFind(a.ActionType.ToLower()) with
                        | Some atId ->
                            let hash = a.ActionData.GetSHA256Hash()

                            ({ Reference = a.Reference
                               PipelineVersionId = int versionId
                               Name = a.Name
                               ActionTypeId = atId
                               ActionJson = a.ActionData
                               Hash = hash
                               Step = a.Step }
                            : Parameters.NewPipelineAction)
                            |> Operations.insertPipelineAction t
                            |> ignore

                            ({ Reference = a.Reference
                               VersionReference = version.Reference
                               ActionName = a.Name
                               ActionType = a.ActionType
                               Hash = hash
                               Step = a.Step }
                            : Events.PipelineActionAddedEvent)
                            |> Events.PipelineActionAdded
                            |> Some
                        | None ->
                            // TODO what to do if action type not found?
                            None)

                [ ({ Reference = version.Reference
                     PipelineReference = pr.Reference
                     Version = versionNumber
                     Description = version.Description
                     CreatedOnDateTime = timestamp }
                  : Events.PipelineVersionAddedEvent)
                  |> Events.PipelineVersionAdded
                  yield! actionEvents ]
                |> Events.addEvents t logger sr.Id ur.Id timestamp
                |> ignore))
        |> toActionResult "Create pipeline version"

    let pipelineActions
        (ctx: MySqlContext)
        (logger: ILogger)
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
                    let hash = action.ActionData.GetSHA256Hash()
                    let timestamp = getTimestamp ()
                    
                    ({ Reference = action.Reference
                       PipelineVersionId = pvr.Id
                       Name = action.Name
                       ActionTypeId = atr.Id
                       ActionJson = action.ActionData
                       Hash = hash
                       Step = action.Step }
                    : Parameters.NewPipelineAction)
                    |> Operations.insertPipelineAction t
                    |> ignore

                    [ ({ Reference = action.Reference
                         VersionReference = action.Reference
                         ActionName = action.Name
                         ActionType = action.ActionType
                         Hash = hash
                         Step = action.Step }
                      : Events.PipelineActionAddedEvent)
                      |> Events.PipelineActionAdded ]
                    |> Events.addEvents t logger sr.Id ur.Id timestamp
                    |> ignore
                | None ->
                    // TODO how to handling missing action type?
                    ()))
        |> toActionResult "Create pipeline action"
