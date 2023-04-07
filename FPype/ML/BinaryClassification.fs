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
        { DataPath: string
          DataSource: DataSource
          ModelSavePath: string
          HasHeaders: bool
          Separators: char array
          TrainingTestSplit: float
          ClassificationType: ClassificationType
          FeatureColumnIndex: int
          LabelColumnName: string
          LabelColumnIndex: int }

    type DataModel = { Label: bool }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        match DataSourceType.Deserialize settings.DataSource.Type with
        | Some DataSourceType.File ->
            // NOTE would mlCtx.Data.CreateTextLoader be better?
            //let loader = mlCtx.Data.CreateTextLoader(
            //    separatorChar ='\t',
            //    hasHeader = true,
            //    (*dataSample = ,*)
            //    allowQuoting = true,
            //    trimWhitespace = true,
            //    allowSparse = true)
            //let dataView = loader.Load([| "path/to/data" |])

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

            let dataView = loader.Load([| settings.DataSource.Uri |])

            let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

            // Is this always needed?
            let dataProcessPipeline =
                mlCtx.Transforms.Text.FeaturizeText(outputColumnName = "Features", inputColumnName = featureColName)

            let trainer =
                mlCtx.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName = settings.LabelColumnName,
                    featureColumnName = "Features"
                )

            let trainingPipeline = dataProcessPipeline.Append(trainer)

            let trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet)

            mlCtx.Model.Save(trainedModel, dataView.Schema, settings.ModelSavePath)
            
            let predictions = trainedModel.Transform(trainTestSplit.TestSet)
            mlCtx.BinaryClassification.Evaluate(data = predictions, labelColumnName = "Label", scoreColumnName = "Score") |> Ok

        | Some DataSourceType.Artifact -> Error "Artifact data sources to be implemented"
        | None -> Error "Unknown data source type"

    let load (mlCtx: MLContext) (path: string) =
        try
            match mlCtx.Model.Load(path) with
            | (m, _) ->
                mlCtx.Model.CreatePredictionEngine<TextClassificationItem, TextPredictionItem>(m)
                |> Ok
        with ex ->
            Error ex.Message

    let rec predict (engine: PredictionEngine<TextClassificationItem, TextPredictionItem>) (text: string) =
        engine.Predict({ Text = text; Label = true })
