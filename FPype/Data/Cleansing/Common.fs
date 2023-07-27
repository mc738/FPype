namespace FPype.Data.Cleansing

[<AutoOpen>]
module Common =
    
    [<RequireQualifiedAccess>]
    type CleansingResult =
        | Untouched of string
        | Modified of string
        | Failure of Message: string
    
    type [<RequireQualifiedAccess>] CleansingAction =
        | Transform of Step: TransformationStep
        | Validate of Step: ValidationStep
        
    
    and [<RequireQualifiedAccess>] TransformationStep =
        | RemoveCharacters of Characters : char list
        | StringReplace of Value : string * Replacement : string
        | RegexReplace of Pattern : string * Replacement : string
        | Bespoke of Handler: (string -> string)
    
    and [<RequireQualifiedAccess>] ValidationStep =
        | ContainsCharacters of Characters: char list
        | Contains
        | RegexMatch of Pattern : string
        | Not of Step: ValidationStep
        | AnyOf of Steps: ValidationStep option
        | AllOf of Steps: ValidationStep option
        | Bespoke of Handler: (string -> bool)
    