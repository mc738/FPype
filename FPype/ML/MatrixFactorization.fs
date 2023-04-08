namespace FPype.ML


[<RequireQualifiedAccess>]
module MatrixFactorization =

    open FPype.Core.Types
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Trainers
    open FPype.Data.Store

    type TrainingSettings = {
        MatrixColumnIndexColumnName: string
        MatrixRowIndexColumnName: string
        LabelColumnName: string
        NumberOfIterations: int
        ApproximationRank: int
        General: GeneralTrainingSettings
    }

    [<CLIMutable>]
    type PredictionItem = { Label: float32; Score: float32 }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri

                let options = MatrixFactorizationTrainer.Options()

                options.MatrixColumnIndexColumnName <- settings.MatrixColumnIndexColumnName
                options.MatrixRowIndexColumnName <- settings.MatrixRowIndexColumnName
                options.LabelColumnName <- settings.LabelColumnName
                options.NumberOfIterations <- settings.NumberOfIterations
                options.ApproximationRank <- settings.ApproximationRank

                let trainer = mlCtx.Recommendation().Trainers.MatrixFactorization(options)


                let trainingPipeline = trainingCtx.Pipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                mlCtx
                    .Recommendation()
                    .Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")
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