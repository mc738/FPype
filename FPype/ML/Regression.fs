namespace FPype.ML

open System.Text.Json
open FsToolbox.Core

[<RequireQualifiedAccess>]
module Regression =

    open FPype.Core.Types
    open FPype.Data.Store
    open Microsoft.FSharp.Core
    open Microsoft.ML
    
    type TrainingSettings =
        { General: GeneralTrainingSettings
          TrainerType: TrainerType }
        
        static member FromJson(element: JsonElement, store: PipelineStore) =
            match
                Json.tryGetProperty "general" element
                |> Option.map (fun el -> GeneralTrainingSettings.FromJson(el, store))
                |> Option.defaultWith (fun _ -> Error "Missing `general` property"),
                Json.tryGetProperty "trainer" element
                |> Option.map TrainerType.FromJson
                |> Option.defaultWith (fun _ -> Error "Missing `trainer` property")
            with
            | Ok gts, Ok ts -> { General = gts; TrainerType = ts } |> Ok
            | Error e, _ -> Error e
            | _, Error e -> Error e
        
    and [<RequireQualifiedAccess>] TrainerType =
        | Sdca of SdcaSettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "sdca" ->
                SdcaSettings.FromJson(element)
                |> Result.map TrainerType.Sdca
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

    and SdcaSettings =
        { LabelColumnName: string
          FeatureColumnName: string
          ExampleWeightColumnName: string option
          L2Regularization: float32 option
          L1Regularization: float32 option
          MaximumNumberOfIterations: int option }

        static member Default() =
            { LabelColumnName = "Label"
              FeatureColumnName = "Features"
              ExampleWeightColumnName = None
              L2Regularization = None
              L1Regularization = None
              MaximumNumberOfIterations = None }

        static member FromJson(element: JsonElement) =
            match
                Json.tryGetStringProperty "labelColumnName" element,
                Json.tryGetStringProperty "featureColumnName" element
            with
            | Some lcn, Some fcn ->
                { LabelColumnName = lcn
                  FeatureColumnName = fcn
                  ExampleWeightColumnName = Json.tryGetStringProperty "exampleWeightColumnName" element
                  L2Regularization = Json.tryGetSingleProperty "l2Regularization" element
                  L1Regularization = Json.tryGetSingleProperty "l1Regularization" element
                  MaximumNumberOfIterations = Json.tryGetIntProperty "maximumNumberOfIterations" element }
                |> Ok
            | None, _ -> Error "Missing `labelColumnName` property"
            | _, None -> Error "Missing `featureColumnName` property"

    and [<CLIMutable>] PredictionItem = { Score: float32 }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri
                
                // TODO set strings are settings
                let trainer =
                    match settings.TrainerType with
                    | TrainerType.Sdca trainerSettings ->
                        mlCtx.Regression.Trainers.Sdca(
                            labelColumnName = trainerSettings.LabelColumnName,
                            featureColumnName = trainerSettings.FeatureColumnName,
                            exampleWeightColumnName =
                                (trainerSettings.ExampleWeightColumnName |> Option.defaultValue null),
                            l2Regularization = (trainerSettings.L2Regularization |> Option.toNullable),
                            l1Regularization = (trainerSettings.L1Regularization |> Option.toNullable),
                            maximumNumberOfIterations = (trainerSettings.MaximumNumberOfIterations |> Option.toNullable))
                        |> Internal.downcastPipeline
                
                let trainingPipeline = trainingCtx.Pipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                // TODO set strings are settings
                mlCtx.Regression.Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")
                |> Ok

            with ex ->
                Error $"Error training model - {ex.Message}")

    let load (mlCtx: MLContext) (path: string) =
        try
            match mlCtx.Model.Load(path) with
            | (m, t) -> Ok(m, t)
        with ex ->
            Error ex.Message

    let predict (mlCtx: MLContext) (model: ITransformer) (schema: DataViewSchema) (value: Map<string, Value>) =
        // NOTE - this uses internal use code, which forgoes various checks.
        let runTimeType = Common.Internal.createRunTimeType schema

        let engine =
            Common.Internal.getDynamicPredictionEngine<PredictionItem> mlCtx runTimeType schema model

        Common.Internal.runDynamicPredictionEngine<PredictionItem>
            runTimeType
            engine
            (Common.ClassFactory.createObjectFromType runTimeType value)
