namespace FPype.Data

open System.Text.Json
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
                let json = JsonDocument.Parse(str).RootElement

                Ok
                    { Select = []
                      From = { Name = ""; Alias = None }
                      Joins = []
                      Where = None }

            with exn ->
                Error $"Unhandled exception while deserialized query: {exn.Message}"

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

        member q.Serialize() = ""


    and [<RequireQualifiedAccess>] Select =
        | Field of TableField
        | Case

        member s.ToSql() =
            match s with
            | Field tf -> $"`{tf.TableName}`.`{tf.Field}`"
            | Case ->
                // TODO implement `Case`.
                failwith "Need to implement"

    and Join =
        { Type: JoinType
          Table: Table
          Condition: Condition }

        member j.ToSql() =
            $"{j.Type.ToSql()} {j.Table.ToSql()} ON {j.Condition.ToSql()}"

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

    and TableField =
        { TableName: string
          Field: string }

        static member FromJson(json: JsonElement) =
            match Json.tryGetStringProperty "tableName" json, Json.tryGetStringProperty "field" json with
            | Some tableName, Some field -> Ok { TableName = tableName; Field = field }
            | None, _ -> Error "Missing tableName property"
            | _, None -> Error "Missing field property"

        member tf.WriteToJson(writer: Utf8JsonWriter) =
            writer.WriteStartObject()
            writer.WriteString("tableName", tf.TableName)
            writer.WriteString("field", tf.Field)
            writer.WriteEndObject()

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
            writer.WriteStartObject()

            match v with
            | Literal value ->
                writer.WriteString("type", "literal")
                writer.WriteString("value", value)
            | Number value ->
                writer.WriteString("type", "value")
                writer.WriteNumber("value", value)
            | Field field ->
                writer.WriteString("type", "field")
                writer.WritePropertyName("field")
                field.WriteToJson(writer)
            | Parameter name ->
                writer.WriteString("type", "parameter")
                writer.WriteString("name", name)

            writer.WriteEndObject()

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
