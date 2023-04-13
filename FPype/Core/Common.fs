namespace FPype.Core

[<AutoOpen>]
module Common =

    open System

    let flattenResultList (r: Result<'a, string> list) =
        r
        |> List.fold
            (fun (s, err) r ->
                match r with
                | Ok v -> s @ [ v ], err
                | Error e -> s, err @ [ e ])
            ([], [])
        |> fun (values, errors) ->
            match errors.IsEmpty with
            | true -> Ok values
            | false -> Error <| String.concat ", " errors


    let chooseResults (r: Result<'a, 'b> list) =
        r
        |> List.fold
            (fun acc r ->
                match r with
                | Ok v -> v :: acc
                | Error _ -> acc)
            []
        |> List.rev


    [<AutoOpen>]
    module Extensions =

        type String with

            member str.ReplaceMultiple(replacements: (string * string) list) =
                replacements |> List.fold (fun (s:string) -> s.Replace) str