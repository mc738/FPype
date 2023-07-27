namespace FPype.Data.Cleansing

open System
open System.Text.RegularExpressions

[<AutoOpen>]
module Common =

    [<AutoOpen>]
    module private Internal =
        
        let compare (strA: string) (strB: string) = String.Equals(strA, strB, StringComparison.Ordinal)
    
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
                let newStr =
                    match ts with
                    | RemoveCharacters characters ->
                        match characters.Length with
                        | 0 -> str
                        | _ ->
                            // PERFORMANCE It might not matter and needs to be tested but this could be the quickest method.
                            System.String.Join(System.String.Empty, str.Split(characters |> Array.ofList))
                    | StringReplace(value, replacement) -> str.Replace(value, replacement)
                    | RegexReplace(pattern, replacement) -> Regex.Replace(str, pattern, replacement)
                    | Bespoke handler ->  handler str
               
                match compare str newStr with
                | true -> input
                | false -> CleansingResult.Modified str
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
