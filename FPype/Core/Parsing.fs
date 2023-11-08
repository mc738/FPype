namespace FPype.Core

module Parsing =

    /// <summary>
    /// Mark definitions.
    /// </summary>
    type MarkDefinition =
        | OneCharSymbol of char
        | TwoCharSymbol of char * char
        | Nested of char * char

        /// <summary>
        /// See if a ParsableInput matches a MarkDefinition from a collection.
        /// </summary>
        /// <param name="definitions"></param>
        /// <param name="input"></param>
        static member Match(definitions: MarkDefinition list, input: ParsableInput) =
            let handle (result: Mark option) (handler: ParsableInput -> Mark option) =
                match result with
                | Some _ -> result
                | None -> handler input

            definitions
            |> List.fold
                (fun r d ->
                    handle r (fun pi ->
                        match d with
                        | OneCharSymbol c ->
                            match pi.CurrentChar = c with
                            | true ->
                                { Definition = d
                                  StartIndex = pi.Position
                                  EndIndex = pi.Position + 1 }
                                |> Some
                            | false -> None
                        | TwoCharSymbol (c1, c2) ->
                            match pi.CurrentChar = c1, pi.LookAhead 1 with
                            | true, Some lc when lc = c2 ->
                                { Definition = d
                                  StartIndex = pi.Position
                                  EndIndex = pi.Position + 1 }
                                |> Some
                            | _ -> None
                        | Nested (c1, c2) ->
                            match pi.CurrentChar = c1 with
                            | true ->
                                match pi.FindNextNonNested(c1, c2) with
                                | Some ei ->
                                    { Definition = d
                                      StartIndex = pi.Position
                                      EndIndex = ei }
                                    |> Some
                                | None -> None
                            | false -> None))
                None

    /// <summary>
    /// A mark representing a place in a ParsableInput.
    /// </summary>
    and Mark =
        { Definition: MarkDefinition
          StartIndex: int
          EndIndex: int }

    and ParsableInput =
        { Input: string
          Position: int }

        static member Create(value) = { Input = value; Position = 0 }

        member pi.CurrentChar =
            match pi.InBounds(pi.Position) with
            | true -> pi.Input.[pi.Position]
            | false -> '\u0000'

        member pi.LookAhead(x: int) =
            match pi.Input.Length > pi.Position + x with
            | true -> Some pi.Input.[pi.Position + x]
            | false -> None

        member ps.LookBehind(x: int) =
            match ps.Position - x >= 0 with
            | true -> Some ps.Input.[ps.Position - x]
            | false -> None

        member pi.FindNext(c: char) =
            let rec handler (i, delimited) =
                match pi.LookAhead(i), delimited with
                | Some c2, None ->
                    match c = c2, c = '"', c = ''' with
                    | true, _, _ -> Some(i + pi.Position)
                    | false, true, _ -> handler (i + 1, Some '"')
                    | _, _, true -> handler (i + 1, Some ''')
                    | false, false, false -> handler (i + 1, None)
                | Some c, Some dc ->
                    match c = dc with
                    | true -> handler (i + 1, None)
                    | false -> handler (i + 1, Some dc)
                | None, _ -> None

            handler (1, None)

        member pi.FindNextIn(chars: char list) =
            let rec handler (i, delimiter) =
                match pi.LookAhead(i), delimiter with
                | Some c, None ->
                    match List.exists (fun lc -> lc = c) chars, c = '"', c = ''' with
                    | true, _, _ -> Some(i + pi.Position)
                    | false, true, _ -> handler (i + 1, Some '"')
                    | _, _, true -> handler (i + 1, Some ''')
                    | false, false, false -> handler (i + 1, None)
                | Some c, Some dc ->
                    match c = dc with
                    | true -> handler (i + 1, None)
                    | false -> handler (i + 1, Some dc)
                | None, _ -> None

            handler (1, None)

        member pi.FindNextNotIn(chars: char list) =
            let rec handler (i, delimiter) =
                match pi.LookAhead(i), delimiter with
                | Some c, None ->
                    match List.exists (fun lc -> lc = c) chars |> not, c = '"', c = ''' with
                    | true, _, _ -> Some(i + pi.Position)
                    | false, true, _ -> handler (i + 1, Some '"')
                    | _, _, true -> handler (i + 1, Some ''')
                    | false, false, false -> handler (i + 1, None)
                | Some c, Some dc ->
                    match c = dc with
                    | true -> handler (i + 1, None)
                    | false -> handler (i + 1, Some dc)
                | None, _ -> None

            handler (1, None)

        member pi.FindNext2(c1: char, c2: char) =
            let rec handler (i, delimiter) =
                // i1 so the handler is always looking one ahead than the value return
                match pi.LookAhead(i), delimiter with
                | Some c, None ->
                    match c = c1 && pi.LookAhead(i + 1) = Some c2, c = '"', c = ''' with
                    | true, _, _ -> Some(i + pi.Position)
                    | false, true, _ -> handler (i + 1, Some '"')
                    | _, _, true -> handler (i + 1, Some ''')
                    | false, false, false -> handler (i + 1, None)
                | Some c, Some dc ->
                    match c = dc with
                    | true -> handler (i + 1, None)
                    | false -> handler (i + 1, Some dc)
                | None, _ -> None

            handler (1, None)

        member pi.FindNextNonNested(openNesting: char, closeNesting: char) =
            let rec handler (i, delimited, nesting) =
                match pi.LookAhead(i), delimited with
                | Some c, None ->
                    match c = closeNesting, c = '"', c = ''', c = openNesting with
                    | true, _, _, _ ->
                        match nesting <= 1 with
                        | true -> Some(i + pi.Position)
                        | false -> handler (i + 1, delimited, nesting - 1)
                    | false, true, _, _ -> handler (i + 1, Some '"', nesting)
                    | _, _, true, _ -> handler (i + 1, Some ''', nesting)
                    | _, _, _, true -> handler (i + 1, delimited, nesting + 1)
                    | false, false, false, false -> handler (i + 1, delimited, nesting)
                | Some c, Some dc ->
                    match c = dc with
                    | true -> handler (i + 1, None, nesting)
                    | false -> handler (i + 1, Some dc, nesting)
                | None, _ -> None

            handler (0, None, 0)

        member ps.InBounds(i) = i >= 0 && i < ps.Input.Length

        member pi.GetSlice(startIndex, endIndex) =
            match pi.InBounds startIndex, pi.InBounds endIndex, startIndex < endIndex with
            | true, true, true -> Some pi.Input.[startIndex..endIndex]
            | _ -> None

        member pi.GetSliceFromEnd(index) =
            match pi.InBounds index with
            | true -> Some pi.Input.[index..]
            | false -> None

        member pi.GetSliceFromPosition(endIndex) =
            pi.GetSlice(pi.Position, pi.Position + endIndex)

        member pi.GetSliceFromOffset(offset, endIndex) =
            pi.GetSlice(pi.Position + offset, pi.Position + endIndex)

        member pi.GetSliceWithNesting(openNesting, closeNesting, includeNesting) =
            match pi.FindNextNonNested(openNesting, closeNesting) with
            | Some ei ->
                match includeNesting with
                | true -> pi.GetSliceFromPosition ei
                | false -> pi.GetSliceFromOffset(1, ei - 1)
            | None -> None

        member pi.Mark(definitions: MarkDefinition list) =
            let rec handler (pi: ParsableInput, marks) =
                match pi.InBounds(pi.Position), MarkDefinition.Match(definitions, pi) with
                | true, Some m -> handler (pi.SetPosition(m.EndIndex + 1), marks @ [ m ])
                | true, None -> handler (pi.SetPosition(pi.Position + 1), marks)
                | false, _ -> marks

            handler (pi, [])



        // Find the end index of a number from the current position.
        member pi.FindNumber() =
            let chars = [ '0'; '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; '.'; '-' ]

            pi.FindNextNotIn chars

        member pi.SetPosition(i) = { pi with Position = i }

        member pi.Progress(i) = { pi with Position = pi.Position + i }

        member pi.Next() = pi.Progress 1

        member pi.Parse<'T>(handler: ParsableInput -> 'T) = handler pi
