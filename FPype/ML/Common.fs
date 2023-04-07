namespace FPype.ML

open System
open FPype.Core.Types
open Microsoft.ML.Data

[<AutoOpen>]
module Common =
    
    type BaseType with
        
        member bt.ToDataKind() =
            let rec handler (bt: BaseType) =
                match bt with
                | BaseType.Boolean -> DataKind.Boolean
                | BaseType.Byte -> DataKind.Byte
                | BaseType.Char -> DataKind.String
                | BaseType.Decimal -> DataKind.Double
                | BaseType.Double -> DataKind.Double
                | BaseType.Float -> DataKind.Single
                | BaseType.Guid -> DataKind.String
                | BaseType.Int -> DataKind.Int32
                | BaseType.Long -> DataKind.Int64
                | BaseType.Short -> DataKind.Int16
                | BaseType.String -> DataKind.String
                | BaseType.DateTime -> DataKind.DateTime
                | BaseType.Option ibt -> handler ibt
                
            handler bt

