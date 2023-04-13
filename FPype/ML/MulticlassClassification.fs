namespace FPype.ML

open System
open System.Text.Json
open FPype.Data.Models
open FPype.Data.Store
open FsToolbox.Core

[<RequireQualifiedAccess>]
module MulticlassClassification =

    open FPype.Core.Types
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Data
    open FPype.Data.Store

    type TrainingSettings =
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

    and [<RequireQualifiedAccess>] TrainerType =
        | SdcaMaximumEntropy of SdcaMaximumEntropySettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "sdca-maximum-entropy" ->
                SdcaMaximumEntropySettings.FromJson(element)
                |> Result.map TrainerType.SdcaMaximumEntropy
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

        member tt.GetName() =
            match tt with
            | SdcaMaximumEntropy _ -> "Sdca maximum entropy"

    and SdcaMaximumEntropySettings =
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

    [<CLIMutable>]
    type PredictionItem =
        { [<ColumnName("PredictedLabel")>]
          PredictedLabel: string

          Score: float32 array }

    let metricsToString (modelName: string) (trainerType: TrainerType) (metrics: MulticlassClassificationMetrics) =
        [ $"{modelName} metrics"
          ""
          "Model type: multiclass classification"
          $"Trainer type: {trainerType.GetName()}"
          $"Confusion matrix: {metrics.ConfusionMatrix.GetFormattedConfusionTable()}"
          $"Log loss: {metrics.LogLoss}"
          $"Macro accuracy: {metrics.MacroAccuracy}"
          $"Micro accuracy: {metrics.MicroAccuracy}"
          $"Log loss reduction: {metrics.LogLossReduction}"
          $"Top K accuracy: {metrics.TopKAccuracy}"
          $"Per class log loss: {floatSeqToString metrics.PerClassLogLoss}"
          $"Top K prediction count: {metrics.TopKPredictionCount}"
          $"Top K accuracy for all K: {floatSeqToString metrics.TopKAccuracyForAllK}" ]
        |> String.concat Environment.NewLine

    let metricsToTable (modelName: string) (trainerType: TrainerType) (metrics: MulticlassClassificationMetrics) =
        ({ Name = "__multiclass_classification_metrics"
           Columns =
             [ { Name = "model"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "trainer_type"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "confusion_matrix"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "log_loss"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "macro_accuracy"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "micro_accuracy"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "top_k_accuracy"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "per_class_log_loss"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "top_k_prediction_count"
                 Type = BaseType.Int
                 ImportHandler = None }
               { Name = "top_k_accuracy_for_all_k"
                 Type = BaseType.String
                 ImportHandler = None } ]
           Rows =
             [ ({ Values =
                   [ Value.String modelName
                     Value.String <| trainerType.GetName()
                     Value.String <| metrics.ConfusionMatrix.GetFormattedConfusionTable()
                     Value.Double metrics.LogLoss
                     Value.Double metrics.MacroAccuracy
                     Value.Double metrics.MicroAccuracy
                     Value.Double metrics.LogLossReduction
                     Value.Double metrics.TopKAccuracy
                     Value.String <| floatSeqToString metrics.PerClassLogLoss
                     Value.Int metrics.TopKPredictionCount
                     Value.String <| floatSeqToString metrics.TopKAccuracyForAllK ] }: TableRow) ] }: TableModel)

    let train (mlCtx: MLContext) (store: PipelineStore) (settings: TrainingSettings) =
        getDataSourceUri store settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri

                let trainer =
                    match settings.TrainerType with
                    | TrainerType.SdcaMaximumEntropy trainerSettings ->
                        mlCtx.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                            labelColumnName = trainerSettings.LabelColumnName,
                            featureColumnName = trainerSettings.FeatureColumnName,
                            exampleWeightColumnName =
                                (trainerSettings.ExampleWeightColumnName |> Option.defaultValue null),
                            l2Regularization = (trainerSettings.L2Regularization |> Option.toNullable),
                            l1Regularization = (trainerSettings.L1Regularization |> Option.toNullable),
                            maximumNumberOfIterations = (trainerSettings.MaximumNumberOfIterations |> Option.toNullable)
                        )
                        |> Internal.downcastPipeline

                // TODO set string are settings
                let trainingPipeline =
                    trainingCtx
                        .Pipeline
                        .AppendCacheCheckpoint(mlCtx)
                        .Append(trainer)
                        .Append(mlCtx.Transforms.Conversion.MapKeyToValue("PredictedLabel"))

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, store.ExpandPath settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                mlCtx.MulticlassClassification.Evaluate(predictions) |> Ok

            with ex ->
                Error $"Error training model - {ex.Message}")

    let load (mlCtx: MLContext) (store: PipelineStore) (path: string) =
        try
            match mlCtx.Model.Load(store.ExpandPath path) with
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
