namespace FPype.ML

open System.Text.Json
open FPype.Data.Store
open FsToolbox.Core

[<RequireQualifiedAccess>]
module MulticlassClassification =

    open FPype.Core.Types
    open Microsoft.FSharp.Core
    open Microsoft.ML
    open Microsoft.ML.Data
    open FPype.Data.Store

    type TrainingSettings =
        { General: GeneralTrainingSettings
          TrainerType: TrainerType }

        static member FromJson(element: JsonElement, store: PipelineStore) =
            match
                Json.tryGetProperty "general" element
                |> Option.map (fun el -> GeneralTrainingSettings.FromJson(el, store))
                |> Option.defaultWith (fun _ -> Error "Missing `general` property"),
                Json.tryGetProperty "trainer" element
                |> Option.map TrainerType.FromJson
                |> Option.defaultWith (fun _ -> Error "Missing `trainer` property")
            with
            | Ok gts, Ok ts -> { General = gts; TrainerType = ts } |> Ok
            | Error e, _ -> Error e
            | _, Error e -> Error e

    and [<RequireQualifiedAccess>] TrainerType =
        | SdcaMaximumEntropy of SdcaMaximumEntropySettings

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "sdca-maximum-entropy" ->
                SdcaMaximumEntropySettings.FromJson(element)
                |> Result.map TrainerType.SdcaMaximumEntropy
            | Some t -> Error $"Unknown trainer type `{t}`"
            | None -> Error "Missing property `type`"

    and SdcaMaximumEntropySettings =
        { LabelColumnName: string
          FeatureColumnName: string
          ExampleWeightColumnName: string option
          L2Regularization: float32 option
          L1Regularization: float32 option
          MaximumNumberOfIterations: int option }

        static member Default() =
            { LabelColumnName = "Label"
              FeatureColumnName = "Features"
              ExampleWeightColumnName = None
              L2Regularization = None
              L1Regularization = None
              MaximumNumberOfIterations = None }

        static member FromJson(element: JsonElement) =
            match
                Json.tryGetStringProperty "labelColumnName" element,
                Json.tryGetStringProperty "featureColumnName" element
            with
            | Some lcn, Some fcn ->
                { LabelColumnName = lcn
                  FeatureColumnName = fcn
                  ExampleWeightColumnName = Json.tryGetStringProperty "exampleWeightColumnName" element
                  L2Regularization = Json.tryGetSingleProperty "l2Regularization" element
                  L1Regularization = Json.tryGetSingleProperty "l1Regularization" element
                  MaximumNumberOfIterations = Json.tryGetIntProperty "maximumNumberOfIterations" element }
                |> Ok
            | None, _ -> Error "Missing `labelColumnName` property"
            | _, None -> Error "Missing `featureColumnName` property"

    [<CLIMutable>]
    type PredictionItem =
        { [<ColumnName("PredictedLabel")>]
          PredictedLabel: string

          Score: float32 array }

    let train (mlCtx: MLContext) (settings: TrainingSettings) =
        getDataSourceUri settings.General.DataSource
        |> Result.bind (fun uri ->
            try
                let trainingCtx = createTrainingContext mlCtx settings.General uri

                let trainer =
                    match settings.TrainerType with
                    | TrainerType.SdcaMaximumEntropy trainerSettings ->
                        mlCtx.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                            labelColumnName = trainerSettings.LabelColumnName,
                            featureColumnName = trainerSettings.FeatureColumnName,
                            exampleWeightColumnName =
                                (trainerSettings.ExampleWeightColumnName |> Option.defaultValue null),
                            l2Regularization = (trainerSettings.L2Regularization |> Option.toNullable),
                            l1Regularization = (trainerSettings.L1Regularization |> Option.toNullable),
                            maximumNumberOfIterations = (trainerSettings.MaximumNumberOfIterations |> Option.toNullable)
                        )
                        |> Internal.downcastPipeline

                // TODO set string are settings
                let trainingPipeline =
                    trainingCtx
                        .Pipeline
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
