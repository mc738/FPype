namespace FPype.Infrastructure.DataSinks

open System
open FPype.Data.ModelExtensions

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

    type ReadRequest =
        { RequestId: string
          Requester: string
          RequestTimestamp: DateTime
          WasSuccessful: bool }
