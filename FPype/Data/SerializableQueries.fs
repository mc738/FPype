namespace FPype.Data

open System.IO
open System.Text
open System.Text.Json
open System.Text.Unicode
open DocumentFormat.OpenXml.Spreadsheet
open FsToolbox.Core
open FPype.Data.Models
open K4os.Compression.LZ4.Internal

/// <summary>
/// Serializable queries allow for clients to define queries via dsl that
/// can be easily serialized and converted to rendered to sql.
/// Initially this will be a simplified version of sql.
/// The aim is for use within code and to allow development of front end query builders.
/// </summary>
[<RequireQualifiedAccess>]
module SerializableQueries =

    type Query =
        { Select: Select list
          From: Table
          Joins: Join list
          Where: Condition option }

        static member Deserialize(str: string) =
            try
                JsonDocument.Parse(str).RootElement |> Query.FromJson
            with exn ->
                Error $"Unhandled exception while deserialized query: {exn.Message}"

        static member FromJson(json: JsonElement) =
            match
                Json.tryGetArrayProperty "select" json,
                Json.tryGetProperty "from" json
                |> Option.map Table.FromJson
                |> Option.defaultValue (Error "Missing from property")
            with
            | Some select, Ok from ->
                match select |> List.map Select.FromJson |> FPype.Core.Common.flattenResultList with
                | Ok selects ->
                    { Select = selects
                      From = from
                      Joins =
                        // NOTE check this behaviour is correct. Failed joins will just be ignored.
                        match
                            Json.tryGetArrayProperty "joins" json
                            |> Option.map (List.map Join.FromJson >> FPype.Core.Common.chooseResults)
                        with
                        | Some joins -> joins
                        | None -> []
                      Where =
                        match Json.tryGetProperty "where" json |> Option.map Condition.FromJson with
                        | Some(Ok condition) -> Some condition
                        | Some(Error e) -> None
                        | None -> None }
                    |> Ok
                | Error e -> Error $"Error deserializing from. {e}"
            | None, _ -> Error "Missing select property"
            | _, Error e -> Error $"Error deserializing from. {e}"

        member q.ToSql(?separator: string) =
            let sep = separator |> Option.defaultValue " "

            [ q.Select
              |> List.map (fun s -> s.ToSql())
              |> String.concat ", "
              |> fun s -> $"SELECT {s}"
              $"FROM {q.From.ToSql()}"
              yield! q.Joins |> List.map (fun j -> j.ToSql())
              match q.Where with
              | Some c -> $"WHERE {c.ToSql()}"
              | None -> () ]
            |> String.concat sep
        
        member q.Serialize(?options: JsonWriterOptions, ?indented: bool) =
            use ms = new MemoryStream()

            let opts =
                options
                |> Option.defaultWith (fun _ ->
                    let mutable opts = JsonWriterOptions()
                    opts.Indented <- indented |> Option.defaultValue true
                    opts)

            use writer = new Utf8JsonWriter(ms, opts)

            q.WriteToJson writer

            writer.Flush()
            
            ms.ToArray() |> Encoding.UTF8.GetString

        member q.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                w.WritePropertyName "select"
                Json.writeArray (fun w -> q.Select |> List.iter (fun s -> s.WriteToJson w)) "select" w
                w.WritePropertyName "from"
                q.From.WriteToJson w

                match q.Joins.IsEmpty with
                | true -> ()
                | false -> Json.writeArray (fun w -> q.Joins |> List.iter (fun j -> j.WriteToJson w)) "joins" w

                match q.Where with
                | Some c ->
                    w.WritePropertyName("where")
                    c.WriteToJson w
                | None -> ())


    and [<RequireQualifiedAccess>] Select =
        | Field of TableField
        | Case

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "type" json with
            | Some selectType ->
                match selectType with
                | "select" ->
                    Json.tryGetProperty "field" json
                    |> Option.map TableField.FromJson
                    |> Option.defaultValue (Error "Missing field property")
                    |> Result.map Select.Field
                | "case" ->
                    // TODO implement `Case`.
                    Error "Case select types not yet implemented"
                | t -> Error $"Unknown select type: `{t}`"
            | None -> Error $"Missing type property"

        member s.ToSql() =
            match s with
            | Field tf -> $"`{tf.TableName}`.`{tf.Field}`"
            | Case ->
                // TODO implement `Case`.
                failwith "Need to implement"

        member s.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                match s with
                | Field field ->
                    w.WriteString("type", "select")
                    w.WritePropertyName("field")
                    field.WriteToJson(w)
                | Case ->
                    // TODO implement `Case`.
                    failwith "Need to implement")



    and Join =
        { Type: JoinType
          Table: Table
          Condition: Condition }

        static member FromJson(json: JsonElement) =
            match
                Json.tryGetStringProperty "type" json,
                Json.tryGetProperty "table" json
                |> Option.map Table.FromJson
                |> Option.defaultValue (Error "Missing table property"),
                Json.tryGetProperty "condition" json
                |> Option.map Condition.FromJson
                |> Option.defaultValue (Error "Missing condition property")
            with
            | Some joinType, Ok table, Ok condition ->
                match joinType with
                | "inner"
                | "join"
                | "inner_join" -> Ok JoinType.Inner
                | "outer"
                | "outer_join" -> Ok JoinType.Outer
                | "cross"
                | "cross_join" -> Ok JoinType.Cross
                | jt -> Error $"Unknown join type: `{jt}`"
                |> Result.map (fun jt ->
                    { Type = jt
                      Table = table
                      Condition = condition })
            | None, _, _ -> Error "Missing type property"
            | _, Error e, _ -> Error $"Error deserializing table. {e}"
            | _, _, Error e -> Error $"Error deserializing condition. {e}"

        member j.ToSql() =
            $"{j.Type.ToSql()} {j.Table.ToSql()} ON {j.Condition.ToSql()}"

        member j.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                match j.Type with
                | Inner -> w.WriteString("type", "inner")
                | Outer -> w.WriteString("type", "outer")
                | Cross -> w.WriteString("type", "cross")

                w.WritePropertyName("table")
                j.Table.WriteToJson(w)

                w.WritePropertyName("condition")
                j.Condition.WriteToJson(w))

    and JoinType =
        | Inner
        | Outer
        | Cross

        member jt.ToSql() =
            match jt with
            | Inner -> "JOIN"
            | Outer -> "OUTER JOIN"
            | Cross -> "CROSS JOIN"

    and Table =

        { Name: string
          Alias: string option }

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "name" json with
            | Some name ->
                { Name = name
                  Alias = Json.tryGetStringProperty "alias" json }
                |> Ok
            | None -> Error "Missing name property"

        member t.ToSql() : string =
            match t.Alias with
            | Some alias -> $"`{t.Name}` `{alias}`"
            | None -> $"`{t.Name}`"

        member t.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                w.WriteString("name", t.Name)
                t.Alias |> Option.iter (fun a -> w.WriteString("alias", a)))

    and TableField =
        { TableName: string
          Field: string }

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "tableName" json, Json.tryGetStringProperty "field" json with
            | Some tableName, Some field -> Ok { TableName = tableName; Field = field }
            | None, _ -> Error "Missing tableName property"
            | _, None -> Error "Missing field property"

        member tf.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                w.WriteString("tableName", tf.TableName)
                w.WriteString("field", tf.Field))

    and [<RequireQualifiedAccess>] Condition =
        | Equals of Value * Value
        | NotEquals of Value * Value
        | GreaterThan of Value * Value
        | GreaterThanOrEquals of Value * Value
        | LessThan of Value * Value
        | LessThanOrEquals of Value * Value
        | IsNull of Value
        | IsNotNull of Value
        | Like of Value * Value
        | And of Condition * Condition
        | Or of Condition * Condition
        | Not of Condition

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "type" json with
            | Some "equals" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> Equals(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "not_equals" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> NotEquals(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "greater_than" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> GreaterThan(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "greater_than_or_equals" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> GreaterThanOrEquals(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "less_than" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> LessThan(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "less_than_or_equals" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> LessThanOrEquals(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "is_null" ->
                match
                    Json.tryGetProperty "value" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value property")
                with
                | Ok value -> IsNull value |> Ok
                | Error e -> Error $"Error deserializing value. {e}"
            | Some "is_not_null" ->
                match
                    Json.tryGetProperty "value" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value property")
                with
                | Ok value -> IsNotNull value |> Ok
                | Error e -> Error $"Error deserializing value. {e}"
            | Some "like" ->
                match
                    Json.tryGetProperty "value1" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value1 property"),
                    Json.tryGetProperty "value2" json
                    |> Option.map Value.FromJson
                    |> Option.defaultValue (Error "Missing value2 property")
                with
                | Ok v1, Ok v2 -> Like(v1, v2) |> Ok
                | Error e, _ -> Error $"Error deserializing value 1. {e}"
                | _, Error e -> Error $"Error deserializing value 2. {e}"
            | Some "and" ->
                match
                    Json.tryGetProperty "condition1" json
                    |> Option.map Condition.FromJson
                    |> Option.defaultValue (Error "Missing condition1 property"),
                    Json.tryGetProperty "condition2" json
                    |> Option.map Condition.FromJson
                    |> Option.defaultValue (Error "Missing condition2 property")
                with
                | Ok c1, Ok c2 -> And(c1, c2) |> Ok
                | Error e, _ -> Error $"Error deserializing condition 1. {e}"
                | _, Error e -> Error $"Error deserializing condition 2. {e}"
            | Some "or" ->
                match
                    Json.tryGetProperty "condition1" json
                    |> Option.map Condition.FromJson
                    |> Option.defaultValue (Error "Missing condition1 property"),
                    Json.tryGetProperty "condition2" json
                    |> Option.map Condition.FromJson
                    |> Option.defaultValue (Error "Missing condition2 property")
                with
                | Ok c1, Ok c2 -> Or(c1, c2) |> Ok
                | Error e, _ -> Error $"Error deserializing condition 1. {e}"
                | _, Error e -> Error $"Error deserializing condition 2. {e}"
            | Some "no" ->
                match
                    Json.tryGetProperty "condition" json
                    |> Option.map Condition.FromJson
                    |> Option.defaultValue (Error "Missing condition property")
                with
                | Ok c -> Not c |> Ok
                | Error e -> Error $"Error deserializing condition. {e}"
            | Some t -> Error $"Unknown condition type: `{t}`"
            | None -> Error "Missing type property"

        member c.ToSql() =
            match c with
            | Equals(v1, v2) -> $"{v1.ToSql()} = {v2.ToSql()}"
            | NotEquals(v1, v2) -> $"{v1.ToSql()} <> {v2.ToSql()}"
            | GreaterThan(v1, v2) -> $"{v1.ToSql()} > {v2.ToSql()}"
            | GreaterThanOrEquals(v1, v2) -> $"{v1.ToSql()} >= {v2.ToSql()}"
            | LessThan(v1, v2) -> $"{v1.ToSql()} < {v2.ToSql()}"
            | LessThanOrEquals(v1, v2) -> $"{v1.ToSql()} <= {v2.ToSql()}"
            | IsNull v -> $"{v.ToSql()} IS NULL"
            | IsNotNull v -> $"{v.ToSql()} IS NOT NULL"
            | Like(v1, v2) -> $"{v1.ToSql()} LIKE {v2.ToSql()}"
            | And(c1, c2) -> $"({c1.ToSql()} AND {c2.ToSql()})"
            | Or(c1, c2) -> $"({c1.ToSql()} OR {c2.ToSql()})"
            | Not c -> $"NOT {c.ToSql()}"

        member c.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                match c with
                | Equals(value1, value2) ->
                    w.WriteString("type", "equals")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | NotEquals(value1, value2) ->
                    w.WriteString("type", "not_equals")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | GreaterThan(value1, value2) ->
                    w.WriteString("type", "greater_than")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | GreaterThanOrEquals(value1, value2) ->
                    w.WriteString("type", "greater_than_or_equals")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | LessThan(value1, value2) ->
                    w.WriteString("type", "less_than")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | LessThanOrEquals(value1, value2) ->
                    w.WriteString("type", "less_than_or_equals")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | IsNull value ->
                    w.WriteString("type", "is_null")
                    w.WritePropertyName("value")
                    value.WriteToJson(w)
                | IsNotNull value ->
                    w.WriteString("type", "is_not_null")
                    w.WritePropertyName("value")
                    value.WriteToJson(w)
                | Like(value1, value2) ->
                    w.WriteString("type", "like")
                    w.WritePropertyName("value1")
                    value1.WriteToJson(w)
                    w.WritePropertyName("value2")
                    value2.WriteToJson(w)
                | And(condition1, condition2) ->
                    w.WriteString("type", "and")
                    w.WritePropertyName("condition1")
                    condition1.WriteToJson(w)
                    w.WritePropertyName("condition2")
                    condition2.WriteToJson(w)
                | Or(condition1, condition2) ->
                    w.WriteString("type", "or")
                    w.WritePropertyName("condition1")
                    condition1.WriteToJson(w)
                    w.WritePropertyName("condition2")
                    condition2.WriteToJson(w)
                | Not condition ->
                    w.WriteString("type", "not")
                    w.WritePropertyName("condition")
                    condition.WriteToJson(w))

    and [<RequireQualifiedAccess>] Value =
        | Literal of string
        | Number of decimal
        | Field of TableField
        | Parameter of Name: string

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "type" json with
            | Some "literal" ->
                match Json.tryGetStringProperty "value" json with
                | Some value -> Literal value |> Ok
                | None -> Error "Missing value property"
            | Some "number" ->
                match Json.tryGetDecimalProperty "value" json with
                | Some value -> Number value |> Ok
                | None -> Error "Missing value property"
            | Some "field" ->
                Json.tryGetProperty "field" json
                |> Option.map TableField.FromJson
                |> Option.defaultValue (Error "Missing field property")
                |> Result.map Field
            | Some "parameter" ->
                match Json.tryGetStringProperty "name" json with
                | Some name -> Parameter name |> Ok
                | None -> Error "Missing name property"
            | Some t -> Error $"Unknown value type: `{t}`"
            | None -> Error "Missing type property"

        member v.ToSql() =
            match v with
            | Literal s -> $"'{s}'"
            | Number decimal -> string decimal
            | Field field -> $"`{field.TableName}`.`{field.Field}`"
            | Parameter name -> $"@{name}"

        member v.WriteToJson(writer: Utf8JsonWriter) =
            writer
            |> Json.writeObject (fun w ->
                match v with
                | Literal value ->
                    w.WriteString("type", "literal")
                    w.WriteString("value", value)
                | Number value ->
                    w.WriteString("type", "value")
                    w.WriteNumber("value", value)
                | Field field ->
                    w.WriteString("type", "field")
                    w.WritePropertyName("field")
                    field.WriteToJson(w)
                | Parameter name ->
                    w.WriteString("type", "parameter")
                    w.WriteString("name", name))

module Dsl =

    let query
        (select: SerializableQueries.Select list)
        (from: SerializableQueries.Table)
        (joins: SerializableQueries.Join list)
        (where: SerializableQueries.Condition option)
        =
        ({ Select = select
           From = from
           Joins = joins
           Where = where }
        : SerializableQueries.Query)

    let selectFields (fields: SerializableQueries.TableField list) =
        fields |> List.map SerializableQueries.Select.Field

    let field tableName fieldName =
        ({ TableName = tableName
           Field = fieldName }
        : SerializableQueries.TableField)

    let (%==) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.Equals(v1, v2)

    let (%>) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.GreaterThan(v1, v2)

    let (%>=) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.GreaterThanOrEquals(v1, v2)

    let (%<) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.LessThan(v1, v2)

    let (%<=) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.LessThanOrEquals(v1, v2)

    let (%?) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.Like(v1, v2)

    let (%!) (c: SerializableQueries.Condition) = SerializableQueries.Condition.Not c

    let (%&&) (c1: SerializableQueries.Condition) (c2: SerializableQueries.Condition) =
        SerializableQueries.Condition.And(c1, c2)

    let (%||) (c1: SerializableQueries.Condition) (c2: SerializableQueries.Condition) =
        SerializableQueries.Condition.Or(c1, c2)

    let isNull (v: SerializableQueries.Value) = SerializableQueries.Condition.IsNull v

    let isNotNull (v: SerializableQueries.Value) =
        SerializableQueries.Condition.IsNotNull v

    let literal (str: string) = SerializableQueries.Value.Literal str

    let where (condition: SerializableQueries.Condition) = Some condition

    let toSql (query: SerializableQueries.Query) = query.ToSql()

    let test _ =

        query
        <| selectFields [ field "t" "foo"; field "t" "bar" ]
        <| { Name = "test"; Alias = Some "t" }
        <| []
        <| where ((literal "a" %> literal "b") %&& (literal "a" %> literal "b"))
