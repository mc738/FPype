namespace FPype.ML

open FPype.Core.Types
open Microsoft.FSharp.Core
open Microsoft.ML
open Microsoft.ML.Data

[<RequireQualifiedAccess>]
module MulticlassClassification =

    open FPype.Data.Store

    type TrainingSettings =
        { DataSource: DataSource
          ModelSavePath: string
          HasHeaders: bool
          Separators: char array
          TrainingTestSplit: float
          Columns: DataColumn list
          RowFilters: RowFilter list
          Transformations: TransformationType list }

    [<CLIMutable>]
    type PredictionItem =
        { [<ColumnName("PredictedLabel")>]
          PredictedLabel: string }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.DataSource
        |> Result.bind (fun uri ->
            try
                // Create text loader
                let loader =
                    createTextLoader mlCtx settings.HasHeaders settings.Separators settings.Columns

                let dataView = loader.Load([| uri |])

                let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

                let trainingData = filterRows mlCtx trainTestSplit.TrainSet settings.RowFilters

                let dataProcessPipeline = runTransformations mlCtx settings.Transformations

                // TODO set strings are settings
                let trainer =
                    mlCtx.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features")

                // TODO set string are settings
                let trainingPipeline =
                    dataProcessPipeline
                        .AppendCacheCheckpoint(mlCtx)
                        .Append(trainer)
                        .Append(mlCtx.Transforms.Conversion.MapKeyToValue("PredictedLabel"))

                let trainedModel = trainingPipeline.Fit(trainingData)

                mlCtx.Model.Save(trainedModel, trainingData.Schema, settings.ModelSavePath)

                let predictions = trainedModel.Transform(trainTestSplit.TestSet)

                mlCtx.MulticlassClassification.Evaluate(predictions) |> Ok

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