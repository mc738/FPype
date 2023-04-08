namespace FPype.ML

open System.Data
open FPype.Core.Types
open FPype.Data.Store
open Microsoft.FSharp.Core
open Microsoft.ML
open Microsoft.ML.Data

[<RequireQualifiedAccess>]
module Regression =

    type TrainingSettings =
        { DataSource: DataSource
          ModelSavePath: string
          HasHeaders: bool
          Separators: char array
          TrainingTestSplit: float
          Columns: DataColumn list
          RowFilters: RowFilter list
          Transformations: TransformationType list }

    and DataColumn =
        { Index: int
          Name: string
          DataKind: DataKind }

    and RowFilter =
        { ColumnName: string
          Minimum: float option
          Maximum: float option }

    and [<CLIMutable>] PredictionItem = { Score: float32 }

    and CopyColumnTransformation =
        { OutputColumnName: string
          InputColumnName: string }

    and [<RequireQualifiedAccess>] TransformationType =
        | CopyColumns of OutputColumnName: string * InputColumnName: string
        | OneHotEncoding of OutputColumnName: string * InputColumnName: string
        | NormalizeMeanVariance of OutputColumnName: string
        | Concatenate of OutputColumnName: string * Columns: string list

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.DataSource
        |> Result.bind (fun uri ->
            try
                let options = TextLoader.Options()

                options.HasHeader <- settings.HasHeaders
                options.Separators <- settings.Separators

                //let (featureColName, featureColType) =
                //    match settings.ClassificationType with
                //    | ClassificationType.Text -> "Text", DataKind.String

                options.Columns <-
                    settings.Columns
                    |> List.map (fun c -> TextLoader.Column(c.Name, c.DataKind, c.Index))
                    |> Array.ofList

                let loader = mlCtx.Data.CreateTextLoader(options = options)

                let dataView = loader.Load([| uri |])

                let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

                let trainingData =
                    settings.RowFilters
                    |> List.fold
                        (fun dv rf ->
                            match rf.Minimum, rf.Maximum with
                            | Some min, Some max ->
                                mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, lowerBound = min, upperBound = max)
                            | Some min, None -> mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, lowerBound = min)
                            | None, Some max -> mlCtx.Data.FilterRowsByColumn(dv, rf.ColumnName, upperBound = max)
                            | None, None -> dv)
                        trainTestSplit.TrainSet

                let downcastPipeline (x: IEstimator<_>) =
                    match x with
                    | :? IEstimator<ITransformer> as y -> y
                    | _ -> failwith "downcastPipeline: expecting a IEstimator<ITransformer>"

                let dataProcessPipeline =
                    settings.Transformations
                    |> List.fold
                        (fun (acc: IEstimator<ITransformer>) t ->
                            match t with
                            | TransformationType.Concatenate (outputColumnName, columns) ->
                                acc.Append(mlCtx.Transforms.Concatenate(outputColumnName, columns |> Array.ofList))
                                |> downcastPipeline
                            | TransformationType.OneHotEncoding (outputColumnName, inputColumnName) ->
                                acc.Append(
                                    mlCtx.Transforms.Categorical.OneHotEncoding(outputColumnName, inputColumnName)
                                )
                                |> downcastPipeline
                            | TransformationType.NormalizeMeanVariance outputColumnName ->
                                acc.Append(mlCtx.Transforms.NormalizeMeanVariance(outputColumnName))
                                |> downcastPipeline
                            | TransformationType.CopyColumns (outputColumnName, inputColumnName) ->
                                acc.Append(mlCtx.Transforms.CopyColumns(outputColumnName, inputColumnName))
                                |> downcastPipeline

                            )
                        (EstimatorChain() |> downcastPipeline)

                let trainer = mlCtx.Regression.Trainers.Sdca(labelColumnName = "Label", featureColumnName = "Feature")
                
                let trainingPipeline = dataProcessPipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet)
                
                mlCtx.Model.Save(trainedModel, dataView.Schema, settings.ModelSavePath)
                
                let predictions = trainedModel.Transform(trainTestSplit.TestSet)
                
                mlCtx.Regression.Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score") |> Ok
                
            with ex ->
                Error $"Error training model - {ex.Message}")

    let load (mlCtx: MLContext) (path: string) =
        try
            match mlCtx.Model.Load(path) with
            | (m, t) -> Ok (m, t)
        with ex ->
            Error ex.Message

    let predict (mlCtx: MLContext) (model: ITransformer) (schema: DataViewSchema) (value: Map<string, Value>) =
        let runTimeType = Common.Internal.createRunTimeType schema
        let engine = Common.Internal.getDynamicPredictionEngine<PredictionItem> mlCtx runTimeType schema model
        
        Common.Internal.runDynamicPredictionEngine<PredictionItem> runTimeType engine (Common.ClassFactory.createObjectFromType runTimeType value)
