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
        | GreaterThan of Value * Value
        | GreatThanOrEquals of Value * Value
        | LessThan of Value * Value
        | LessThanOrEquals of Value * Value
        | IsNull of Value
        | IsNotNull of Value
        | Like of Value * Value
        | And of Condition * Condition
        | Or of Condition * Condition

    and [<RequireQualifiedAccess>] Value =
        | Literal of string
        | Field of TableName: string * FieldName: string
    
    
    ()
