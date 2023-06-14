namespace FPype.Infrastructure.Configuration.Tables

open System
open FPype.Core.Types

[<AutoOpen>]
module Models =

    type NewTable =
        { Reference: string
          Name: string
          Version: NewTableVersion }

    and NewTableVersion =
        { Reference: string
          Columns: NewTableColumn list }

    and NewTableColumn =
        { Reference: string
          Name: string
          Type: BaseType
          Optional: bool
          ImportHandlerData: string option
          Index: int }

    type TableDetails =
        { Reference: string
          Name: string
          Versions: TableVersionDetails list }
    
    and TableVersionDetails =
        { TableReference: string
          Reference: string
          Name: string
          Version: int
          CreatedOn: DateTime
          Columns: TableColumnDetails list }

    and TableColumnDetails =
        { Reference: string
          Name: string
          Type: BaseType
          Optional: bool
          ImportHandlerData: string option
          Index: int }

    type TableVersionOverview =
        { TableReference: string
          Reference: string
          Name: string
          Version: int
          CreatedOn: DateTime }

    type TableOverview = { Reference: string; Name: string }
