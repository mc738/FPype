namespace FPype.Actions


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
                | TableColumn i ->
                    tr.Values
                    |> List.item i
                    |> ObjectPropertyType.Value
                | Table rots ->

                    let parameters =
                        rots.ParameterIndexes
                        |> List.map (fun i -> tr.Values |> List.item i |> fun v -> v.Box())

                    let st =
                        store.BespokeSelectRows(rots.Table, rots.Query, parameters)

                    st.Rows
                    |> List.map (fun sr ->
                        ({ Name = rots.Name
                           Properties = rots.Properties |> List.map (createProperty sr store) }: ObjectModel))
                    |> ObjectPropertyType.Array

            ({ Name = pm.Name; Value = v }: ObjectProperty)

        let buildObjects (map: ObjectTableMap)  (store: PipelineStore) =
            let t =
                store.BespokeSelectRows(map.Table, map.Query, [])

            t.Rows
            |> List.map (fun r ->

                let p =
                    map.Properties |> List.map (createProperty r store)

                ({ Name = map.ObjectName
                   Properties = p }: ObjectModel))

    /// Aggregate data in a store and save the results in a new table.
    let aggregate (tableName) (columns) (sql) (parameters) (store: PipelineStore) =
        store.CreateTable(tableName, columns)
        |> fun t -> store.BespokeSelectRows(t, sql, parameters)
        |> store.InsertRows
        |> Result.map (fun r ->
            store.Log("aggregate", $"Aggregated and saved {r.Length} row(s) to table `{tableName}`.")
            store)

    type QueryData = { Condition: string; Values: obj list }

    let aggregateByDate
        (dates: DateGroups)
        (columns: TableColumn list)
        (tableName: string)
        (selectSql: string)
        (store: PipelineStore)
        =

        let t =
            { Name = tableName
              Columns = columns
              Rows = [] }

        // Create columns for the results table, with start and end dates plus label.        
        let rtc =
            [ { Name = dates.Label
                Type = BaseType.DateTime
                ImportHandler = None }
              { Name = "start_date"
                Type = BaseType.DateTime
                ImportHandler = None }
              { Name = "end_date"
                Type = BaseType.DateTime
                ImportHandler = None } ]
            @ columns
        
        let rt = store.CreateTable(tableName, rtc) 
        
        dates.Groups
        |> List.map (fun dg ->
            let condition =
                $"WHERE DATE({dates.FieldName}) >= DATE(@0) AND DATE({dates.FieldName}) < DATE(@1)"

            let parameters =
                [ dg.StartDate |> box
                  dg.EndDate |> box ]

            let r =
                store.BespokeSelectRows(t, $"{selectSql} {condition}", parameters)

            r.Rows
            |> List.map (fun r -> { r with Values = [ Value.String dg.Label; Value.DateTime dg.StartDate; Value.DateTime dg.EndDate ] @ r.Values })
            |> fun rs -> { rt with Rows = rs }
            |> store.InsertRows)
        |> fun _ -> Ok store

    let aggregateByDateAndCategory
        (dates: DateGroups)
        (categoryField: string)
        (columns: TableColumn list)
        (tableName: string)
        (selectSql: string)
        (store: PipelineStore)
        =

        let t =
            { Name = tableName
              Columns = columns
              Rows = [] }

        // Create columns for the results table, with start and end dates plus label.        
        let rtc =
            [ { Name = dates.Label
                Type = BaseType.DateTime
                ImportHandler = None }
              { Name = "start_date"
                Type = BaseType.DateTime
                ImportHandler = None }
              { Name = "end_date"
                Type = BaseType.DateTime
                ImportHandler = None } ]
            @ columns
        
        let rt = store.CreateTable(tableName, rtc) 
        
        dates.Groups
        |> List.map (fun dg ->
            let condition =
                $"WHERE DATE({dates.FieldName}) >= DATE(@0) AND DATE({dates.FieldName}) < DATE(@1) GROUP BY {categoryField}"

            let parameters =
                [ dg.StartDate |> box
                  dg.EndDate |> box ]

            let r =
                store.BespokeSelectRows(t, $"{selectSql} {condition}", parameters)

            r.Rows
            |> List.map (fun r -> { r with Values = [ Value.String dg.Label; Value.DateTime dg.StartDate; Value.DateTime dg.EndDate ] @ r.Values })
            |> fun rs -> { rt with Rows = rs }
            |> store.InsertRows)
        |> fun _ -> Ok store
    
    let mapToObject (mapper: ObjectTableMap) (store: PipelineStore) =
        
        use ms = new MemoryStream()

        let mutable opts = JsonWriterOptions()
        opts.Indented <- true
        
        use writer = new Utf8JsonWriter(ms, opts)

        writer.WriteStartArray()

        let objs = Internal.buildObjects mapper store

        objs
        |> List.iter (fun co -> co.ToJson writer)

        writer.WriteEndArray()
        
        writer.Flush()

        store.AddArtifact(mapper.ObjectName, "objects", "object", ms.ToArray())
        Ok store

