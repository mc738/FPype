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

    
    
    type GeneralSettings =
        { [<JsonPropertyName "hasHeaders">]
          HasHeaders: bool
          [<JsonPropertyName "separators">]
          Separators: char array
          [<JsonPropertyName "allowQuoting">]
          AllowQuoting: bool
          [<JsonPropertyName "readMultilines">]
          ReadMultilines: bool
          [<JsonPropertyName "trainingTestSplit">]
          TrainingTestSplit: float
          Columns: MLDataColumn list
          RowFilters: RowFilter list
          [<JsonPropertyName "transformations">]
          Transformations: ITransformationType list }

    type TrainBinaryClassificationModelAction =
        { [<JsonPropertyName "modelName">]
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

            member this.ToSerializedActionParameters() = failwith "todo"




    ()
