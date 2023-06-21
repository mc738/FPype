namespace FPype.Data

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

        member q.ToSql(?separator: string) =
            let sep = separator |> Option.defaultValue ""

            [ q.From.Serialize()
              yield! q.Joins |> List.map (fun j -> j.Serialize())
              match q.Where with
              | Some c -> $"WHERE {c.Serialize()}"
              | None -> () ]
            |> String.concat sep

    and [<RequireQualifiedAccess>] Select =
        | Field of TableName: string * FieldName: string
        | Case

    and Join =
        { Type: JoinType
          Table: Table
          Condition: Condition }

        member j.Serialize() =
            $"{j.Type.Serialize()} {j.Table.Serialize()} ON {j.Condition.Serialize()}"

    and JoinType =
        | Inner
        | Outer
        | Cross

        member jt.Serialize() =
            match jt with
            | Inner -> "JOIN"
            | Outer -> "OUTER JOIN"
            | Cross -> "CROSS JOIN"

    and Table =

        { Name: string
          Alias: string option }

        member t.Serialize() : string =
            match t.Alias with
            | Some alias -> $"`{t.Name}` `{alias}`"
            | None -> $"`{t.Name}`"

    and [<RequireQualifiedAccess>] Condition =
        | Equals of Value * Value
        | NotEquals of Value * Value
        | GreaterThan of Value * Value
        | GreatThanOrEquals of Value * Value
        | LessThan of Value * Value
        | LessThanOrEquals of Value * Value
        | IsNull of Value
        | IsNotNull of Value
        | Like of Value * Value
        | And of Condition * Condition
        | Or of Condition * Condition
        | Not of Condition

        member c.Serialize() =
            match c with
            | Equals(v1, v2) -> $"{v1.Serialize()} = {v2.Serialize()}"
            | NotEquals(v1, v2) -> $"{v1.Serialize()} <> {v2.Serialize()}"
            | GreaterThan(v1, v2) -> $"{v1.Serialize()} > {v2.Serialize()}"
            | GreatThanOrEquals(v1, v2) -> $"{v1.Serialize()} >= {v2.Serialize()}"
            | LessThan(v1, v2) -> $"{v1.Serialize()} < {v2.Serialize()}"
            | LessThanOrEquals(v1, v2) -> $"{v1.Serialize()} <= {v2.Serialize()}"
            | IsNull v -> $"{v.Serialize()} IS NULL"
            | IsNotNull v -> $"{v.Serialize()} IS NOT NULL"
            | Like(v1, v2) -> $"{v1.Serialize()} LIKE {v2.Serialize()}"
            | And(c1, c2) -> $"({c1.Serialize()} AND {c2.Serialize()})"
            | Or(c1, c2) -> $"({c1.Serialize()} OR {c2.Serialize()})"
            | Not c -> $"NOT {c.Serialize()}"

    and [<RequireQualifiedAccess>] Value =
        | Literal of string
        | Number of decimal
        | Field of TableName: string * FieldName: string
        | Parameter of Name: string

        member v.Serialize() =
            match v with
            | Literal s -> $"'{s}'"
            | Number decimal -> string decimal
            | Field(tableName, fieldName) -> $"`{tableName}`.`{fieldName}`"
            | Parameter name -> $"@{name}"


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

    let select (fields: SerializableQueries.Select list) = ()

    let field tableName fieldName =
        SerializableQueries.Select.Field(tableName, fieldName)

    let (%==) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.Equals(v1, v2)

    let (%>) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.GreaterThan(v1, v2)

    let (%>=) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
            SerializableQueries.Condition.GreatThanOrEquals(v1, v2)
            
    let (%<) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
        SerializableQueries.Condition.LessThan(v1, v2)

    let (%<=) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
            SerializableQueries.Condition.LessThanOrEquals(v1, v2)

    let (%?) (v1: SerializableQueries.Value) (v2: SerializableQueries.Value) =
            SerializableQueries.Condition.Like(v1, v2)
    
    let (%!) (c: SerializableQueries.Condition) =
            SerializableQueries.Condition.Not c
            
            
    
    let literal (str: string) = SerializableQueries.Value.Literal str
    
    
    let toSql (query: SerializableQueries.Query) = query.ToSql()

    let test _ =
        

        query
        <| [ field "t" "foo"; field "t" "bar" ]
        <| { Name = "test"; Alias = Some "t" }
        <| []
        <| Some(literal "a" %> literal "b")
