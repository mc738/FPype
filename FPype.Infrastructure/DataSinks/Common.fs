namespace FPype.Infrastructure.DataSinks

[<AutoOpen>]
module Common =

    open FPype.Data.Models

    [<RequireQualifiedAccess>]
    type DataSinkModelType =
        | Table of TableSchema
        | Object

    [<RequireQualifiedAccess>]
    type DataSinkType =
        | Push
        | Pull

    type DataSinkSettings =
        { Id: string
          SubscriptionId: string
          StorePath: string
          ModelType: DataSinkModelType
          Type: DataSinkModelType }

    
