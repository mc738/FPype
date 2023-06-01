namespace FPype.Data

open System
open System.IO
open System.Text
open System.Text.Json
open FPype.Core
open FPype.Core.JPath
open FPype.Core.Types
open FsToolbox.Core

module Models =

    type TableModel =
        { Name: string
          Columns: TableColumn list
          Rows: TableRow list }

        static member TryDeserialize(data: byte array) =
            // Table consists of
            // 1. Head (table details)
            //     a. Name length
            //     b. Name
            // 2. Columns
            //    a. Length
            //    b. Count
            //    c. Data
            // 3. Rows
            //    a. Length
            //    b. Count
            //    c. Data

            match data.Length >= 4 with
            | true ->
                let (nlb, tail) = data |> Array.splitAt 4

                let nLen = BitConverter.ToInt32 nlb

                match tail.Length >= nLen with
                | true -> Ok(tail |> Array.splitAt nLen)
                | false -> Error "Unable to deserialize table name"
            | false -> Error "Data length is too short"
            |> Result.bind (fun (nb, data) ->
                let name = nb |> Encoding.UTF8.GetString

                // 2. Columns
                //    a. Length
                //    b. Count
                //    c. Data

                match data.Length >= 8 with
                | true ->
                    let (b, tail) = data |> Array.splitAt 8

                    let lb, cb = b |> Array.splitAt 4

                    let len = BitConverter.ToInt32 lb
                    let count = BitConverter.ToInt32 cb

                    match tail.Length >= len with
                    | true ->
                        let (cd, tail) = tail |> Array.splitAt len

                        let rec handle (acc, i, d) =
                            match i < count with
                            | true ->
                                match TableColumn.TryDeserialize(d) with
                                | Ok(tc, t) -> handle (acc @ [ tc ], i + 1, t)
                                | Error e -> Error $"Failed to deserialize column {i}. Error - {e}"
                            | false -> Ok acc

                        handle ([], 0, cd) |> Result.map (fun tcs -> name, tcs, tail)
                    | false -> Error "Unable to deserialize columns: data too short"
                | false -> Error "Column header data length is too short")
            |> Result.bind (fun (name, tcs, data) ->
                match data |> Array.isEmpty with
                | true -> Ok(name, tcs, [], data)
                | false ->
                    let (b, tail) = data |> Array.splitAt 8

                    let lb, cb = b |> Array.splitAt 4

                    let len = BitConverter.ToInt32 lb
                    let count = BitConverter.ToInt32 cb

                    match tail.Length >= len with
                    | true ->
                        let (cd, tail) = tail |> Array.splitAt len

                        let rec handle (acc, i, d) =
                            match i < count with
                            | true ->
                                match TableRow.TryDeserialize(d) with
                                | Ok(tr, t) -> handle (acc @ [ tr ], i + 1, t)
                                | Error e -> Error $"Failed to deserialize row {i}. Error - {e}"
                            | false -> Ok acc

                        handle ([], 0, cd) |> Result.map (fun trs -> name, tcs, trs, tail)
                    | false -> Error "Unable to deserialize rows: data too short")
            |> Result.map (fun (name, tcs, trs, tail) ->
                { Name = name
                  Columns = tcs
                  Rows = trs },
                tail)

        static member FromSchema(schema: TableSchema) =
            { Name = schema.Name
              Columns = schema.Columns |> List.map TableColumn.FromSchema
              Rows = [] }

        member tm.PrependColumns(columns: TableColumn list) =
            { tm with
                Columns = columns @ tm.Columns }

        member tm.AppendColumns(columns: TableColumn list) =
            { tm with
                Columns = tm.Columns @ columns }

        member tm.SetRows(rows: TableRow list) = { tm with Rows = rows }

        member tm.PrependRows(rows: TableRow list) = { tm with Rows = tm.Rows @ rows }

        member tm.AppendRows(rows: TableRow list) = { tm with Rows = rows @ tm.Rows }

        member tm.Max(columnIndex: int) =
            tm.Rows |> List.maxBy (fun tr -> tr.Values |> List.item columnIndex)

        member tm.ColumnToCollection(columnIndex: int) =
            tm.Rows |> List.map (fun tr -> tr.Values |> List.item columnIndex)

        member tm.ColumnToDecimalCollection(columnIndex: int) =
            tm.Rows
            |> List.map (fun tr -> tr.Values |> List.item columnIndex |> (fun c -> c.GetDecimal()))

        /// Join with another table and drop set columns.
        /// Any matches not found in the second table will be replaced with `Value.Option None`
        /// This might not be the most efficient and queries might prove a better approach in many cases.
        member tm.Join
            (
                newName: string,
                table2: TableModel,
                tableAColumnIndex: int,
                tableBColumnIndex: int,
                dropColumnsA: int list,
                dropColumnsB: int list
            ) =
            let dropValues (tr: TableRow) (dropIndexes: int list) =
                tr.Values
                |> List.mapi (fun i v ->
                    match dropIndexes |> List.contains i with
                    | true -> None
                    | false -> Some v)
                |> List.choose id

            tm.Rows
            |> List.map (fun tr ->
                let v = tr.Values.[tableAColumnIndex]

                let t2r =
                    table2.Rows
                    |> List.tryPick (fun t2r ->
                        match t2r.MatchValue(v, tableBColumnIndex) with
                        | true -> Some t2r
                        | false -> None)
                    |> Option.defaultValue (
                        (fun _ -> Value.Option None)
                        |> List.init table2.Columns.Length
                        |> fun v -> { Values = v }
                    )

                ({ Values = dropValues tr dropColumnsA @ dropValues t2r dropColumnsB }: TableRow))
            |> fun r ->
                ({ Name = newName
                   Columns =
                     TableColumn.JoinColumns(tm.Columns, table2.Columns, dropColumnsA, dropColumnsB, false, true)
                   Rows = r }
                : TableModel)

        member tm.ToCsv(settings: CsvExportSettings) =
            [ match settings.IncludeHeader with
              | true -> tm.Columns |> List.map (fun c -> c.Name) |> String.concat ","
              | false -> ()

              yield! tm.Rows |> List.map (fun tr -> tr.ToCsv(settings)) ]

        member tm.Serialize() =
            // Table consists of
            // 1. Head (table details)
            //     a. Name length
            //     b. Name
            // 2. Columns
            //    a. Length
            //    b. Count
            //    c. Data
            // 3. Rows
            //    a. Length
            //    b. Count
            //    c. Data

            let nb = tm.Name |> Encoding.UTF8.GetBytes

            let tcb = tm.Columns |> List.map (fun tc -> tc.Serialize()) |> Array.concat
            let trb = tm.Rows |> List.map (fun tr -> tr.Serialize()) |> Array.concat

            [| yield! nb.Length |> BitConverter.GetBytes
               yield! nb
               yield! tcb.Length |> BitConverter.GetBytes
               yield! tm.Columns.Length |> BitConverter.GetBytes
               yield! tcb
               yield! trb.Length |> BitConverter.GetBytes
               yield! tm.Rows.Length |> BitConverter.GetBytes
               yield! trb |]

        member tm.ToSchema() =
            ({ Name = tm.Name
               Columns = tm.Columns |> List.map (fun tc -> tc.ToSchema()) }
            : TableSchema)

        member tm.GetSchemaJson() = tm.ToSchema().ToJson()

    and TableColumn =
        { Name: string
          Type: BaseType
          ImportHandler: (string -> CoercionResult) option }

        static member TryDeserialize(data: byte array) =
            // The serialized column contains
            // 1. The type (as a byte) (index 0)
            // 2. Is optional (index 1)
            // 3. Name length (index 2, 3, 4, 5)
            // 4. The name (6 - onwards)

            match data.Length >= 6 with
            | true ->
                let h, t = data |> Array.splitAt 6

                let btb = h[0]
                let o = [| h[1] |] |> BitConverter.ToBoolean

                let len = data |> Array.splitAt 2 |> snd |> BitConverter.ToInt32

                match t.Length >= len with
                | true ->
                    BaseType.TryFromByte(btb, o)
                    |> Result.map (fun bt ->
                        let nb, rb = t |> Array.splitAt len

                        { Name = Encoding.UTF8.GetString nb
                          Type = bt
                          ImportHandler = None },
                        rb)
                | false -> Error "Unable to deserialize column name"
            | false -> Error "Data length is too short"

        static member FromSchema(schema: TableColumnSchema) =
            { Name = schema.Name
              Type = schema.Type
              ImportHandler = None }

        static member JoinColumns
            (
                c1,
                c2,
                dropColumnsA: int list,
                dropColumnsB: int list,
                c1Optional: bool,
                c2Optional: bool
            ) =
            let handler (c: TableColumn list) (dropIndexes: int list) (makeOptional: bool) =
                c
                |> List.mapi (fun i tc ->
                    match dropIndexes |> List.contains i with
                    | true -> None
                    | false -> Some tc)
                |> List.choose id
                |> fun r ->
                    match makeOptional with
                    | true ->
                        r
                        |> List.map (fun tc ->
                            match tc.Type with
                            | BaseType.Option _ as bt -> tc
                            | bt -> { tc with Type = BaseType.Option bt })
                    | false -> r

            handler c1 dropColumnsA c1Optional @ handler c2 dropColumnsB c2Optional


        member tc.Serialize() =
            let nb = tc.Name |> Encoding.UTF8.GetBytes

            [| tc.Type.ToByte()
               match tc.Type.IsOptionType() with
               | true -> 1uy
               | false -> 0uy
               yield! BitConverter.GetBytes nb.Length
               yield! nb |]

        member tc.ToSchema() =
            ({ Name = tc.Name; Type = tc.Type }: TableColumnSchema)

    and TableRow =
        { Values: Value list }

        static member FromValues(values: Value list) = { Values = values }

        static member TryDeserialize(data: byte array) =
            // NOTE this will fail if the row length is bigger than Int32.MaxValue
            // This should be fine, rows shouldn't be bigger than this (2147483647 bytes).
            match data |> Array.length >= 4 with
            | true ->
                let l, r = data |> Array.splitAt 4
                let len = BitConverter.ToInt32 l

                match r.Length >= len with
                | true ->
                    let row, tail = r |> Array.splitAt len

                    let rec run (acc: Value list, remaining: byte array) =
                        match Value.TryDeserialize remaining with
                        | Ok(v, t) ->
                            match t |> Array.isEmpty with
                            | true -> acc @ [ v ] |> Ok
                            | false -> run (acc @ [ v ], t)
                        | Error e -> Error e

                    run ([], row) |> Result.map (fun vs -> { Values = vs }, tail)
                | false -> Error "Row length is too short"
            | false -> Error "Data length is too short"

        member tr.Box() =
            tr.Values
            |> List.map (fun v ->
                match v with
                | Value.Option iv ->
                    match iv with
                    | None -> box DBNull.Value
                    | Some v -> v.Box()
                | _ -> v.Box())

        member tr.GetDecimal(i) = tr.Values.[i]

        member tr.MatchValue(v: Value, index: int) = tr.Values.[index].IsMatch(v)

        member tr.ToCsv(settings: CsvExportSettings) =
            let wrapString (v: string) =
                v.Replace("\"", "\"\"\"") |> fun v -> $"\"{v}\""

            let rec handler (v: Value) =
                match v with
                | Value.Boolean b ->
                    match settings.BoolToWord, b with
                    | true, true ->
                        match settings.WrapAllValues, settings.WrapBools with
                        | true, _
                        | _, true -> "true" |> wrapString
                        | false, false -> "true"
                    | true, false ->
                        match settings.WrapAllValues, settings.WrapBools with
                        | true, _
                        | _, true -> "false" |> wrapString
                        | false, false -> "false"
                    | false, true ->
                        match settings.WrapAllValues, settings.WrapBools with
                        | true, _
                        | _, true -> "1" |> wrapString
                        | false, false -> "1"
                    | false, false ->
                        match settings.WrapAllValues, settings.WrapBools with
                        | true, _
                        | _, true -> "0" |> wrapString
                        | false, false -> "0"
                | Value.Byte b ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string b |> wrapString
                    | false, false -> string b
                | Value.Char c ->
                    match settings.WrapStrings, settings.WrapAllValues with
                    | true, _
                    | _, true -> string c |> wrapString
                    | false, false -> string c
                | Value.DateTime d ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true ->
                        match settings.DefaultDateTimeFormation with
                        | Some f -> d.ToString(f)
                        | None -> d.ToString()
                        |> wrapString
                    | false, false ->
                        match settings.DefaultDateTimeFormation with
                        | Some f -> d.ToString(f)
                        | None -> d.ToString()
                | Value.Decimal d ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string d |> wrapString
                    | false, false -> string d
                | Value.Double d ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string d |> wrapString
                    | false, false -> string d
                | Value.Float f ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string f |> wrapString
                    | false, false -> string f
                | Value.Guid g ->
                    match settings.WrapGuids, settings.WrapAllValues with
                    | true, _
                    | _, true ->
                        match settings.DefaultGuidFormat with
                        | Some f -> g.ToString(f)
                        | None -> g.ToString()
                        |> wrapString
                    | false, false ->
                        match settings.DefaultGuidFormat with
                        | Some f -> g.ToString(f)
                        | None -> g.ToString()
                | Value.Int i ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string i |> wrapString
                    | false, false -> string i
                | Value.Long l ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string l |> wrapString
                    | false, false -> string l
                | Value.Short s ->
                    match settings.WrapNumbers, settings.WrapAllValues with
                    | true, _
                    | _, true -> string s |> wrapString
                    | false, false -> string s
                | Value.String s ->
                    match settings.WrapStrings, settings.WrapAllValues with
                    | true, _
                    | _, true -> string s |> wrapString
                    | false, false -> string s
                | Value.Option ov ->
                    match ov with
                    | Some iv -> handler iv
                    | None -> ""

            tr.Values |> List.map handler |> String.concat ","

        member tr.TryGetValue(index: int) = tr.Values |> List.tryItem index

        member tr.Serialize() =
            let sv = tr.Values |> List.map (fun v -> v.Serialize()) |> Array.concat

            [| yield! BitConverter.GetBytes sv.Length; yield! sv |]

    /// <summary>
    /// A table schema is simplified representation of a TableModel.
    /// The are designed to allow the store to save a representation of a table in the data store and
    /// provide an easy way for clients to show the table layout.
    /// </summary>
    and TableSchema =
        { Name: string
          Columns: TableColumnSchema list }

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "name" json, Json.tryGetArrayProperty "columns" json with
            | Some n, Some c ->
                c
                |> List.map TableColumnSchema.FromJson
                |> flattenResultList
                |> Result.map (fun cs -> { Name = n; Columns = cs })
            | None, _ -> Serialization.missingProperty "name"
            | _, None -> Serialization.missingProperty "columns"

        member ts.ToJson() =
            use ms = new MemoryStream()

            let mutable opts = JsonWriterOptions()
            opts.Indented <- true

            use writer = new Utf8JsonWriter(ms, opts)

            ts.WriteToJson writer

            writer.Flush()

            ms.ToArray() |> Encoding.UTF8.GetString

        member internal ts.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            writer.WriteString("name", ts.Name)

            Json.writeArray (fun w -> ts.Columns |> List.iter (fun c -> c.WriteToJson w)) "columns" writer

            writer.WriteEndObject()

    and TableColumnSchema =
        { Name: string
          Type: BaseType }

        static member FromJson(json: JsonElement) =
            match
                Json.tryGetStringProperty "name" json,
                Json.tryGetStringProperty "type" json,
                Json.tryGetBoolProperty "optional" json
            with
            | Some n, Some t, Some o ->
                match BaseType.FromId(t, o) with
                | Some bt -> Ok { Name = n; Type = bt }
                | None -> Error $"Failed to deserialize base type for column `{n}` (base type: {t})"
            | None, _, _ -> Serialization.missingProperty "name"
            | _, None, _ -> Serialization.missingProperty "type"
            | _, _, None -> Serialization.missingProperty "optional"

        member internal tcs.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            writer.WriteString("name", tcs.Name)

            writer.WriteString("type", tcs.Type.Serialize())
            writer.WriteBoolean("optional", tcs.Type.IsOptionType())

            writer.WriteEndObject()

    type ObjectDefinition =
        { Name: string
          Properties: PropertyDefinition list }

    and PropertyDefinition = { Name: string; Type: PropertyType }

    and PropertyType =
        | Value of BaseType
        | Object of ObjectDefinition
        | Array of ObjectDefinition list
        | FixedArray of ObjectDefinition

    /// <summary>
    /// A record representing mapping of values from a table to an object.
    /// </summary>
    and TableObjectMap =
        { ObjectName: string
          Table: TableModel
          Query: string
          Properties: PropertyMap list }

    and PropertyMap =
        { Name: string; Source: PropertySource }

    and PropertySource =
        | TableColumn of int
        | Table of RelatedObjectTableSource

    and RelatedObjectTableSource =
        { Name: string
          Query: string
          Table: TableModel
          ParameterIndexes: int list
          Properties: PropertyMap list }

    type ObjectModel =
        { Name: string
          Properties: ObjectProperty list }

        member om.ToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()

            om.Properties |> List.iter (fun op -> op.ToJson writer)

            writer.WriteEndObject()

    and ObjectProperty =
        { Name: string
          Value: ObjectPropertyType }

        member op.ToJson(writer: Utf8JsonWriter) =
            //writer.WritePropertyName op.Name

            let writeObj (om: ObjectModel) (jw: Utf8JsonWriter) = om.ToJson jw

            let writeArr (oma: ObjectModel list) (jw: Utf8JsonWriter) =
                oma |> List.iter (fun om -> om.ToJson jw)

            let writeValue (v: Value) (jw: Utf8JsonWriter) =
                let rec handler value =
                    match value with
                    | Value.Boolean b -> jw.WriteBoolean(op.Name, b)
                    | Value.Byte b -> jw.WriteNumber(op.Name, int b)
                    | Value.Char c -> jw.WriteString(op.Name, string c)
                    | Value.Decimal d -> jw.WriteNumber(op.Name, d)
                    | Value.Double d -> jw.WriteNumber(op.Name, d)
                    | Value.Float f -> jw.WriteNumber(op.Name, f)
                    | Value.Guid g -> jw.WriteString(op.Name, g)
                    | Value.Int i -> jw.WriteNumber(op.Name, i)
                    | Value.Long l -> jw.WriteNumber(op.Name, l)
                    | Value.Short s -> jw.WriteNumber(op.Name, int s)
                    | Value.String s -> jw.WriteString(op.Name, s)
                    | Value.DateTime dt -> jw.WriteString(op.Name, dt)
                    | Value.Option vo ->
                        match vo with
                        | Some v1 -> handler v1
                        | None -> jw.WriteNull(op.Name)

                handler v

            match op.Value with
            | Value v -> writeValue v writer
            | Object o -> Json.writePropertyObject (writeObj o) op.Name writer
            | Array a -> Json.writeArray (writeArr a) op.Name writer

    and ObjectPropertyType =
        | Value of Value
        | Object of ObjectModel
        | Array of ObjectModel list

    type ObjectTableMap =
        { Table: TableModel
          RootScope: ObjectTableMapScope }

    and ObjectTableMapScope =
        { Selector: JPath
          Columns: ObjectTableMapColumn list
          InnerScopes: ObjectTableMapScope list }

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "selector" element with
            | Some selector ->
                let columns =
                    Json.tryGetArrayProperty "columns" element
                    |> Option.map (fun cs -> cs |> List.map ObjectTableMapColumn.FromJson |> flattenResultList)
                    |> Option.defaultValue (Ok [])

                let innerScopes =
                    Json.tryGetArrayProperty "innerScopes" element
                    |> Option.map (fun iss -> iss |> List.map ObjectTableMapScope.FromJson |> flattenResultList)
                    |> Option.defaultValue (Ok [])

                match JPath.Compile(selector), columns, innerScopes with
                | Ok s, Ok c, Ok is ->
                    { Selector = s
                      Columns = c
                      InnerScopes = is }
                    |> Ok
                | Error e, _, _ -> Error e
                | _, Error e, _ -> Error e
                | _, _, Error e -> Error e
            | None -> Error "Missing selector property"

        member otm.IsBaseScope() = otm.InnerScopes.IsEmpty

    and ObjectTableMapColumn =
        { Name: string
          Type: ObjectTableMapColumnType }

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "name" element with
            | Some name ->
                ObjectTableMapColumnType.FromJson element
                |> Result.map (fun ct -> { Name = name; Type = ct })
            | None -> Error "Missing name property"

    and ObjectTableMapColumnType =
        | Selector of JPath
        | Constant of string

        static member FromJson(element: JsonElement) =
            match Json.tryGetStringProperty "type" element with
            | Some "selector" ->
                Json.tryGetStringProperty "selector" element
                |> Option.map (fun s -> JPath.Compile(s) |> Result.map Selector)
                |> Option.defaultWith (fun _ -> Error "Missing selector property")
            | Some "constant" ->
                Json.tryGetStringProperty "value" element
                |> Option.map (Constant >> Ok)
                |> Option.defaultWith (fun _ -> Error "Missing value property")
            | Some t -> Error $"Unknown column type: `{t}`"
            | None -> Error $"Missing type property"
