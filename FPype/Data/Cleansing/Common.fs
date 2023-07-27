namespace FPype.Data.Cleansing

[<AutoOpen>]
module Common =
    
    type CleansingAction =
        | Transform
        | Validate
        
    and TransformationStep =
        | RemoveCharacters of Characters : char list
        | StringReplace of Value : string * Replacement : string
        | Bespoke of Handler: (string -> string)
    
    
    [<RequireQualifiedAccess>]
    type CleansingResult =
        | Untouched of string
        | Modified of string
        | Failure of Message: string
    
    ()

