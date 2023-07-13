namespace FPype.Infrastructure.DataSinks

[<AutoOpen>]
module Common =
    
    [<RequireQualifiedAccess>]
    type DataSinkModelType =
        | Table
        | Object
        
    [<RequireQualifiedAccess>]
    type DataSinkType =
        | Push
        | Pull
    
    type DataSinkSettings =
        {
            Id: string
            SubscriptionId: string
            StorePath: string
            ModelType: DataSinkModelType
            Type: DataSinkModelType
            
        }
    
    ()

