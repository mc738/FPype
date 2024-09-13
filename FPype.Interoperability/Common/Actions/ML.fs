namespace FPype.Interoperability.Common.Actions

module ML =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions
    
    type ITransformationType =
        
        abstract member TransformationType: string
        
    type ConcatenateTransformation =
        {
            [<JsonPropertyName "outputColumnName">]
            OutputColumnName: string
            Columns: string list
        }
    
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
            
            member this.GetActionName() = ML.``train-binary-classification-model``.name
            member this.ToSerializedActionParameters() = failwith "todo"

    
    
    
    ()
