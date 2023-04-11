namespace FPype.ML

open System.Text.Json
open FsToolbox.Core

[<RequireQualifiedAccess>]
module BinaryClassification =

    open FPype.Core.Types
    open FPype.Data
    open FPype.Data.Store
    open Microsoft.ML
    open Microsoft.ML.Data

    type TextClassificationItem = { Text: string; Label: bool }

    and [<RequireQualifiedAccess>] ClassificationType = | Text

    and TrainingSettings =
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

    and [<CLIMutable>] TextPredictionItem =
        { [<ColumnName("PredictedLabel")>]
          Prediction: bool
          Probability: float32
          Score: float32 }

    and [<RequireQualifiedAccess>] TrainerType =
        | SdcaLogisticRegression of SdcaLogisticRegressionSettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "sdca-logistic-regression" ->
                SdcaLogisticRegressionSettings.FromJson(element)
                |> Result.map TrainerType.SdcaLogisticRegression
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

    and SdcaLogisticRegressionSettings =
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


    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri

                (*
                let dataProcessPipeline =
                    match settings.ClassificationType with
                    | ClassificationType.Text ->
                        mlCtx.Transforms.Text.FeaturizeText(
                            outputColumnName = "Features",
                            inputColumnName = featureColName
                        )
                *)

                let trainer =
                    match settings.TrainerType with
                    | TrainerType.SdcaLogisticRegression trainerSettings ->
                        mlCtx.BinaryClassification.Trainers.SdcaLogisticRegression(
                            labelColumnName = trainerSettings.LabelColumnName,
                            featureColumnName = trainerSettings.FeatureColumnName,
                            exampleWeightColumnName =
                                (trainerSettings.ExampleWeightColumnName |> Option.defaultValue null),
                            l2Regularization = (trainerSettings.L2Regularization |> Option.toNullable),
                            l1Regularization = (trainerSettings.L1Regularization |> Option.toNullable),
                            maximumNumberOfIterations = (trainerSettings.MaximumNumberOfIterations |> Option.toNullable)
                        )
                        |> Internal.downcastPipeline


                let trainingPipeline = trainingCtx.Pipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                mlCtx.BinaryClassification.Evaluate(
                    data = predictions,
                    labelColumnName = "Label",
                    scoreColumnName = "Score"
                )
                |> Ok
            with ex ->
                Error $"Error training model - {ex.Message}")

    let load (mlCtx: MLContext) (path: string) =
        try
            match mlCtx.Model.Load(path) with
            | (m, _) ->
                mlCtx.Model.CreatePredictionEngine<TextClassificationItem, TextPredictionItem>(m)
                |> Ok
        with ex ->
            Error ex.Message

    let predict (engine: PredictionEngine<TextClassificationItem, TextPredictionItem>) (text: string) =
        engine.Predict({ Text = text; Label = true })
