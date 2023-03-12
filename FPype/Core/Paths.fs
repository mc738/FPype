namespace FPype.Core

open System
open FPype.Core.Expressions

module Paths =

    open System

    module Parsing =

        type ParserPhase =
            | Root // $
            | Selector // Either ., .. or [...] (check for all 3)
            | NextToken
            //| DotSelector // .name or ..
            //| BracketSelector // [name]
            | ArraySelector // [0] [0,2] [0:3]
            | Filter // [?(...)]

        type SelectorToken =
            | Child of string
            | ChildUnion of string
            | ChildWildCard
            | DeepScan of string
            | DeepScanUnion of string
            | DeepScanWildCard

        type ArraySelectorToken =
            | Index of int
            | Indexes of int list
            | Slice of int option * int option

        type SectionToken =
            { Selector: SelectorToken
              Filter: string option
              ArraySelector: string option }

        type ParserState =
            { Input: string
              Position: int
              Phase: ParserPhase
              CurrentSection: SectionToken option
              Sections: SectionToken list }

            static member Start(value) =
                { Input = value
                  Position = 0
                  Phase = ParserPhase.Root
                  CurrentSection = None
                  Sections = [] }

            member ps.ToSelectorPhase(progress) =
                { ps with
                    Position =
                        match progress with
                        | true -> ps.Position + 1
                        | false -> ps.Position
                    Phase = ParserPhase.Selector
                    CurrentSection = None
                    Sections =
                        match ps.CurrentSection with
                        | Some cs -> ps.Sections @ [ cs ]
                        | None -> ps.Sections }

            member ps.SelectorComplete(token, newPosition) =
                { ps with
                    Position = newPosition
                    Phase = ParserPhase.NextToken
                    CurrentSection =
                        Some(
                            { Selector = token
                              Filter = None
                              ArraySelector = None }
                        ) }

            member ps.ToFilterPhase(progress) =
                { ps with
                    Position =
                        match progress with
                        | true -> ps.Position + 2
                        | false -> ps.Position
                    Phase = ParserPhase.Filter }

            member ps.FilterComplete(token, newPosition) =
                { ps with
                    Position = newPosition
                    Phase = ParserPhase.NextToken
                    CurrentSection =
                        Some(
                            { Selector = ps.CurrentSection.Value.Selector
                              Filter = Some token
                              ArraySelector = ps.CurrentSection.Value.Filter }
                        ) }

            member ps.ToArraySelectorPhase(progress) =
                { ps with
                    Position =
                        match progress with
                        | true -> ps.Position + 1
                        | false -> ps.Position
                    Phase = ParserPhase.ArraySelector }

            member ps.ArraySelectorComplete(token, newPosition) =
                { ps with
                    Position = newPosition
                    Phase = ParserPhase.NextToken
                    CurrentSection =
                        Some(
                            { Selector = ps.CurrentSection.Value.Selector
                              Filter = ps.CurrentSection.Value.Filter
                              ArraySelector = Some token }
                        ) }

            member ps.CurrentChar = ps.Input.[ps.Position]

            member ps.LookAhead(x: int) =
                match ps.Input.Length > ps.Position + x with
                | true -> Some ps.Input.[ps.Position + x]
                | false -> None

            member ps.LookBehind(x: int) =
                match ps.Position - x >= 0 with
                | true -> Some ps.Input.[ps.Position - x]
                | false -> None

            member ps.FindNext(c: char) =
                let rec handler (i, delimited) =
                    match ps.LookAhead(i), delimited with
                    | Some c2, None ->
                        match c = c2, c = '"', c = ''' with
                        | true, _, _ -> Some(i + ps.Position)
                        | false, true, _ -> handler (i + 1, Some '"')
                        | _, _, true -> handler (i + 1, Some ''')
                        | false, false, false -> handler (i + 1, None)
                    | Some c, Some dc ->
                        match c = dc with
                        | true -> handler (i + 1, None)
                        | false -> handler (i + 1, Some dc)
                    | None, _ -> None

                handler (1, None)

            member ps.FindNextIn(chars: char list) =
                let rec handler (i, delimiter) =
                    match ps.LookAhead(i), delimiter with
                    | Some c, None ->
                        match List.exists (fun lc -> lc = c) chars, c = '"', c = ''' with
                        | true, _, _ -> Some(i + ps.Position)
                        | false, true, _ -> handler (i + 1, Some '"')
                        | _, _, true -> handler (i + 1, Some ''')
                        | false, false, false -> handler (i + 1, None)
                    | Some c, Some dc ->
                        match c = dc with
                        | true -> handler (i + 1, None)
                        | false -> handler (i + 1, Some dc)
                    | None, _ -> None

                handler (1, None)

            member ps.FindNextNotIn(chars: char list) =
                let rec handler (i, delimiter) =
                    match ps.LookAhead(i), delimiter with
                    | Some c, None ->
                        match List.exists (fun lc -> lc = c) chars |> not, c = '"', c = ''' with
                        | true, _, _ -> Some(i + ps.Position)
                        | false, true, _ -> handler (i + 1, Some '"')
                        | _, _, true -> handler (i + 1, Some ''')
                        | false, false, false -> handler (i + 1, None)
                    | Some c, Some dc ->
                        match c = dc with
                        | true -> handler (i + 1, None)
                        | false -> handler (i + 1, Some dc)
                    | None, _ -> None

                handler (1, None)

            member ps.FindNext2(c1: char, c2: char) =
                let rec handler (i, delimiter) =
                    // i1 so the handler is always looking one ahead than the value return
                    match ps.LookAhead(i), delimiter with
                    | Some c, None ->
                        match c = c1 && ps.LookAhead(i + 1) = Some c2, c = '"', c = ''' with
                        | true, _, _ -> Some(i + ps.Position)
                        | false, true, _ -> handler (i + 1, Some '"')
                        | _, _, true -> handler (i + 1, Some ''')
                        | false, false, false -> handler (i + 1, None)
                    | Some c, Some dc ->
                        match c = dc with
                        | true -> handler (i + 1, None)
                        | false -> handler (i + 1, Some dc)
                    | None, _ -> None

                handler (1, None)

            member ps.InBounds(i) = i >= 0 && i < ps.Input.Length

            member ps.GetSlice(startIndex, endIndex) =
                match ps.InBounds startIndex, ps.InBounds endIndex, startIndex < endIndex with
                | true, true, true -> Some ps.Input.[startIndex..endIndex]
                | _ -> None

            member ps.GetSliceFromEnd(index) =
                match ps.InBounds index with
                | true -> Some ps.Input.[index..]
                | false -> None

            member ps.GetCharSlice(index) =
                ps.Input |> Seq.tryItem index |> Option.map (fun c -> String(c, 1))

            // Find the end index of a number from the current position.
            member ps.FindNumber() =
                let chars = [ '0'; '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; '.'; '-' ]

                ps.FindNextNotIn chars

        type ParserResult =
            | Success of SectionToken list
            | MissingRoot
            | MissingSelectorToken of int
            | MissingChar of int * char list
            | MissingSelectorName of int
            | MissingToken of int
            | NotImplemented of string

        let parse (input: string) (rootChar: Char) =

            let rec handler (state: ParserState) =

                let newPos = state.Position + 1

                match newPos >= state.Input.Length with
                | true ->
                    match state.CurrentSection with
                    | Some cs -> state.Sections @ [ cs ]
                    | None -> state.Sections
                    |> ParserResult.Success
                | false ->
                    match state.Phase with
                    | Root ->
                        match state.CurrentChar with
                        | c when c = rootChar -> handler (state.ToSelectorPhase(true))
                        | _ -> ParserResult.MissingRoot
                    | Selector ->
                        match state.CurrentChar with
                        | '.' ->
                            // Check for deep scan (..)
                            let (offset, isDeepScan) =
                                match state.LookAhead 1 with
                                | Some '.' -> (2, true)
                                | _ -> (1, false)
                            // Check for wildcard.
                            match state.LookAhead(offset + 1) with
                            | Some '*' ->
                                // Wild card.
                                let token =
                                    match isDeepScan with
                                    | true -> SelectorToken.DeepScanWildCard
                                    | false -> SelectorToken.ChildWildCard

                                // Offset + 2 -> one for the lookahead for * and one to progress.
                                handler (state.SelectorComplete(token, offset + 1))
                            | Some _ ->
                                match state.FindNextIn([ '['; '.' ]) with
                                | Some foundIndex ->
                                    let name =
                                        // NOTE special handling required for single letter names. Instead of
                                        match state.Position + offset = foundIndex - 1 with
                                        | true -> state.GetCharSlice(state.Position + offset)
                                        | false -> state.GetSlice(state.Position + offset, foundIndex - 1)
                                        |> Option.defaultValue String.Empty

                                    handler (state.SelectorComplete(SelectorToken.Child name, foundIndex))
                                | None ->
                                    // No selector, array selector or filter. this is the end of the input
                                    let name =
                                        state.GetSliceFromEnd(state.Position + offset)
                                        |> Option.defaultValue String.Empty

                                    ParserResult.Success(
                                        state.Sections
                                        @ [ { Selector = SelectorToken.Child name
                                              Filter = None
                                              ArraySelector = None } ]
                                    )
                            | None ->
                                // NOTE - This is to handle the situation where the final selector is a single letter.
                                // It will have no filter etc.

                                ParserResult.Success(
                                    state.Sections
                                    @ [ { Selector =
                                            state.GetCharSlice(state.Position + offset)
                                            |> Option.defaultValue String.Empty
                                            |> SelectorToken.Child
                                          Filter = None
                                          ArraySelector = None } ]
                                )
                        //.
                        | _ -> ParserResult.MissingSelectorToken state.Position
                    | NextToken ->
                        match state.CurrentChar with
                        | '.' -> handler (state.ToSelectorPhase(false))
                        | '[' ->
                            match state.LookAhead 1 with
                            | Some '?' -> handler (state.ToFilterPhase(true))
                            | Some _ -> handler (state.ToArraySelectorPhase(true))
                            | None -> ParserResult.MissingChar(state.Position + 1, [ ']' ])
                        | _ -> ParserResult.MissingChar(state.Position, [ '.'; '[' ])
                    | ArraySelector ->
                        match state.FindNext(']') with
                        | Some endIndex ->
                            /// Important!
                            /// This is needed because since digit values (i.e. '1') where being missed.
                            /// Basically if the end index - 1 is the same as the current position
                            /// (i.e. the result is one character long), then just return the current character as a string.
                            let v =
                                match state.Position = endIndex - 1 with
                                | true -> $"{state.CurrentChar}"
                                | false -> state.GetSlice(state.Position, endIndex - 1) |> Option.defaultValue ""


                            handler (state.ArraySelectorComplete(v, endIndex + 1))
                        | None -> ParserResult.MissingChar(state.Position, [ ']' ])
                    | Filter ->
                        match state.FindNext2(')', ']') with
                        | Some endIndex ->
                            handler (
                                state.FilterComplete(
                                    state.GetSlice(state.Position + 1, endIndex - 1) |> Option.defaultValue "",
                                    endIndex + 2
                                )
                            )
                        | None -> ParserResult.MissingChar(state.Position, [ ')'; ']' ])

            let r = handler (ParserState.Start(input))

            r

    type Selector =
        | Child of string
        | ChildUnion of string list
        | ChildWildCard
        | DeepScan of string
        | DeepScanUnion of string list
        | DeepScanWildCard

        static member FromToken(token: Parsing.SelectorToken) =
            match token with
            | Parsing.SelectorToken.Child s -> Child s
            | Parsing.SelectorToken.ChildUnion s -> ChildUnion(s.Split('|') |> List.ofSeq) // BUG? if there is a '|' in delimited name
            | Parsing.SelectorToken.ChildWildCard -> ChildWildCard
            | Parsing.SelectorToken.DeepScan s -> DeepScan s
            | Parsing.SelectorToken.DeepScanUnion s -> DeepScanUnion(s.Split('|') |> List.ofSeq) // BUG? if there is a '|' in delimited name
            | Parsing.SelectorToken.DeepScanWildCard -> DeepScanWildCard

    and EvaluationResult =
        | True
        | False
        | WrongType
        | NonScalarValue
        | ValueNotFound
        | NotImplemented

    and FilterValue =
        | String of string
        | Numeric of decimal
        | CurrentNode of Path
        | RootNode of Path

        static member Parse(str: string) =

            match str.Trim() |> Seq.tryHead with
            | Some '$' -> Path.Compile(str) |> Result.map FilterValue.RootNode
            | Some '@' -> Path.Compile(str, '@') |> Result.map FilterValue.CurrentNode
            | Some '\''
            | Some '"' -> str.Substring(1, str.Length - 2) |> FilterValue.String |> Ok
            | Some c when Char.IsNumber(c) || c = '-' ->
                // Number
                match Decimal.TryParse str with
                | true, v -> FilterValue.Numeric v |> Ok
                | false, _ -> Error "NaN"
            | Some c -> Error $"Unknown character `{c}`"
            | None -> Error "Out of bounds"

        static member AreEqual(fv1: FilterValue, fv2: FilterValue) = ()

        member fv.GetNumericValue() =
            match fv with
            | String s ->
                match Decimal.TryParse s with
                | true, v -> Ok v // TODO warn of string value
                | false, _ -> Error EvaluationResult.WrongType
            | Numeric n -> Ok n
            | CurrentNode p -> Error EvaluationResult.NotImplemented
            | RootNode p -> Error EvaluationResult.NotImplemented

        member fv.GetStringValue() =
            match fv with
            | String s -> Ok s
            | Numeric n -> Ok(n.ToString()) //TODO warn of int value
            | CurrentNode p -> Error EvaluationResult.NotImplemented
            | RootNode p -> Error EvaluationResult.NotImplemented

    and [<RequireQualifiedAccess>] FilterOperator =
        | Exists of FilterValue
        | Equal of FilterValue * FilterValue
        | NotEqual of FilterValue * FilterValue
        | LessThan of FilterValue * FilterValue
        | LessThanOrEqual of FilterValue * FilterValue
        | GreaterThan of FilterValue * FilterValue
        | GreaterThanOrEqual of FilterValue * FilterValue
        | RegexMatch of FilterValue * FilterValue
        | In of FilterValue * FilterValue list
        | NotIn of FilterValue * FilterValue list
        | SubsetOf of FilterValue * FilterValue list
        | AnyOf of FilterValue * FilterValue list
        | NoneOf of FilterValue * FilterValue list
        | Size of FilterValue * FilterValue
        | Empty of FilterValue

        static member FromToken(token: ExpressionOperatorToken) =
            match token with
            | ExpressionOperatorToken.Exists v -> FilterValue.Parse v |> Result.map FilterOperator.Exists
            | ExpressionOperatorToken.Equal (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.Equal(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.NotEqual (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.NotEqual(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.LessThan (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.LessThan(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.LessThanOrEqual (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.LessThanOrEqual(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.GreaterThan (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.GreaterThan(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.GreaterThanOrEqual (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.GreaterThanOrEqual(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.RegexMatch (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.RegexMatch(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.In (v1, vs) ->
                match FilterValue.Parse v1, vs |> List.map FilterValue.Parse |> flattenResultList with
                | Ok pv1, Ok pvs -> FilterOperator.In(pv1, pvs) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.NotIn (v1, vs) ->
                match FilterValue.Parse v1, vs |> List.map FilterValue.Parse |> flattenResultList with
                | Ok pv1, Ok pvs -> FilterOperator.NotIn(pv1, pvs) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.SubsetOf (v1, vs) ->
                match FilterValue.Parse v1, vs |> List.map FilterValue.Parse |> flattenResultList with
                | Ok pv1, Ok pvs -> FilterOperator.SubsetOf(pv1, pvs) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.AnyOf (v1, vs) ->
                match FilterValue.Parse v1, vs |> List.map FilterValue.Parse |> flattenResultList with
                | Ok pv1, Ok pvs -> FilterOperator.AnyOf(pv1, pvs) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.NoneOf (v1, vs) ->
                match FilterValue.Parse v1, vs |> List.map FilterValue.Parse |> flattenResultList with
                | Ok pv1, Ok pvs -> FilterOperator.NoneOf(pv1, pvs) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.Size (v1, v2) ->
                match FilterValue.Parse v1, FilterValue.Parse v2 with
                | Ok pv1, Ok pv2 -> FilterOperator.Size(pv1, pv2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionOperatorToken.Empty v -> FilterValue.Parse v |> Result.map FilterOperator.Exists


    and [<RequireQualifiedAccess>] FilterExpression =
        | Operator of FilterOperator
        | And of FilterExpression * FilterExpression
        | Or of FilterExpression * FilterExpression
        | All of FilterExpression list
        | Any of FilterExpression list

        static member FromToken(statement: ExpressionStatement) =
            // How this can be split?

            // 1. Brackets (top level)
            // 2. And/Or/All/Any
            // 3. Individual bits

            // Precedence
            // L to R grouping
            // expr1 && expr2 || expr3
            //
            // will be
            //
            // (expr1 && expr2) || expr3
            //
            // expr1 && expr2 && expr3 || expr4
            //
            // will be
            //
            // (expr1 && expr2 && expr3) || expr4


            match statement with
            | ExpressionStatement.Operator op -> FilterOperator.FromToken op |> Result.map Operator
            | ExpressionStatement.And (s1, s2) ->
                match FilterExpression.FromToken s1, FilterExpression.FromToken s2 with
                | Ok exp1, Ok exp2 -> FilterExpression.And(exp1, exp2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionStatement.Or (s1, s2) ->
                match FilterExpression.FromToken s1, FilterExpression.FromToken s2 with
                | Ok exp1, Ok exp2 -> FilterExpression.Or(exp1, exp2) |> Ok
                | Error e, _ -> Error e
                | _, Error e -> Error e
            | ExpressionStatement.Any ss ->
                ss
                |> List.map FilterExpression.FromToken
                |> flattenResultList
                |> Result.map FilterExpression.Any
            | ExpressionStatement.All ss ->
                ss
                |> List.map FilterExpression.FromToken
                |> flattenResultList
                |> Result.map FilterExpression.Any


    and ArraySelector =
        | Index of int
        | Indexes of int list
        | Slice of int option * int option

        static member FromToken(token: string option) =
            token
            |> Option.bind (fun t ->
                match t.Contains(':'), t.Contains(',') with
                | true, _ ->
                    t.Split(':')
                    |> List.ofArray
                    |> List.map (fun v ->
                        match Int32.TryParse v with
                        | true, n -> Some n
                        | false, _ -> None)
                    |> (fun r ->
                        (r.Head,
                         match r.Length > 1 with
                         | true -> r.Tail.Head
                         | false -> None)
                        |> Slice
                        |> Some)
                | _, true ->
                    t.Split(',')
                    |> List.ofArray
                    |> List.map (fun v ->
                        match Int32.TryParse v with
                        | true, n -> Some n
                        | false, _ -> None)
                    |> List.choose id
                    |> Indexes
                    |> Some
                | false, false ->
                    match Int32.TryParse t with
                    | true, i -> Index i |> Some
                    | false, _ -> Index 0 |> Some) // TODO should this do this?

    and PathSection =
        { Selector: Selector
          FilterExpression: FilterExpression option
          ArraySelector: ArraySelector option }

    and Path =
        { Sections: PathSection list }

        static member Create(sections: PathSection list) = { Sections = sections }

        static member Compile(path: string, ?rootChar) =
            match Parsing.parse path (defaultArg rootChar '$') with
            | Parsing.ParserResult.Success tokens ->
                tokens
                |> List.map (fun t ->
                    { Selector = Selector.FromToken t.Selector
                      FilterExpression = None // TODO implement filter from token
                      ArraySelector = ArraySelector.FromToken t.ArraySelector })
                |> Path.Create
                |> Ok
            | _ -> Error "Failure to parse path."
