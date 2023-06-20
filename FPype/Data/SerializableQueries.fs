namespace FPype.Data

/// <summary>
/// Serializable queries allow for clients to define queries via dsl that 
/// </summary>
[<RequireQualifiedAccess>]
module SerializableQueries =

    type Query =
        {
            Select: SelectPart
            From: FromPart
            Joins: JoinPart list
            WherePart
        }
    
    
    and [<RequireQualifiedAccess>] SelectPart = Field of TableName: string * FieldName: string

    
    and FromPart =
        {
            Table: Table
        }
        
    and JoinPart =
        {
            Name: string
        }
        
    and WherePart =
        {
            Name: string
        }

    and Table = { Name: string; Alias: string }

    and [<RequireQualifiedAccess>] Condition =
        | Equals
        | GreaterThan
        | GreatThanOrEquals
        | LessThan
        | LessThanOrEquals
        | IsNull
        | IsNotNull
        | Like

    ()
