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
        {
            Select: SelectPart list
            From: FromPart
            Joins: JoinPart list
            WherePart: WherePart list
        }
    
    
    and [<RequireQualifiedAccess>] SelectPart =
        | Field of TableName: string * FieldName: string
        | Case
    
    and FromPart =
        {
            Table: Table
        }
        
    and JoinPart =
        {
            Table: Table
            Condition
        }
        
    and WherePart =
        {
            Name: string
        }

    and Table = { Name: string; Alias: string }

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
        
        member c.ToSql() =
            match c with
            | Equals (v1, v2) -> $"{v1.ToSql()} = {v2.ToSql()}"
            | NotEquals (v1, v2) -> $"{v1.ToSql()} <> {v2.ToSql()}"
            | GreaterThan(v1, v2) -> $"{v1.ToSql()} > {v2.ToSql()}"
            | GreatThanOrEquals(v1, v2) -> $"{v1.ToSql()} >= {v2.ToSql()}"
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
        | Field of TableName: string * FieldName: string
        
        member v.ToSql() =
            match v with
            | Literal s -> $"'{s}'"
            | Number decimal -> string decimal
            | Field(tableName, fieldName) -> $"{tableName}.{fieldName}"
        
        
    
    
    ()
