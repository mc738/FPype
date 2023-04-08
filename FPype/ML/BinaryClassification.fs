namespace FPype.ML

open FPype.Core.Types
open FPype.Data
open FPype.Data.Store
open Microsoft.ML
open Microsoft.ML.Data

[<RequireQualifiedAccess>]
module BinaryClassification =

    type TextClassificationItem = { Text: string; Label: bool }

    and [<CLIMutable>] TextPredictionItem =
        { [<ColumnName("PredictedLabel")>]
          Prediction: bool
          Probability: float32
          Score: float32 }

    and [<RequireQualifiedAccess>] ClassificationType = | Text

    and TrainingSettings =
        { DataSource: DataSource
          ModelSavePath: string
          HasHeaders: bool
          Separators: char array
          TrainingTestSplit: float
          ClassificationType: ClassificationType
          FeatureColumnIndex: int
          LabelColumnName: string
          LabelColumnIndex: int }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.DataSource
        |> Result.bind (fun uri ->
            try
                let options = TextLoader.Options()

                options.HasHeader <- settings.HasHeaders
                options.Separators <- settings.Separators

                let (featureColName, featureColType) =
                    match settings.ClassificationType with
                    | ClassificationType.Text -> "Text", DataKind.String

                options.Columns <-
                    [| TextLoader.Column(featureColName, featureColType, settings.FeatureColumnIndex)
                       TextLoader.Column(settings.LabelColumnName, DataKind.Boolean, settings.LabelColumnIndex) |]

                let loader = mlCtx.Data.CreateTextLoader(options = options)

                let dataView = loader.Load([| uri |])

                let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

                let dataProcessPipeline =
                    match settings.ClassificationType with
                    | ClassificationType.Text ->
                        mlCtx.Transforms.Text.FeaturizeText(
                            outputColumnName = "Features",
                            inputColumnName = featureColName
                        )

                let trainer =
                    mlCtx.BinaryClassification.Trainers.SdcaLogisticRegression(
                        labelColumnName = settings.LabelColumnName,
                        featureColumnName = "Features"
                    )

                let trainingPipeline = dataProcessPipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet)

                mlCtx.Model.Save(trainedModel, dataView.Schema, settings.ModelSavePath)

                let predictions = trainedModel.Transform(trainTestSplit.TestSet)

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
