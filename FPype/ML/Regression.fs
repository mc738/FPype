namespace FPype.ML

[<RequireQualifiedAccess>]
module Regression =

    open System
    open System.Text.Json
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Data
    open FsToolbox.Core
    open FPype.Data.Models
    open FPype.Core.Types
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
        | Sdca of SdcaSettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "sdca" -> SdcaSettings.FromJson(element) |> Result.map TrainerType.Sdca
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

        member tt.GetName() =
            match tt with
            | Sdca _ -> "Scda"

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

    let metricsToString (modelName: string) (trainerType: TrainerType) (metrics: RegressionMetrics) =
        [ $"{modelName} metrics"
          ""
          "Model type: multiclass classification"
          $"Trainer type: {trainerType.GetName()}"
          $"Loss function: {metrics.LossFunction}"
          $"R squared: {metrics.RSquared}"
          $"Mean absolute error: {metrics.MeanAbsoluteError}"
          $"Mean squared error: {metrics.MeanSquaredError}"
          $"Root mean squared error: {metrics.RootMeanSquaredError}" ]
        |> String.concat Environment.NewLine

    let metricsToTable (modelName: string) (trainerType: TrainerType) (metrics: RegressionMetrics) =
        ({ Name = "__regression_metrics"
           Columns =
             [ { Name = "model"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "trainer_type"
                 Type = BaseType.String
                 ImportHandler = None }
               { Name = "loss_function"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "r_squared"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "mean_absolute_error"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "mean_squared_error"
                 Type = BaseType.Double
                 ImportHandler = None }
               { Name = "root_mean_squared_error"
                 Type = BaseType.Double
                 ImportHandler = None } ]
           Rows =
             [ ({ Values =
                   [ Value.String modelName
                     Value.String <| trainerType.GetName()
                     Value.Double metrics.LossFunction
                     Value.Double metrics.RSquared
                     Value.Double metrics.MeanAbsoluteError
                     Value.Double metrics.MeanSquaredError
                     Value.Double metrics.RootMeanSquaredError ] }: TableRow) ] }: TableModel)

    let train (mlCtx: MLContext) (modelSavePath: string) (settings: TrainingSettings) (dataUri: string) =
        try
            let trainingCtx = createTrainingContext mlCtx settings.General dataUri

            let trainer =
                match settings.TrainerType with
                | TrainerType.Sdca trainerSettings ->
                    mlCtx.Regression.Trainers.Sdca(
                        labelColumnName = trainerSettings.LabelColumnName,
                        featureColumnName = trainerSettings.FeatureColumnName,
                        exampleWeightColumnName = (trainerSettings.ExampleWeightColumnName |> Option.defaultValue null),
                        l2Regularization = (trainerSettings.L2Regularization |> Option.toNullable),
                        l1Regularization = (trainerSettings.L1Regularization |> Option.toNullable),
                        maximumNumberOfIterations = (trainerSettings.MaximumNumberOfIterations |> Option.toNullable)
                    )
                    |> Internal.downcastPipeline

            let trainingPipeline = trainingCtx.Pipeline.Append(trainer)

            let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

            mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, modelSavePath)

            let predictions = trainedModel.Transform(trainingCtx.TestData)

            // TODO set strings are settings
            mlCtx.Regression.Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")
            |> Ok

        with ex ->
            Error $"Error training model - {ex.Message}"

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
