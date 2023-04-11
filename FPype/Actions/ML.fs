namespace FPype.Actions

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
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed
            
            BinaryClassification.train mlCtx parameters.TrainingSettings
            |> Result.map (fun metrics ->
                // TODO do something with metrics
                store)
            
        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-multiclass-classification-model`` =
        let name = "train_multiclass_classification_model"

        type Parameters =
            { TrainingSettings: MulticlassClassification.TrainingSettings
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed
            
            MulticlassClassification.train mlCtx parameters.TrainingSettings
            |> Result.map (fun metrics ->
                // TODO do something with metrics
                store)
            
        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-regression-model`` =
        let name = "train_regression_model"

        type Parameters =
            { TrainingSettings: Regression.TrainingSettings
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed
            
            Regression.train mlCtx parameters.TrainingSettings
            |> Result.map (fun metrics ->
                // TODO do something with metrics
                store)
            
        let createAction (parameters: Parameters) = run parameters |> createAction name

    module ``train-matrix-factorization-model`` =
        let name = "train_matrix_factorization_model"

        type Parameters =
            { TrainingSettings: MatrixFactorization.TrainingSettings
              ContextSeed: int option }

        let run (parameters: Parameters) (store: PipelineStore) =
            let mlCtx = createCtx parameters.ContextSeed
            
            MatrixFactorization.train mlCtx parameters.TrainingSettings
            |> Result.map (fun metrics ->
                // TODO do something with metrics
                store)
            
        let createAction (parameters: Parameters) = run parameters |> createAction name
