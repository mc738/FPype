namespace FPype.Core

[<AutoOpen>]
module Common =
    
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

