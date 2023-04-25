namespace FPype.Infrastructure.Configuration.Tables

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

    type TableVersionDetails =
        { TableReference: string
          Reference: string
          Name: string
          Version: int
          Columns: TableColumnDetails list }

    and TableColumnDetails =
        { Reference: string
          Name: string
          Type: BaseType
          Optional: bool
          ImportHandlerData: string option
          Index: int }