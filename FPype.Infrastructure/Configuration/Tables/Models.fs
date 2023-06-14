namespace FPype.Infrastructure.Configuration.Tables

open System
open FPype.Core.Types
open FPype.Infrastructure.Core.Persistence

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

        static member FromEntity
            (
                tableEntity: Records.TableModel,
                entity: Records.TableModelVersion,
                columns: Records.TableColumn list
            ) =
            ({ TableReference = tableEntity.Reference
               Reference = entity.Reference
               Name = tableEntity.Name
               Version = entity.Version
               CreatedOn = entity.CreatedOn
               Columns =
                 columns
                 |> List.map (fun tc -> TableColumnDetails.FromEntity(tc, BaseType.String)) }
            : TableVersionDetails)

    and TableColumnDetails =
        { Reference: string
          Name: string
          Type: BaseType
          Optional: bool
          ImportHandlerData: string option
          Index: int }

        static member FromEntity(entity: Records.TableColumn, ?defaultBaseType: BaseType) =
            ({ Reference = entity.Reference
               Name = entity.Name
               Type =
                 // If a default base type if provided that will be used in the (unlikely) event the base type is unknown.
                 // If none is provided and error will be thrown.
                 // However, this should be a rare occurrence.
                 BaseType.FromId(entity.DataType, entity.Optional)
                 |> Option.defaultWith (fun _ ->
                     match defaultBaseType with
                     | Some dbt -> dbt
                     | None -> failwith $"Unknown base type: `{entity.DataType}`")
               Optional = entity.Optional
               ImportHandlerData = entity.ImportHandlerJson
               Index = entity.ColumnIndex }
            : TableColumnDetails)


    type TableVersionOverview =
        { TableReference: string
          Reference: string
          Name: string
          Version: int
          CreatedOn: DateTime }

        static member FromEntity(tableEntity: Records.TableModel, entity: Records.TableModelVersion) =
            ({ TableReference = tableEntity.Reference
               Reference = entity.Reference
               Name = tableEntity.Name
               Version = entity.Version
               CreatedOn = entity.CreatedOn }
            : TableVersionOverview)

    type TableOverview =

        { Reference: string
          Name: string }

        static member FromEntity(entity: Records.TableModel) =
            { Reference = entity.Reference
              Name = entity.Name }
