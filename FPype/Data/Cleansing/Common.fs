namespace FPype.Data.Cleansing

[<AutoOpen>]
module Common =

    open System
    open System.Text.RegularExpressions

    [<AutoOpen>]
    module private Internal =

        let compare (strA: string) (strB: string) =
            String.Equals(strA, strB, StringComparison.Ordinal)

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
        | ToUpper
        | ToLower
        | Trim
        | TrimStart
        | TrimEnd
        | Insert of Position: int * Value: string
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
                    | ToUpper -> str.ToUpper()
                    | ToLower -> str.ToLower()
                    | Trim -> str.Trim()
                    | TrimStart -> str.TrimStart()
                    | TrimEnd -> str.TrimEnd()
                    | Insert(position, value) -> str.Insert(position, value)
                    | Bespoke handler -> handler str

                match compare str newStr with
                | true -> input
                | false -> CleansingResult.Modified str
            | CleansingResult.Failure _ -> input

    and [<RequireQualifiedAccess>] ValidationStep =
        | ContainsCharacters of Characters: char list
        | Contains of Value: string
        | RegexMatch of Pattern: string
        | Not of Step: ValidationStep
        | AnyOf of Steps: ValidationStep option
        | AllOf of Steps: ValidationStep option
        | Bespoke of Handler: (string -> bool)

        member vs.Handle(input: CleansingResult) =
            match input with
            | CleansingResult.Untouched str
            | CleansingResult.Modified str ->
                let valid =
                    match vs with
                    | ContainsCharacters characters -> characters |> List.exists (fun c -> str.Contains c |> not)
                    | Contains value -> str.Contains(value)
                    | RegexMatch pattern -> failwith "todo"
                    | Not step -> failwith "todo"
                    | AnyOf steps -> failwith "todo"
                    | AllOf steps -> failwith "todo"
                    | Bespoke handler -> handler str

                match valid with
                | true -> input
                | false ->
                    CleansingResult.Failure ""
            | CleansingResult.Failure _ -> input


    let cleanString (steps: CleansingStep) (str: string) =





        ()
