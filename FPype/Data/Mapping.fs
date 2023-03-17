namespace FPype.Data

module Mapping =
    
    open System.Text.Json
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models

    [<RequireQualifiedAccess>]
    module ObjectTable =

        type MapperState =
            { ColumnValues: ColumnValue list }

            static member Initialize() = { ColumnValues = [] }

            member ms.AppendValue(value: ColumnValue) = ms.AppendValues([ value ])

            member ms.AppendValues(values: ColumnValue list) =
                { ms with ColumnValues = ms.ColumnValues @ values }

            member ms.CreateRow(table: TableModel) =
                table.Columns
                |> List.map (fun c ->
                    match ms.ColumnValues |> List.tryFind (fun cv -> cv.Column.Name = c.Name) with
                    | Some cv -> Ok cv.Value
                    | None ->
                        match c.Type with
                        | BaseType.Option _ -> Value.Option None |> Ok
                        | _ -> Error $"Missing value for column `{c.Name}`")
                |> flattenResultList
                |> Result.map TableRow.FromValues

        and ColumnValue =
            { Column: TableColumn
              Value: Value }

            static member Create(column: TableColumn, value: Value) = { Column = column; Value = value }

        let run (mapper: ObjectTableMap) (json: JsonElement) =

            let rec handler (state: MapperState) (element: JsonElement) (scope: ObjectTableMapScope) =

                let values =
                    scope.Columns
                    |> List.choose (fun otc ->
                        mapper.Table.Columns
                        |> List.tryFind (fun tc -> tc.Name = otc.Name)
                        |> Option.bind (fun c ->
                            // TODO use import handler??
                            match otc.Type with
                            | ObjectTableMapColumnType.Selector p ->
                                p.RunScalar(element)
                                |> Option.bind (fun el ->
                                    match Value.FromJsonValue(el, c.Type) with
                                    | CoercionResult.Success v -> Some v
                                    | _ -> None)
                                |> Option.map (fun v -> { Column = c; Value = v })

                            | ObjectTableMapColumnType.Constant v ->
                                Value.FromString(v, c.Type) |> Option.map (fun v -> { Column = c; Value = v })))

                let newState = state.AppendValues(values)

                match scope.IsBaseScope() with
                | true ->
                    // Base scope - so append any values and make the rows
                    [ newState.CreateRow(mapper.Table) ]

                | false ->
                    scope.InnerScopes
                    |> List.collect (fun is ->

                        is.Selector.Run(element) |> List.collect (fun el -> handler newState el is))

            handler (MapperState.Initialize()) json mapper.RootScope
