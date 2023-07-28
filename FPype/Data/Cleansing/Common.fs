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

        member cs.Handle(input: CleansingResult) =
            match cs with
            | Transform ts -> ts.Handle input
            | Validate vs -> vs.Handle input

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
        | ContainsLetters
        | ContainsNumbers
        | ContainsPunctuation
        | ContainsWhiteSpace
        | RegexMatch of Pattern: string
        | Not of Step: ValidationStep
        | AnyOf of Steps: ValidationStep list
        | AllOf of Steps: ValidationStep list
        | Bespoke of Handler: (string -> bool)

        member vs.Handle(input: CleansingResult) =
            match input with
            | CleansingResult.Untouched str
            | CleansingResult.Modified str ->
                let valid =
                    let rec handler (validation: ValidationStep) =

                        match validation with
                        | ContainsCharacters characters -> characters |> List.exists (fun c -> str.Contains c |> not)
                        | Contains value -> str.Contains(value)
                        | ContainsLetters -> str |> Seq.exists Char.IsLetter
                        | ContainsNumbers -> str |> Seq.exists Char.IsNumber
                        | ContainsPunctuation -> str |> Seq.exists Char.IsSeparator
                        | ContainsWhiteSpace -> str |> Seq.exists Char.IsWhiteSpace
                        | RegexMatch pattern -> Regex.IsMatch(str, pattern)
                        | Not step -> handler step |> not
                        | AnyOf steps ->
                            steps
                            |> List.fold
                                (fun r s ->
                                    match r with
                                    | true -> true
                                    | false -> handler s)
                                false
                        | AllOf steps ->
                            steps
                            |> List.fold
                                (fun r s ->
                                    match r with
                                    | true -> handler s
                                    | false -> false)
                                true
                        | Bespoke handler -> handler str

                    handler vs

                match valid with
                | true -> input
                | false -> CleansingResult.Failure ""
            | CleansingResult.Failure _ -> input

    let cleanString (steps: CleansingStep list) (str: string) =
        steps |> List.fold (fun r s -> s.Handle r) (CleansingResult.Untouched str)
