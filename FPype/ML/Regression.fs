﻿namespace FPype.ML

[<RequireQualifiedAccess>]
module Regression =

    open FPype.Core.Types
    open FPype.Data.Store
    open Microsoft.FSharp.Core
    open Microsoft.ML
    
    type TrainingSettings =
        { General: GeneralTrainingSettings }

    and [<CLIMutable>] PredictionItem = { Score: float32 }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri
                
                // TODO set strings are settings
                let trainer =
                    mlCtx.Regression.Trainers.Sdca(labelColumnName = "Label", featureColumnName = "Feature")

                let trainingPipeline = trainingCtx.Pipeline.Append(trainer)

                let trainedModel = trainingPipeline.Fit(trainingCtx.TrainingData)

                mlCtx.Model.Save(trainedModel, trainingCtx.TrainingData.Schema, settings.General.ModelSavePath)

                let predictions = trainedModel.Transform(trainingCtx.TestData)

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
