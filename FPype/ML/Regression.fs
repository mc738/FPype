﻿namespace FPype.ML

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

    and [<CLIMutable>] PredictionItem = { Score: float32 }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.DataSource
        |> Result.bind (fun uri ->
            try
                let loader =
                    createTextLoader mlCtx settings.HasHeaders settings.Separators settings.Columns

                let dataView = loader.Load([| uri |])

                let trainTestSplit = mlCtx.Data.TrainTestSplit(dataView, settings.TrainingTestSplit)

                let trainingData = filterRows mlCtx trainTestSplit.TrainSet settings.RowFilters

                let dataProcessPipeline = runTransformations mlCtx settings.Transformations

                // TODO set strings are settings
                let trainer =
                    mlCtx.Regression.Trainers.Sdca(labelColumnName = "Label", featureColumnName = "Feature")

                let trainingPipeline = dataProcessPipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainingData)

                mlCtx.Model.Save(trainedModel, dataView.Schema, settings.ModelSavePath)

                let predictions = trainedModel.Transform(trainTestSplit.TestSet)

                // TODO set strings are settings
                mlCtx.Regression.Evaluate(predictions, labelColumnName = "Label", scoreColumnName = "Score")
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
