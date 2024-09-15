namespace FPype.Interoperability.Common.Actions

module ML =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type ITransformationType =

        abstract member TransformationType: string

        abstract member WriteToJsonValue: Writer: Utf8JsonWriter -> unit

    type ConcatenateTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string
          [<JsonPropertyName "columns">]
          Columns: string list }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "copy-columns")
                        w.WriteString("outputColumnName", this.OutputColumnName)
                        Json.writeArray (fun aw -> this.Columns |> List.iter aw.WriteStringValue) "columns" w)
                    writer

    type CopyColumnsTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string
          [<JsonPropertyName "inputColumnName">]
          InputColumnName: string }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "copy-columns")
                        w.WriteString("outputColumnName", this.OutputColumnName)
                        w.WriteString("inputColumnName", this.InputColumnName))
                    writer

    type FeaturizeTextTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string
          [<JsonPropertyName "inputColumnName">]
          InputColumnName: string }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "featurize-text")
                        w.WriteString("outputColumnName", this.OutputColumnName)
                        w.WriteString("inputColumnName", this.InputColumnName))
                    writer

    type MapValueToKeyTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string
          [<JsonPropertyName "inputColumnName">]
          InputColumnName: string }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "map-value-to-key")
                        w.WriteString("outputColumnName", this.OutputColumnName)
                        w.WriteString("inputColumnName", this.InputColumnName))
                    writer

    type NormalizeMeanVarianceTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "normalize-mean-variance")
                        w.WriteString("outputColumnName", this.OutputColumnName))
                    writer

    type OneHotEncodingTransformation =
        { [<JsonPropertyName "outputColumnName">]
          OutputColumnName: string
          [<JsonPropertyName "inputColumnName">]
          InputColumnName: string }

        interface ITransformationType with

            [<JsonPropertyName "transformationType">]
            member this.TransformationType = nameof this

            member this.WriteToJsonValue(writer) =
                Json.writeObject
                    (fun w ->
                        w.WriteString("type", "one-hot-encoding")
                        w.WriteString("outputColumnName", this.OutputColumnName)
                        w.WriteString("inputColumnName", this.InputColumnName))
                    writer

    type MLDataColumn =
        { [<JsonPropertyName "index">]
          Index: int
          [<JsonPropertyName "name">]
          Name: string
          [<JsonPropertyName "dataType">]
          DataType: string }

        member this.WriteToJsonValue(writer) =
            Json.writeObject
                (fun w ->
                    w.WriteNumber("index", this.Index)
                    w.WriteString("name", this.Name)
                    w.WriteString("dataType", this.DataType))
                writer

    type MLRowFilter =
        { [<JsonPropertyName "columnName">]
          ColumnName: string
          [<JsonPropertyName "minimum">]
          Minimum: float option
          [<JsonPropertyName "maximum">]
          Maximum: float option }

        member this.WriteToJsonValue(writer) =
            Json.writeObject
                (fun w ->
                    w.WriteString("columnName", this.ColumnName)
                    this.Minimum |> Option.iter (fun v -> w.WriteNumber("minimum", v))
                    this.Maximum |> Option.iter (fun v -> w.WriteNumber("maximum", v)))
                writer

    type GeneralSettings =
        { [<JsonPropertyName "hasHeaders">]
          HasHeaders: bool
          [<JsonPropertyName "separators">]
          Separators: string list
          [<JsonPropertyName "allowQuoting">]
          AllowQuoting: bool
          [<JsonPropertyName "readMultilines">]
          ReadMultilines: bool
          [<JsonPropertyName "trainingTestSplit">]
          TrainingTestSplit: float
          [<JsonPropertyName "columns">]
          Columns: MLDataColumn list
          [<JsonPropertyName "rowFilters">]
          RowFilters: MLRowFilter list
          [<JsonPropertyName "transformations">]
          Transformations: ITransformationType list }

        member this.WriteToJsonProperty(name, writer) =
            Json.writePropertyObject
                (fun w ->
                    w.WriteBoolean("hasHeaders", this.HasHeaders)
                    Json.writeArray (fun aw -> this.Separators |> List.iter aw.WriteStringValue) "separators" w
                    w.WriteBoolean("allowQuoting", this.AllowQuoting)
                    w.WriteBoolean("readMultilines", this.ReadMultilines)
                    w.WriteBoolean("trainingTestSplit", this.ReadMultilines)
                    w.WriteNumber("trainingTestSplit", this.TrainingTestSplit)
                    Json.writeArray (fun aw -> this.Columns |> List.iter (fun c -> c.WriteToJsonValue aw)) "columns" w

                    Json.writeArray
                        (fun aw -> this.RowFilters |> List.iter (fun rf -> rf.WriteToJsonValue aw))
                        "rowFilters"
                        w

                    Json.writeArray
                        (fun aw -> this.Transformations |> List.iter (fun t -> t.WriteToJsonValue aw))
                        "transformations"
                        w)
                name
                writer

    type IBinaryTrainerSettings =

        abstract member TrainerType: string

        abstract member WriteToJsonProperty: Name: string * Writer: Utf8JsonWriter -> unit

    type SdcaLogisticRegressionBinaryTrainerSettings =
        { [<JsonPropertyName "labelColumnName">]
          LabelColumnName: string
          [<JsonPropertyName "featureColumnName">]
          FeatureColumnName: string
          [<JsonPropertyName "exampleWeightColumnName">]
          ExampleWeightColumnName: string
          [<JsonPropertyName "l2Regularization">]
          L2Regularization: double option
          [<JsonPropertyName "l1Regularization">]
          L1Regularization: double option
          [<JsonPropertyName "maximumNumberOfIterations">]
          MaximumNumberOfIterations: int option }

        interface IBinaryTrainerSettings with

            [<JsonPropertyName "trainerType">]
            member this.TrainerType = nameof this

            member this.WriteToJsonProperty(name, writer) =
                Json.writePropertyObject
                    (fun w ->
                        w.WriteString("type", "sdca-logistic-regression")
                        w.WriteString("labelColumnName", this.LabelColumnName)
                        w.WriteString("featureColumnName", this.FeatureColumnName)
                        w.WriteString("exampleWeightColumnName", this.ExampleWeightColumnName)

                        this.L2Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l2Regularization", v))

                        this.L1Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l1Regularization", v))

                        this.MaximumNumberOfIterations
                        |> Option.iter (fun v -> w.WriteNumber("maximumNumberOfIterations", v)))
                    name
                    writer

    type IMulticlassTrainerSettings =

        abstract member TrainerType: string

        abstract member WriteToJsonProperty: Name: string * Writer: Utf8JsonWriter -> unit

    type SdcaMaximumEntropyMulticlassTrainerSettings =
        { [<JsonPropertyName "labelColumnName">]
          LabelColumnName: string
          [<JsonPropertyName "featureColumnName">]
          FeatureColumnName: string
          [<JsonPropertyName "exampleWeightColumnName">]
          ExampleWeightColumnName: string
          [<JsonPropertyName "l2Regularization">]
          L2Regularization: double option
          [<JsonPropertyName "l1Regularization">]
          L1Regularization: double option
          [<JsonPropertyName "maximumNumberOfIterations">]
          MaximumNumberOfIterations: int option }

        interface IMulticlassTrainerSettings with

            [<JsonPropertyName "trainerType">]
            member this.TrainerType = nameof this


            member this.WriteToJsonProperty(name, writer) =
                Json.writePropertyObject
                    (fun w ->
                        w.WriteString("type", "sdca-maximum-entropy")
                        w.WriteString("labelColumnName", this.LabelColumnName)
                        w.WriteString("featureColumnName", this.FeatureColumnName)
                        w.WriteString("exampleWeightColumnName", this.ExampleWeightColumnName)

                        this.L2Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l2Regularization", v))

                        this.L1Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l1Regularization", v))

                        this.MaximumNumberOfIterations
                        |> Option.iter (fun v -> w.WriteNumber("maximumNumberOfIterations", v)))
                    name
                    writer

    type IRegressionTrainerSettings =

        abstract member TrainerType: string

        abstract member WriteToJsonProperty: Name: string * Writer: Utf8JsonWriter -> unit

    type SdcaRegressionTrainerSettings =
        { [<JsonPropertyName "labelColumnName">]
          LabelColumnName: string
          [<JsonPropertyName "featureColumnName">]
          FeatureColumnName: string
          [<JsonPropertyName "exampleWeightColumnName">]
          ExampleWeightColumnName: string
          [<JsonPropertyName "l2Regularization">]
          L2Regularization: double option
          [<JsonPropertyName "l1Regularization">]
          L1Regularization: double option
          [<JsonPropertyName "maximumNumberOfIterations">]
          MaximumNumberOfIterations: int option }

        interface IRegressionTrainerSettings with

            [<JsonPropertyName "trainerType">]
            member this.TrainerType = nameof this

            member this.WriteToJsonProperty(name, writer) =
                Json.writePropertyObject
                    (fun w ->
                        w.WriteString("type", "sdca")
                        w.WriteString("labelColumnName", this.LabelColumnName)
                        w.WriteString("featureColumnName", this.FeatureColumnName)
                        w.WriteString("exampleWeightColumnName", this.ExampleWeightColumnName)

                        this.L2Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l2Regularization", v))

                        this.L1Regularization
                        |> Option.iter (fun v -> w.WriteNumber("l1Regularization", v))

                        this.MaximumNumberOfIterations
                        |> Option.iter (fun v -> w.WriteNumber("maximumNumberOfIterations", v)))
                    name
                    writer

    type IMatrixFactorizationTrainerSettings =

        abstract member TrainerType: string

        abstract member WriteToJsonProperty: Name: string * Writer: Utf8JsonWriter -> unit

    type MatrixFactorizationTrainerSettings =
        { [<JsonPropertyName "alpha">]
          Alpha: double option
          [<JsonPropertyName "c">]
          C: double option
          [<JsonPropertyName "lambda">]
          Lambda: double option
          [<JsonPropertyName "approximationRank">]
          ApproximationRank: int option
          [<JsonPropertyName "learningRate">]
          LearningRate: double option
          [<JsonPropertyName "lossFunction">]
          LossFunction: string
          [<JsonPropertyName "nonNegative">]
          NonNegative: bool option
          [<JsonPropertyName "labelColumnName">]
          LabelColumnName: string
          [<JsonPropertyName "numberOfIterations">]
          NumberOfIterations: int option
          [<JsonPropertyName "numberOfThreads">]
          NumberOfThreads: int option
          [<JsonPropertyName "matrixColumnIndexColumnName">]
          MatrixColumnIndexColumnName: string
          [<JsonPropertyName "matrixRowIndexColumnName">]
          MatrixRowIndexColumnName: string }

        interface IMatrixFactorizationTrainerSettings with

            [<JsonPropertyName "trainerType">]
            member this.TrainerType = nameof this

            member this.WriteToJsonProperty(name, writer) =
                Json.writePropertyObject
                    (fun w ->
                        this.Alpha |> Option.iter (fun v -> w.WriteNumber("alpha", v))
                        this.C |> Option.iter (fun v -> w.WriteNumber("c", v))
                        this.Lambda |> Option.iter (fun v -> w.WriteNumber("lambda", v))

                        this.ApproximationRank
                        |> Option.iter (fun v -> w.WriteNumber("approximationRank", v))

                        this.LearningRate |> Option.iter (fun v -> w.WriteNumber("learningRate", v))
                        w.WriteString("lossFunction", this.LossFunction)
                        this.NonNegative |> Option.iter (fun v -> w.WriteBoolean("nonNegative", v))
                        w.WriteString("labelColumnName", this.LabelColumnName)

                        this.NumberOfIterations
                        |> Option.iter (fun v -> w.WriteNumber("numberOfIterations", v))

                        this.NumberOfThreads
                        |> Option.iter (fun v -> w.WriteNumber("numberOfThreads", v))

                        w.WriteString("matrixColumnIndexColumnName", this.MatrixColumnIndexColumnName)
                        w.WriteString("matrixRowIndexColumnName", this.MatrixRowIndexColumnName))
                    name
                    writer

    type BinaryClassificationTrainingSettings =
        { [<JsonPropertyName "general">]
          General: GeneralSettings
          [<JsonPropertyName "trainer">]
          Trainer: IBinaryTrainerSettings }

        member this.WriteToJsonProperty(name, writer) =

            Json.writePropertyObject
                (fun w ->
                    this.General.WriteToJsonProperty("general", w)
                    this.Trainer.WriteToJsonProperty("trainer", w))
                name
                writer

    type MulticlassClassificationTrainingSettings =
        { [<JsonPropertyName "general">]
          General: GeneralSettings
          [<JsonPropertyName "trainer">]
          Trainer: IMulticlassTrainerSettings }

        member this.WriteToJsonProperty(name, writer) =

            Json.writePropertyObject
                (fun w ->
                    this.General.WriteToJsonProperty("general", w)
                    this.Trainer.WriteToJsonProperty("trainer", w))
                name
                writer

    type RegressionTrainingSettings =
        { [<JsonPropertyName "general">]
          General: GeneralSettings
          [<JsonPropertyName "trainer">]
          Trainer: IRegressionTrainerSettings }

        member this.WriteToJsonProperty(name, writer) =

            Json.writePropertyObject
                (fun w ->
                    this.General.WriteToJsonProperty("general", w)
                    this.Trainer.WriteToJsonProperty("trainer", w))
                name
                writer

    type MatrixFactorizationTrainingSettings =
        { [<JsonPropertyName "general">]
          General: GeneralSettings
          [<JsonPropertyName "trainer">]
          Trainer: IMatrixFactorizationTrainerSettings }

        member this.WriteToJsonProperty(name, writer) =

            Json.writePropertyObject
                (fun w ->
                    this.General.WriteToJsonProperty("general", w)
                    this.Trainer.WriteToJsonProperty("trainer", w))
                name
                writer

    type TrainBinaryClassificationModelAction =
        { [<JsonPropertyName "trainingSettings">]
          TrainingSettings: BinaryClassificationTrainingSettings
          [<JsonPropertyName "modelName">]
          ModelName: string
          // TODO fix this?
          [<JsonPropertyName "source">]
          DataSource: string
          [<JsonPropertyName "modelSavePath">]
          ModelSavePath: string
          [<JsonPropertyName "contextSeed">]
          ContextSeed: int option }

        interface IPipelineAction with


            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() =
                ML.``train-binary-classification-model``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.TrainingSettings.WriteToJsonProperty("trainingSettings", w)
                        w.WriteString("modelName", this.ModelName)
                        w.WriteString("source", this.DataSource)
                        w.WriteString("modelSavePath", this.ModelSavePath)
                        this.ContextSeed |> Option.iter (fun v -> w.WriteNumber("contextSeed", v))
                        ())
                )

    type TrainMulticlassClassificationModelAction =
        { [<JsonPropertyName "trainingSettings">]
          TrainingSettings: MulticlassClassificationTrainingSettings
          [<JsonPropertyName "modelName">]
          ModelName: string
          // TODO fix this?
          [<JsonPropertyName "source">]
          DataSource: string
          [<JsonPropertyName "modelSavePath">]
          ModelSavePath: string
          [<JsonPropertyName "contextSeed">]
          ContextSeed: int option }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() =
                ML.``train-multiclass-classification-model``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.TrainingSettings.WriteToJsonProperty("trainingSettings", w)
                        w.WriteString("modelName", this.ModelName)
                        w.WriteString("source", this.DataSource)
                        w.WriteString("modelSavePath", this.ModelSavePath)
                        this.ContextSeed |> Option.iter (fun v -> w.WriteNumber("contextSeed", v))
                        ())
                )

    type TrainRegressionModelAction =
        { [<JsonPropertyName "trainingSettings">]
          TrainingSettings: RegressionTrainingSettings
          [<JsonPropertyName "modelName">]
          ModelName: string
          // TODO fix this?
          [<JsonPropertyName "source">]
          DataSource: string
          [<JsonPropertyName "modelSavePath">]
          ModelSavePath: string
          [<JsonPropertyName "contextSeed">]
          ContextSeed: int option }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = ML.``train-regression-model``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.TrainingSettings.WriteToJsonProperty("trainingSettings", w)
                        w.WriteString("modelName", this.ModelName)
                        w.WriteString("source", this.DataSource)
                        w.WriteString("modelSavePath", this.ModelSavePath)
                        this.ContextSeed |> Option.iter (fun v -> w.WriteNumber("contextSeed", v))
                        ())
                )

    type TrainMatrixFactorizationModelAction =
        { [<JsonPropertyName "trainingSettings">]
          TrainingSettings: MatrixFactorizationTrainingSettings
          [<JsonPropertyName "modelName">]
          ModelName: string
          // TODO fix this?
          [<JsonPropertyName "source">]
          DataSource: string
          [<JsonPropertyName "modelSavePath">]
          ModelSavePath: string
          [<JsonPropertyName "contextSeed">]
          ContextSeed: int option }

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() = ML.``train-matrix-factorization-model``.name

            member this.ToSerializedActionParameters() =
                writeJson (
                    Json.writeObject (fun w ->
                        this.TrainingSettings.WriteToJsonProperty("trainingSettings", w)
                        w.WriteString("modelName", this.ModelName)
                        w.WriteString("source", this.DataSource)
                        w.WriteString("modelSavePath", this.ModelSavePath)
                        this.ContextSeed |> Option.iter (fun v -> w.WriteNumber("contextSeed", v))
                        ())
                )
