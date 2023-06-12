namespace FPype.Infrastructure.Configuration.TableObjectMappers

[<AutoOpen>]
module Models =

    open System
    
    type NewTableObjectMapper =
        { Reference: string
          Name: string
          Version: NewTableObjectMapperVersion }

    and NewTableObjectMapperVersion = { Reference: string; MapperData: string }

    type TableObjectMapperDetails =
        { Reference: string
          Name: string
          Version: TableObjectMapperVersionDetails }

    and TableObjectMapperVersionDetails =
        { Reference: string
          Version: int
          MapperData: string
          Hash: string
          CreatedOn: DateTime }
        
    type TableObjectMapperVersionOverview =
        { MapperReference: string
          Reference: string
          Name: string
          Version: int }

    type TableObjectMapperOverview = { Reference: string; Name: string }
