﻿namespace FPype.Actions

open FPype.Core.Types
open FPype.Data.Models
open FPype.Data.Store
open Microsoft.FSharp.Core


[<RequireQualifiedAccess>]
module Transform =

    open System.IO
    open System.Text.Json
    open FPype.Core
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store
    open FPype.Data.Grouping

    module Internal =

        let rec createProperty (tr: TableRow) (store: PipelineStore) (pm: PropertyMap) =
            let v =
                match pm.Source with
                | TableColumn i -> tr.Values |> List.item i |> ObjectPropertyType.Value
                | Table rots ->

                    let parameters =
                        rots.ParameterIndexes
                        |> List.map (fun i -> tr.Values |> List.item i |> (fun v -> v.Box()))

                    let st = store.BespokeSelectRows(rots.Table, rots.Query, parameters)

                    st.Rows
                    |> List.map (fun sr ->
                        ({ Name = rots.Name
                           Properties = rots.Properties |> List.map (createProperty sr store) }: ObjectModel))
                    |> ObjectPropertyType.Array

            ({ Name = pm.Name; Value = v }: ObjectProperty)

        let buildObjects (map: TableObjectMap) (store: PipelineStore) =
            let t = store.BespokeSelectRows(map.Table, map.Query, [])

            t.Rows
            |> List.map (fun r ->

                let p = map.Properties |> List.map (createProperty r store)

                ({ Name = map.ObjectName
                   Properties = p }: ObjectModel))

    [<RequireQualifiedAccess>]
    module ``aggregate`` =
        let name = "aggregate"

        type Parameters =
            { Table: TableModel
              Sql: string
              Parameters: obj list }

        let run (parameters: Parameters) (store: PipelineStore) =
            store.CreateTable(parameters.Table)
            |> fun t -> store.BespokeSelectRows(t, parameters.Sql, parameters.Parameters)
            |> store.InsertRows
            |> Result.map (fun r ->
                store.Log("aggregate", $"Aggregated and saved {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    [<RequireQualifiedAccess>]
    module ``aggregate-by-date`` =

        let name = "aggregate_by_date"

        type Parameters =
            { Table: TableModel
              SelectSql: string
              DateGroups: DateGroups }

        let run (parameters: Parameters) (store: PipelineStore) =

            // Create columns for the results table, with start and end dates plus label.
            let rtc =
                [ { Name = parameters.DateGroups.Label
                    Type = BaseType.DateTime
                    ImportHandler = None }
                  { Name = "start_date"
                    Type = BaseType.DateTime
                    ImportHandler = None }
                  { Name = "end_date"
                    Type = BaseType.DateTime
                    ImportHandler = None } ]

            let rt = store.CreateTable(parameters.Table.PrependColumns(rtc))

            parameters.DateGroups.Groups
            |> List.map (fun dg ->
                let condition =
                    $"WHERE DATE({parameters.DateGroups.FieldName}) >= DATE(@0) AND DATE({parameters.DateGroups.FieldName}) < DATE(@1)"

                let queryParameters = [ dg.StartDate |> box; dg.EndDate |> box ]

                let r =
                    store.BespokeSelectRows(parameters.Table, $"{parameters.SelectSql} {condition}", queryParameters)

                r.Rows
                |> List.map (fun r ->
                    { r with
                        Values =
                            [ Value.String dg.Label
                              Value.DateTime dg.StartDate
                              Value.DateTime dg.EndDate ]
                            @ r.Values })
                |> fun rs -> { rt with Rows = rs }
                |> store.InsertRows)
            |> fun _ -> Ok store

        let createAction (parameters: Parameters) = run parameters |> createAction name

    [<RequireQualifiedAccess>]
    module ``aggregate-by-date-and-category`` =

        let name = "aggregate_by_date_and_category"

        type Parameters =
            { Table: TableModel
              SelectSql: string
              DateGroups: DateGroups
              CategoryField: string }

        let run (parameters: Parameters) (store: PipelineStore) =


            // Create columns for the results table, with start and end dates plus label.
            let rtc =
                [ { Name = parameters.DateGroups.Label
                    Type = BaseType.DateTime
                    ImportHandler = None }
                  { Name = "start_date"
                    Type = BaseType.DateTime
                    ImportHandler = None }
                  { Name = "end_date"
                    Type = BaseType.DateTime
                    ImportHandler = None } ]

            let rt = store.CreateTable(parameters.Table.PrependColumns(rtc))

            parameters.DateGroups.Groups
            |> List.map (fun dg ->
                let condition =
                    $"WHERE DATE({parameters.DateGroups.FieldName}) >= DATE(@0) AND DATE({parameters.DateGroups.FieldName}) < DATE(@1) GROUP BY {parameters.CategoryField}"

                let queryParameters = [ dg.StartDate |> box; dg.EndDate |> box ]

                let r =
                    store.BespokeSelectRows(parameters.Table, $"{parameters.SelectSql} {condition}", queryParameters)

                r.Rows
                |> List.map (fun r ->
                    { r with
                        Values =
                            [ Value.String dg.Label
                              Value.DateTime dg.StartDate
                              Value.DateTime dg.EndDate ]
                            @ r.Values })
                |> fun rs -> { rt with Rows = rs }
                |> store.InsertRows)
            |> fun _ -> Ok store

        let createAction (parameters: Parameters) = run parameters |> createAction name

    [<RequireQualifiedAccess>]
    module ``map-to-object`` =
        let name = "map_to_object"

        let run (mapper: TableObjectMap) (store: PipelineStore) =

            use ms = new MemoryStream()

            let mutable opts = JsonWriterOptions()
            opts.Indented <- true

            use writer = new Utf8JsonWriter(ms, opts)

            writer.WriteStartArray()

            let objs = Internal.buildObjects mapper store

            objs |> List.iter (fun co -> co.ToJson writer)

            writer.WriteEndArray()

            writer.Flush()

            store.AddArtifact(mapper.ObjectName, "objects", "object", ms.ToArray())
            Ok store

        let createAction (mapper: TableObjectMap) = run mapper |> createAction name

    [<RequireQualifiedAccess>]
    module ``map-object-to-table`` =

        [<AutoOpen>]
        module private Internal =

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

        let name = "map_object_to_table"

        let run (mapper: ObjectTableMap) (store: PipelineStore) =

            let rec handler (state: MapperState) (element: JsonElement) (scope: ObjectTableMapScope) =

                let values =
                    scope.Columns
                    |> List.choose (fun otc ->
                        mapper.Table.Columns
                        |> List.tryFind (fun tc -> tc.Name = otc.Name)
                        |> Option.bind (fun c ->
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

            let initialState = ()

            match getDataSourceAsStringByName store "" |> Result.bind toJsonElement with
            | Ok json ->
                let rows =
                    handler (MapperState.Initialize()) json mapper.RootScope
                    |> List.fold
                        (fun (acc) r ->
                            match r with
                            | Ok row -> row :: acc
                            | Error e ->
                                store.LogError(name, $"Error creating row: {e}")
                                acc)
                        ([])
                    |> List.rev

                store.CreateTable(mapper.Table)
                |> fun m -> m.SetRows rows
                |> store.InsertRows
                |> Result.map (fun r ->
                    store.Log(
                        name,
                        $"Imported {r.Length} row(s) from object `{ds.Name}` to table `{mapper.Table.Name}`."
                    )

                    store)
            | Error e -> Error e


    [<RequireQualifiedAccess>]
    module ``merge-results`` =

        let name = "merge_results"

        type Parameters =
            { Table: TableModel
              Queries: string list }

        let run (parameters: Parameters) (store: PipelineStore) =
            let t = store.CreateTable(parameters.Table)

            parameters.Queries
            |> List.fold (fun t q -> store.BespokeSelectAndAppendRows(t, q, [])) t
            |> store.InsertRows
            |> Result.map (fun r ->
                store.Log("merge_results", $"Merged and saved {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name


    [<RequireQualifiedAccess>]
    module ``map-to-table`` =

        ()
