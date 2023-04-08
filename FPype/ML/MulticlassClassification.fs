namespace FPype.ML

[<RequireQualifiedAccess>]
module MulticlassClassification =

    open FPype.Core.Types
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Data
    open FPype.Data.Store

    type TrainingSettings =
        { General: GeneralTrainingSettings }

    [<CLIMutable>]
    type PredictionItem =
        { [<ColumnName("PredictedLabel")>]
          PredictedLabel: string }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri
                
                // TODO set strings are settings
                let trainer =
                    mlCtx.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features")

                // TODO set string are settings
                let trainingPipeline =
                    trainingCtx.Pipeline
                        .AppendCacheCheckpoint(mlCtx)
                        .Append(trainer)
                        .Append(mlCtx.Transforms.Conversion.MapKeyToValue("PredictedLabel"))

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

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