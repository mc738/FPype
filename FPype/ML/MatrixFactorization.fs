namespace FPype.ML

open System
open System.Text.Json
open FPype.Data.Models
open FsToolbox.Core
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
        | MatrixFactorization of MatrixFactorizationSettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "matrix-factorization" ->
                MatrixFactorizationSettings.FromJson(element)
                |> Result.map TrainerType.MatrixFactorization
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

        member tt.GetName() =
            match tt with
            | MatrixFactorization _ -> "Matrix factorization"

    and MatrixFactorizationSettings =
        { Alpha: float option
          C: float option
          Lambda: float option
          ApproximationRank: int option
          LearningRate: float option
          LossFunction: MatrixFactorizationTrainer.LossFunctionType option
          NonNegative: bool option
          LabelColumnName: string
          NumberOfIterations: int option
          NumberOfThreads: int option
          MatrixColumnIndexColumnName: string
          MatrixRowIndexColumnName: string }

        static member FromJson(element: JsonElement) =
            match
                Json.tryGetStringProperty "labelColumnName" element,
                Json.tryGetStringProperty "matrixColumnIndexColumnName" element,
                Json.tryGetStringProperty "matrixRowIndexColumnName" element
            with
            | Some lc, Some cin, Some rin ->
                { Alpha = Json.tryGetDoubleProperty "alpha" element
                  C = Json.tryGetDoubleProperty "c" element
                  Lambda = Json.tryGetDoubleProperty "lambda" element
                  ApproximationRank = Json.tryGetIntProperty "approximationRank" element
                  LearningRate = Json.tryGetDoubleProperty "learningRate" element
                  LossFunction =
                    Json.tryGetStringProperty "lossFunction" element
                    |> Option.bind (fun lf ->
                        match lf with
                        | "square-loss-regression" ->
                            Some MatrixFactorizationTrainer.LossFunctionType.SquareLossRegression
                        | "square-loss-one-class" -> Some MatrixFactorizationTrainer.LossFunctionType.SquareLossOneClass
                        | _ -> None)
                  NonNegative = Json.tryGetBoolProperty "nonNegative" element
                  LabelColumnName = lc
                  NumberOfIterations = Json.tryGetIntProperty "numberOfIterations" element
                  NumberOfThreads = Json.tryGetIntProperty "numberOfThreads" element
                  MatrixColumnIndexColumnName = cin
                  MatrixRowIndexColumnName = rin }
                |> Ok
            | None, _, _ -> Error "Missing `labelColumnName` property"
            | _, None, _ -> Error "Missing `matrixColumnIndexColumnName` property"
            | _, _, None -> Error "Missing `matrixRowIndexColumnName` property"

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

    let train (mlCtx: MLContext) (store: PipelineStore) (settings: TrainingSettings) =
        getDataSourceUri store settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri

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
                        trainerSettings.LossFunction |> Option.iter (fun v -> options.LossFunction <- v)
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

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, store.ExpandPath settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

                mlCtx
                    .Recommendation()
                    .Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")
                |> Ok

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
