namespace FPype.Actions

open FPype.Core.Types
open FPype.Data.Mapping
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
                           Properties = rots.Properties |> List.map (createProperty sr store) }
                        : ObjectModel))
                    |> ObjectPropertyType.Array

            ({ Name = pm.Name; Value = v }: ObjectProperty)

        let buildObjects (map: TableObjectMap) (store: PipelineStore) =
            let t = store.BespokeSelectRows(map.Table, map.Query, [])

            t.Rows
            |> List.map (fun r ->

                let p = map.Properties |> List.map (createProperty r store)

                ({ Name = map.ObjectName
                   Properties = p }
                : ObjectModel))

    [<RequireQualifiedAccess>]
    module ``execute-query`` =
        // NOTE This is currently the same as aggregate. However the name is better.
        
        let name = "execute_query"

        type Parameters =
            { Table: TableModel
              Sql: string
              Parameters: obj list }

        let run (parameters: Parameters) (store: PipelineStore) =
            store.CreateTable(parameters.Table)
            |> fun t -> store.BespokeSelectRows(t, parameters.Sql, parameters.Parameters)
            |> store.InsertRows
            |> Result.map (fun r ->
                store.Log(name, $"Executed query and saved {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name
    
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

        let name = "map_object_to_table"

        type Parameters =
            { DataSource: string
              Map: ObjectTableMap }

        let run (parameters: Parameters) (store: PipelineStore) =

            match
                getDataSourceAsStringByName store parameters.DataSource
                |> Result.bind toJsonElement
            with
            | Ok json ->
                let rows =
                    ObjectTable.run parameters.Map json
                    |> List.fold
                        (fun (acc) r ->
                            match r with
                            | Ok row -> row :: acc
                            | Error e ->
                                store.LogError(name, $"Error creating row: {e}")
                                acc)
                        ([])
                    |> List.rev

                store.CreateTable(parameters.Map.Table)
                |> fun m -> m.SetRows rows
                |> store.InsertRows
                |> Result.map (fun r ->
                    store.Log(
                        name,
                        $"Imported {r.Length} row(s) from object `{parameters.DataSource}` to table `{parameters.Map.Table.Name}`."
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

    [<RequireQualifiedAccess>]
    module ``pivot`` =

        let name = "pivot"


        let run () (store: PipelineStore) =
            // Parameters
            let bt =
                ({ Name = "avg_vales"
                   Columns =
                     [ { Name = "avg_variance"
                         Type = BaseType.Decimal
                         ImportHandler = None }
                       { Name = "entry_year"
                         Type = BaseType.Int
                         ImportHandler = None }
                       { Name = "entry_month"
                         Type = BaseType.Int
                         ImportHandler = None }
                       { Name = "entry_day"
                         Type = BaseType.Int
                         ImportHandler = None } ]
                   Rows = [] })

            let ct =
                ({ Name = "categories_table"
                   Columns =
                     [ ({ Name = "category"
                          Type = BaseType.String
                          ImportHandler = None }
                       : TableColumn) ]
                   Rows = [] }
                : TableModel)
                
            let bct =
                ({ Name = "by_cat"
                   Columns =
                     [ { Name = "avg_variance"
                         Type = BaseType.Decimal
                         ImportHandler = None }
                       { Name = "entry_year"
                         Type = BaseType.Int
                         ImportHandler = None }
                       { Name = "entry_month"
                         Type = BaseType.Int
                         ImportHandler = None }
                       { Name = "entry_day"
                         Type = BaseType.Int
                         ImportHandler = None } ]
                   Rows = [] })

            let q1 = "SELECT DISTINCT (gics_sector) FROM sp500_prices"
            
            let q2 = "SELECT avg_variance, entry_year, entry_month, entry_day FROM total GROUP BY GROUP BY entry_year, entry_month, entry_day"
            
            let q3 = "SELECT avg_variance FROM by_cat WHERE industry = @0 AND entry_year = @1 AND entry_month = @2 AND entry_day = @3;"

            let catIndex = 0

            let newTableName = ""

            let catValueType = BaseType.Decimal

            // 1. Query to get categories
            let ctr = store.SelectRows(ct, q1, [])

            let cats = ctr.Rows |> List.choose (fun tr -> tr.Values |> List.tryItem catIndex)

            // Create new table

            // Combine base table and category columns
            let pt =
                ({ Name = newTableName
                   Columns =
                     bt.Columns
                     @ (cats
                        |> List.map (fun c ->
                            { Name = c.GetString()
                              Type = catValueType
                              ImportHandler = None }))
                   Rows = [] })

            // Add table here??
            
            // Build up rows
            //store.sele




            // 1.a. Get specific



            ()

        ()
