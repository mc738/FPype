namespace FPype.Data.Cleansing

[<AutoOpen>]
module Common =

    [<RequireQualifiedAccess>]
    type CleansingResult =
        | Untouched of string
        | Modified of string
        | Failure of Message: string

    [<RequireQualifiedAccess>]
    type CleansingStep =
        | Transform of Step: TransformationStep
        | Validate of Step: ValidationStep


    and [<RequireQualifiedAccess>] TransformationStep =
        | RemoveCharacters of Characters: char list
        | StringReplace of Value: string * Replacement: string
        | RegexReplace of Pattern: string * Replacement: string
        | Bespoke of Handler: (string -> string)

        member ts.Handle(input: CleansingResult) =
            match input with
            | CleansingResult.Untouched str
            | CleansingResult.Modified str ->
                match ts with
                | RemoveCharacters characters ->
                    match characters.Length with
                    | 0 -> input
                    
                    
                    CleansingResult.Modified ""
                | StringReplace(value, replacement) -> failwith "todo"
                | RegexReplace(pattern, replacement) -> failwith "todo"
                | Bespoke handler -> failwith "todo"
            | CleansingResult.Failure _ -> input

    and [<RequireQualifiedAccess>] ValidationStep =
        | ContainsCharacters of Characters: char list
        | Contains
        | RegexMatch of Pattern: string
        | Not of Step: ValidationStep
        | AnyOf of Steps: ValidationStep option
        | AllOf of Steps: ValidationStep option
        | Bespoke of Handler: (string -> bool)


    let cleanString (steps: CleansingStep) (str: string) =





        ()
