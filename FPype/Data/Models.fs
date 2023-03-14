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
            { tm with Columns = columns @ tm.Columns }

        member tm.AppendColumns(columns: TableColumn list) =
            { tm with Columns = tm.Columns @ columns }

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
                   Rows = r }: TableModel)

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
          Columns: ObjectTableMapColumns list
          InnerScopes: ObjectTableMapScope list }

    and ObjectTableMapColumns =
        { Name: string
          Type: ObjectTableMapColumnType }

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
