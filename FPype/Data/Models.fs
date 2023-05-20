namespace FPype.Data

open System
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

        (*
        static member FromViewModel(model: TableViewModel) =
            model.Fields
            |> List.ofSeq
            |> List.map (fun f -> f.Name, TableColumn.FromTabularModelField f)
            |> List.fold
                (fun (columns, errors) (name, column) ->
                    match column with
                    | Some c -> columns @ [ c ], errors
                    | None -> columns, errors @ [ name ])
                ([], [])
            |> fun (columns, errors) ->
                match errors |> List.isEmpty with
                | true ->
                    { Name = model.Name
                      Columns = columns
                      Rows = [] }
                    |> Ok
                | false ->
                    let errorsStr = String.concat ", " errors
                    Error $"The following columns could not be created: {errorsStr}."

        member tm.ToViewModel(id, height, width, x, y) =
            let tvm = TableViewModel()

            tvm.Name <- tm.Name

            tvm.Fields <-
                tm.Columns
                |> List.mapi (fun i c -> c.ToField(i))
                |> ResizeArray

            tvm.Id <- id
            tvm.Height <- height
            tvm.Width <- width
            tvm.X <- x
            tvm.Y <- y
            tvm.OriginalX <- x
            tvm.OriginalY <- y
            tvm
        *)

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

    and TableColumn =
        { Name: string
          Type: BaseType
          ImportHandler: (string -> CoercionResult) option }

        (*
        static member FromTabularModelField(field: Field) =
            BaseType.FromDataType(field.Type, field.Optional)
            |> Option.map (fun t ->
                { Name = field.Name
                  Type = t
                  ImportHandler = None })
        *)

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



    (*
        member tc.ToField(index: int) =
            let f = Field()
            f.Name <- tc.Name
            f.Type <- tc.Type.ToDateType()
            f.Optional <- tc.Type.IsOptionType()
            f.FieldIndex <- index
            f
        *)

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
                    let row, tail = r |> Array.splitAt r.Length

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
