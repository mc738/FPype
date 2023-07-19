namespace FPype.Actions

open System.IO
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
              DataSource: string
              ModelSavePath: string
              ContextSeed: int option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            let savePath =
                Path.Combine(store.SubstituteValues parameters.ModelSavePath, $"{parameters.ModelName}.zip")

            // NOTE Check this is correct
            getDataSourceAsFileUri store parameters.DataSource true
            |> Result.bind (BinaryClassification.train mlCtx savePath parameters.TrainingSettings)
            |> Result.map (fun metrics ->
                store.Log(stepName, name, $"Model saved to `{parameters.ModelSavePath}`.")

                match
                    BinaryClassification.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(stepName, name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(stepName, name, $"Error saving metrics: {e}")

                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``train-multiclass-classification-model`` =
        let name = "train_multiclass_classification_model"

        type Parameters =
            { TrainingSettings: MulticlassClassification.TrainingSettings
              ModelName: string
              DataSource: string
              ModelSavePath: string
              ContextSeed: int option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            let savePath =
                Path.Combine(store.SubstituteValues parameters.ModelSavePath, $"{parameters.ModelName}.zip")

            getDataSourceAsFileUri store parameters.DataSource true
            |> Result.bind (MulticlassClassification.train mlCtx savePath parameters.TrainingSettings)
            |> Result.map (fun metrics ->
                store.Log(stepName, name, $"Model saved to `{parameters.ModelSavePath}`.")

                match
                    MulticlassClassification.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(stepName, name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(stepName, name, $"Error saving metrics: {e}")

                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``train-regression-model`` =
        let name = "train_regression_model"

        type Parameters =
            { TrainingSettings: Regression.TrainingSettings
              ModelName: string
              DataSource: string
              ModelSavePath: string
              ContextSeed: int option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            let savePath =
                Path.Combine(store.SubstituteValues parameters.ModelSavePath, $"{parameters.ModelName}.zip")

            getDataSourceAsFileUri store parameters.DataSource true
            |> Result.bind (Regression.train mlCtx savePath parameters.TrainingSettings)
            |> Result.map (fun metrics ->
                store.Log(stepName, name, $"Model saved to `{parameters.ModelSavePath}`.")

                match
                    Regression.metricsToTable parameters.ModelName parameters.TrainingSettings.TrainerType metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(stepName, name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(stepName, name, $"Error saving metrics: {e}")

                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``train-matrix-factorization-model`` =
        let name = "train_matrix_factorization_model"

        type Parameters =
            { TrainingSettings: MatrixFactorization.TrainingSettings
              ModelName: string
              DataSource: string
              ModelSavePath: string
              ContextSeed: int option }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed

            let savePath =
                Path.Combine(store.SubstituteValues parameters.ModelSavePath, $"{parameters.ModelName}.zip")

            getDataSourceAsFileUri store parameters.DataSource true
            |> Result.bind (MatrixFactorization.train mlCtx savePath parameters.TrainingSettings)
            |> Result.map (fun metrics ->
                match
                    MatrixFactorization.metricsToTable
                        parameters.ModelName
                        parameters.TrainingSettings.TrainerType
                        metrics
                    |> store.CreateTable
                    |> store.InsertRows
                with
                | Ok _ -> store.Log(stepName, name, $"Model `{parameters.ModelName}` metrics saved.")
                | Error e -> store.LogError(stepName, name, $"Error saving metrics: {e}")

                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName
