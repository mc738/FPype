namespace FPype.ML

open System
open FPype.Data.Models
open Microsoft.ML.Data
open Microsoft.ML.Trainers


[<RequireQualifiedAccess>]
module MatrixFactorization =

    open FPype.Core.Types
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Trainers
    open FPype.Data.Store

    type TrainingSettings =
        {
          //MatrixColumnIndexColumnName: string
          //MatrixRowIndexColumnName: string
          //LabelColumnName: string
          //NumberOfIterations: int
          //ApproximationRank: int
          General: GeneralTrainingSettings
          TrainerType: TrainerType }

    and [<RequireQualifiedAccess>] TrainerType =
        | MatrixFactorization of MatrixFactorizationSettings

        member tt.GetName() =
            match tt with
            | MatrixFactorization _ -> "Matrix factorization"

    and MatrixFactorizationSettings =
        { Alpha: float option
          C: float option
          Lambda: float option
          ApproximationRank: int option
          LearningRate: float option
          // Loss function
          NonNegative: bool option
          LabelColumnName: string
          NumberOfIterations: int option
          NumberOfThreads: int option
          MatrixColumnIndexColumnName: string
          MatrixRowIndexColumnName: string

         }

    [<CLIMutable>]
    type PredictionItem = { Label: float32; Score: float32 }

    let metricsToString (modelName: string) (trainerType: TrainerType) (metrics: RegressionMetrics) =
        [ $"{modelName} metrics"
          ""
          "Model type: multiclass classification"
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

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri





                (*
                options.MatrixColumnIndexColumnName <- settings.MatrixColumnIndexColumnName
                options.MatrixRowIndexColumnName <- settings.MatrixRowIndexColumnName
                options.LabelColumnName <- settings.LabelColumnName
                options.NumberOfIterations <- settings.NumberOfIterations
                options.ApproximationRank <- settings.ApproximationRank
                *)


                let trainer =
                    match settings.TrainerType with
                    | TrainerType.MatrixFactorization trainerSettings ->
                        let options = MatrixFactorizationTrainer.Options()

                        options.LabelColumnName <- trainerSettings.LabelColumnName
                        options.MatrixColumnIndexColumnName <- trainerSettings.MatrixColumnIndexColumnName
                        options.MatrixRowIndexColumnName <- trainerSettings.MatrixRowIndexColumnName
                        trainerSettings.Alpha |> Option.iter (fun v -> options.Alpha <- v)
                        trainerSettings.C |> Option.iter (fun v -> options.C <- v)
                        trainerSettings.Lambda |> Option.iter (fun v -> options.Lambda <- v)

                        trainerSettings.ApproximationRank
                        |> Option.iter (fun v -> options.ApproximationRank <- v)

                        trainerSettings.LearningRate |> Option.iter (fun v -> options.LearningRate <- v)
                        trainerSettings.NonNegative |> Option.iter (fun v -> options.NonNegative <- v)

                        trainerSettings.NumberOfIterations
                        |> Option.iter (fun v -> options.NumberOfIterations <- v)

                        trainerSettings.NumberOfThreads
                        |> Option.toNullable
                        |> (fun v -> options.NumberOfThreads <- v)

                        mlCtx.Recommendation().Trainers.MatrixFactorization(options)
                        |> Internal.downcastPipeline

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
