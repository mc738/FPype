namespace FPype.Core

module Queries =

    open System
    open FsToolbox.Extensions

    type QueryFragment =
        { Sql: string
          Parameters: (int * obj) list
          StartIndex: int
          EndIndex: int }

        member qf.NextIndex() =
            match qf.Parameters.IsEmpty with
            | true ->
                // If there are no parameters there should no increase in the index from this fragment.
                // So just return the start index.
                qf.StartIndex
            | false -> qf.EndIndex + 1

    type Pagination =
        { PageSize: int
          PageNumber: int }

        /// <summary>
        ///     Generate query parameters and an sql statement from a Paging record.
        ///     It should be noted, this does use string interpolation,
        ///     however this is insert parameters names (@1, @2 etc) so should not be =
        ///     vulnerable to sql inject.
        /// </summary>
        /// <param name="i">The starting index for
        ///
        /// </param>
        member p.ToQueryFragment(i: int) =
            // SECURITY Uses string interpolation to create statement.
            // MITIGATION this should be safe from sqlite injection because.
            // NOTE removed - 1 from page number calculation (before - p.PageNumber) for use with mudblazor
            ({ Sql = $"LIMIT @{i} OFFSET @{i + 1}"
               Parameters = [ i, box p.PageSize; i + 1, box ((p.PageNumber) * p.PageSize) ]
               StartIndex = i
               EndIndex = i + 1 }: QueryFragment)

    [<RequireQualifiedAccess>]
    type Order =
        | Ascending
        | Descending

        /// <summary>Deserialized a string to OrderBy - "desc" (case insensitive) for descending, anything else for ascending.</summary>
        static member Deserialize(str: string) =
            match str.Equals("desc", StringComparison.OrdinalIgnoreCase) with
            | true -> Order.Descending
            | false -> Order.Ascending

        member o.Serialize() =
            match o with
            | Order.Ascending -> "ASC"
            | Order.Descending -> "DESC"

    [<RequireQualifiedAccess>]
    type DateRange =
        { From: DateTime option
          FromInclusive: bool
          To: DateTime option
          ToInclusive: bool }

        member dr.FromComparer() =
            match dr.FromInclusive with
            | true -> ">="
            | false -> ">"

        member dr.ToComparer() =
            match dr.ToInclusive with
            | true -> "<="
            | false -> "<"

    [<RequireQualifiedAccess>]
    type StringFilterType =
        | StartsWith
        | EndsWith
        | Contains
        | Exact

        static member Deserialize(str: string) =
            match str.ToLower() with
            | "startswith"
            | "start"
            | "sw"
            | "s" -> StringFilterType.StartsWith
            | "endswith"
            | "end"
            | "ew"
            | "e" -> StringFilterType.EndsWith
            | "contains"
            | "c" -> StringFilterType.Contains
            | "exact"
            | "ex" -> StringFilterType.Exact
            | _ -> StringFilterType.Contains

    type StringFilter =
        { Value: string
          CaseInsensitive: bool
          FilterType: StringFilterType }

    type BespokeFilterPart =
        { Handler: int -> (string * (int * obj) list * int) }

    type FilterPart =
        | DateRange of FieldName: string * Range: DateRange
        | StringFilter of FieldName: string * Filter: StringFilter
        | Equals of FieldName: string * Value: obj
        | Boolean of FieldName: string * value: bool option
        | Bespoke of BespokeFilterPart
        | And of FilterPart * FilterPart
        | Or of FilterPart * FilterPart
        | All of FilterPart list
        | Any of FilterPart list

        member qp.ToQueryFragment(startI: int) =
            let handleStringFilter (sf: StringFilter) (i: int) (fieldName: string) =
                let sql, value =
                    match sf.FilterType with
                    | StringFilterType.StartsWith -> $"{fieldName} LIKE @{i}", $"{sf.Value}%%"
                    | StringFilterType.EndsWith -> $"{fieldName} LIKE @{i}", $"%%@{sf.Value}"
                    | StringFilterType.Contains -> $"{fieldName} LIKE @{i}", $"%%{sf.Value}%%"
                    | StringFilterType.Exact -> $"{fieldName} = @{i}", sf.Value

                sql, (i, box value), i + 1

            let handleDateRange (dr: DateRange) (i: int) (fieldName: string) =

                let createFromComparison (dt: DateTime) (index: int) =
                    $"DATE({fieldName}) {dr.FromComparer()} DATE(@{index})"

                let createToComparison (dt: DateTime) (index: int) =
                    $"DATE({fieldName}) {dr.ToComparer()} DATE(@{index})"

                match dr.From, dr.To with
                | Some fromDt, Some toDt ->
                    $"({createFromComparison fromDt i} AND {createToComparison toDt (i + 1)})",
                    [ i, box fromDt; i + 1, box toDt ],
                    i + 2
                | Some fromDt, None -> createFromComparison fromDt i, [ i, box fromDt ], i + 1
                | None, Some toDt -> createToComparison toDt i, [ i, box toDt ], i + 1
                | None, None -> "", [], i

            let concat (wrap: bool) (prefix: string option) (values: string list) =
                values
                |> List.filter (fun s -> s.IsNotNullOrWhiteSpace())
                |> String.concat " "
                |> fun r ->
                    match wrap, prefix with
                    | true, Some v -> $"{v}({r})"
                    | true, None -> $"({r})"
                    | false, Some v -> $"{v}{r}"
                    | false, None -> r

            let rec build (curr: FilterPart, acc: string list, parameters: (int * obj) list, currI: int) =
                // PERFORMANCE this could be more efficient with prepending (for example sql :: acc then later List.rev).
                // NOTE skipped optimization for now because queries shouldn't get that large for it to matter and this is simpler to reason about.
                // TEST NEEDED test that generated query is correct.
                // TEST NEEDED test bad strings can not be injected (`EXPLOIT` for testing purposes).
                // BUG if filter type is FilterBy.Any/FilterBy.All with an empty list it should be skipped (And and Or need to check this?)
                match curr with

                | DateRange (fieldName, range) ->
                    let (sql, newParameters, newI) = handleDateRange range currI fieldName

                    acc @ [ sql ], parameters @ newParameters, newI
                | StringFilter (fieldName, filter) ->
                    let (sql, parameter, newI) = handleStringFilter filter currI fieldName

                    acc @ [ sql ], parameters @ [ parameter ], newI
                | Bespoke bqp ->
                    let sql, newParameters, newI = bqp.Handler currI
                    acc @ [ sql ], parameters @ newParameters, newI
                | Equals (fieldName, value) ->
                    acc @ [ $"{fieldName} = @{currI}" ], parameters @ [ currI, value ], (currI + 1)
                | Boolean (fieldName, value) ->
                    match value with
                    | Some true -> acc @ [ $"{fieldName} = TRUE" ], parameters, currI
                    | Some false -> acc @ [ $"{fieldName} = FALSE" ], parameters, currI
                    | None ->
                        // NOTE effectively a pass through.
                        acc, parameters, currI
                | And (a, b) ->
                    // Create a then pass values into b
                    // Test if a and b have values to stop invalid sql bug

                    let vA =
                        match a.IsEmpty() with
                        | true -> None
                        | false -> build (a, [], [], currI) |> Some

                    let vB =
                        match b.IsEmpty() with
                        | true -> None
                        | false ->
                            build (
                                b,
                                [],
                                [],
                                vA |> Option.map (fun (_, _, newI) -> newI) |> Option.defaultValue currI
                            )
                            |> Some

                    //let (sqlA, parametersA, newI) = build (a, [], [], currI)

                    //let (sqlB, parametersB, newI) = build (b, [], [], newI)

                    match vA, vB with
                    | Some (sqlA, parametersA, _), Some (sqlB, parametersB, newI) ->
                        [ sqlA |> concat true None; "AND"; sqlB |> concat true None ],
                        parameters @ parametersA @ parametersB,
                        newI
                    | Some (sqlA, parametersA, newI), None ->
                        [ sqlA |> concat true None ], parameters @ parametersA, newI
                    | None, Some (sqlB, parametersB, newI) ->
                        [ sqlB |> concat true None ], parameters @ parametersB, newI
                    | None, None -> [], parameters, currI
                | Or (a, b) ->
                    (*
                    // Create a then pass values into b
                    let (sqlA, parametersA, newI) = build (a, [], [], currI)

                    let (sqlB, parametersB, newI) = build (b, [], [], newI)

                    [ sqlA |> concat true None; "OR"; sqlB |> concat true None ],
                    parameters @ parametersA @ parametersB,
                    newI
                    *)
                    // Create a then pass values into b
                    // Test if a and b have values to stop invalid sql bug

                    let vA =
                        match a.IsEmpty() with
                        | true -> None
                        | false -> build (a, [], [], currI) |> Some

                    let vB =
                        match b.IsEmpty() with
                        | true -> None
                        | false ->
                            build (
                                b,
                                [],
                                [],
                                vA |> Option.map (fun (_, _, newI) -> newI) |> Option.defaultValue currI
                            )
                            |> Some

                    //let (sqlA, parametersA, newI) = build (a, [], [], currI)

                    //let (sqlB, parametersB, newI) = build (b, [], [], newI)

                    match vA, vB with
                    | Some (sqlA, parametersA, _), Some (sqlB, parametersB, newI) ->
                        [ sqlA |> concat true None; "OR"; sqlB |> concat true None ],
                        parameters @ parametersA @ parametersB,
                        newI
                    | Some (sqlA, parametersA, newI), None ->
                        [ sqlA |> concat true None ], parameters @ parametersA, newI
                    | None, Some (sqlB, parametersB, newI) ->
                        [ sqlB |> concat true None ], parameters @ parametersB, newI
                    | None, None -> [], parameters, currI
                | All filters ->
                    let (sql, p, nextI) =
                        filters
                        |> List.fold
                            (fun (acc, parameters, nextI) f -> build (f, acc, parameters, nextI))
                            ([], parameters, currI)

                    [ sql |> String.concat " AND " |> (fun r -> $"({r})") ], p, nextI
                | Any filters ->
                    let (sql, p, nextI) =
                        filters
                        |> List.fold
                            (fun (acc, parameters, nextI) f -> build (f, acc, parameters, nextI))
                            ([], parameters, currI)

                    [ sql |> String.concat " OR " |> (fun r -> $"({r})") ], p, nextI

            let (sql, parameters, newI) = build (qp, [], [], startI)

            let endI =
                match parameters.IsEmpty with
                | true -> startI
                | false -> newI - 1

            ({ Sql = sql |> concat false (Some "WHERE ")
               Parameters = parameters
               StartIndex = startI
               EndIndex = endI }: QueryFragment)

        member fp.IsEmpty() =
            let rec hasValue (curr) =
                match curr with
                | FilterPart.Any fs when fs.IsEmpty -> false
                | FilterPart.All fs when fs.IsEmpty -> false
                | FilterPart.Any fs
                | FilterPart.All fs -> fs |> List.exists (hasValue)
                | FilterPart.Boolean (_, value) -> value.IsSome
                | _ -> true

            hasValue fp |> not

    type Query = { Filter: string }

