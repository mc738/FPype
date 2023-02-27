namespace FPype.Actions

open FPype.Data.Models


[<RequireQualifiedAccess>]
module Transform =

    open System.IO
    open System.Text.Json
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

        let name = "aggregate-by-date"

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

        let name = "aggregate-by-date-and-category"

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
        let name = "map-to-object"

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