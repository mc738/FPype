namespace FPype.ML

open System
open System.Text.Json
open FPype.Data.Models
open FPype.Data.Store
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

        static member FromJson(element: JsonElement) =
            match
                Json.tryGetProperty "general" element
                |> Option.map GeneralTrainingSettings.FromJson
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

        member tt.GetName() =
            match tt with
            | SdcaLogisticRegression _ -> "Sdca logistic regression"

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


    let metricsToString
        (modelName: string)
        (trainerType: TrainerType)
        (metrics: CalibratedBinaryClassificationMetrics)
        =
        [ $"{modelName} metrics"
          ""
          "Model type: binary classification"
          $"Trainer type: {trainerType.GetName()}"
          $"Accuracy: {metrics.Accuracy}"
          $"Entropy: {metrics.Entropy}"
          $"Entropy: {metrics.Entropy}"
          $"Confusion matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable}"
          $"F1 score: {metrics.F1Score}"
          $"Log loss: {metrics.LogLoss}"
          $"Negative precision: {metrics.NegativePrecision}"
          $"Negative recall: {metrics.NegativeRecall}"
          $"Positive precision: {metrics.PositivePrecision}"
          $"Positive recall: {metrics.PositiveRecall}"
          $"Log loss reduction: {metrics.LogLossReduction}"
          $"Area under roc curve: {metrics.AreaUnderRocCurve}"
          $"Area under precision recall curve: {metrics.AreaUnderPrecisionRecallCurve}" ]
        |> String.concat Environment.NewLine

    let metricsToTable (modelName: string) (trainerType: TrainerType) (metrics: CalibratedBinaryClassificationMetrics) =
        ({ Name = "__calibrated_binary_classification_metrics"
           Columns =
             [ { Name = "model"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "trainer_type"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "accuracy"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "entropy"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "confusion_matrix"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "f1_score"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "log_loss"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "negative_precision"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "negative_recall"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "positive_precision"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "positive_recall"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "log_loss_reduction"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "area_under_roc_curve"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "area_under_precision_recall_curve"
                 Type = BaseType.Double
                 ImportHandler = None } ]
           Rows =
             [ ({ Values =
                   [ Value.String modelName
                     Value.String <| trainerType.GetName()
                     Value.Double metrics.Accuracy
                     Value.Double metrics.Entropy
                     Value.String <| metrics.ConfusionMatrix.GetFormattedConfusionTable()
                     Value.Double metrics.F1Score
                     Value.Double metrics.LogLoss
                     Value.Double metrics.NegativePrecision
                     Value.Double metrics.NegativeRecall
                     Value.Double metrics.PositivePrecision
                     Value.Double metrics.PositiveRecall
                     Value.Double metrics.LogLossReduction
                     Value.Double metrics.AreaUnderRocCurve
                     Value.Double metrics.AreaUnderPrecisionRecallCurve ] }: TableRow) ] }: TableModel)

    let train (mlCtx: MLContext) (store: PipelineStore) (settings: TrainingSettings) =
        getDataSourceUri store settings.General.DataSource
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

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, store.ExpandPath settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                mlCtx.BinaryClassification.Evaluate(
                    data = predictions,
                    labelColumnName = "Label",
                    scoreColumnName = "Score"
                )
                |> Ok
            with ex ->
                Error $"Error training model - {ex.Message}")

    let load (mlCtx: MLContext) (store: PipelineStore) (path: string) =
        try
            match mlCtx.Model.Load(store.ExpandPath path) with
            | (m, _) ->
                mlCtx.Model.CreatePredictionEngine<TextClassificationItem, TextPredictionItem>(m)
                |> Ok
        with ex ->
            Error ex.Message

    let predict (engine: PredictionEngine<TextClassificationItem, TextPredictionItem>) (text: string) =
        engine.Predict({ Text = text; Label = true })
