namespace FPype.Interoperability.Common.Actions

open System.Text.Json.Serialization

module Import =
    
    type ImportFileAction =
            {
                [<JsonPropertyName "path">]
                Path: string
                [<JsonPropertyName "dataSourceName">]
                DataSourceName: string
            }
     
            interface IPipelineAction with
                
                member _.ActionType = nameof(ImportFileAction)