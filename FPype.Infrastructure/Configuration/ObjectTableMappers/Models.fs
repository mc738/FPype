namespace FPype.Infrastructure.Configuration.ObjectTableMappers

[<AutoOpen>]
module Models =
    
    open System
    
    type NewObjectTableMapper =
        { Reference: string
          Name: string
          Version: NewObjectTableMapperVersion }

    and NewObjectTableMapperVersion = {
        Reference: string
        TableModelReference: string
        MapperData: string
    }

    type ObjectTableMapperDetails =
        { Reference: string
          Name: string
          Version: ObjectTableMapperVersionDetails }

    and ObjectTableMapperVersionDetails =
        { Reference: string
          Version: int
          TableModelReference: string
          MapperData: string
          Hash: string
          CreatedOn: DateTime }

