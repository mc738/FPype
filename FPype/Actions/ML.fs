﻿namespace FPype.Actions

open FPype.Data.Store


module ML =

    open System
    open FsToolbox.Extensions
    open FPype.Data
    open FPype.Data.Models
    open FPype.Data.Store
    open FPype.ML

    module ``train-binary-classification-model`` =
        let name = "train_binary_classification_model"

        type Parameters =
            { TrainingSettings: BinaryClassification.TrainingSettings
              ModelName: string
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            BinaryClassification.train mlCtx store parameters.TrainingSettings
            |> Result.map (fun metrics ->

                match
                    BinaryClassification.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(name, $"Error saving metrics: {e}")

                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-multiclass-classification-model`` =
        let name = "train_multiclass_classification_model"

        type Parameters =
            { TrainingSettings: MulticlassClassification.TrainingSettings
              ModelName: string
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            MulticlassClassification.train mlCtx store parameters.TrainingSettings
            |> Result.map (fun metrics ->
                match
                    MulticlassClassification.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(name, $"Error saving metrics: {e}")

                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-regression-model`` =
        let name = "train_regression_model"

        type Parameters =
            { TrainingSettings: Regression.TrainingSettings
              ModelName: string
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            Regression.train mlCtx store parameters.TrainingSettings
            |> Result.map (fun metrics ->
                match
                    Regression.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(name, $"Error saving metrics: {e}")
                
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-matrix-factorization-model`` =
        let name = "train_matrix_factorization_model"

        type Parameters =
            { TrainingSettings: MatrixFactorization.TrainingSettings
              ModelName: string
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            MatrixFactorization.train mlCtx store parameters.TrainingSettings
            |> Result.map (fun metrics ->
                match
                   MatrixFactorization.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(name, $"Error saving metrics: {e}")
                
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name
