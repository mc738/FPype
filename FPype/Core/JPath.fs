namespace FPype.Core

open System
open System.Text.Json
open System.Text.RegularExpressions
open FPype.Core.Expressions
open FPype.Core.Paths
open Google.Protobuf.WellKnownTypes

module JPath =

    open System
    open System.Text.Json
    open FsToolbox.Core
    open FPype.Core.Paths

    type JPathSelector =
        { Selector: Selector }

        static member Create(selector: Selector) = { Selector = selector }

        member jps.Run(node: JsonElement) =

            let tryGetPropertyList name =
                match node.ValueKind = JsonValueKind.Array with
                | true ->
                    node.EnumerateArray()
                    |> List.ofSeq
                    |> List.map (fun n -> Json.tryGetProperty name n)
                | false -> [ Json.tryGetProperty name node ]

            match jps.Selector with
            | Child name -> tryGetPropertyList name
            | ChildUnion names -> names |> List.map (fun n -> Json.tryGetProperty n node)
            | ChildWildCard -> node.EnumerateObject() |> List.ofSeq |> List.map (fun n -> Some n.Value)
            | DeepScan name ->
                let rec search (currNode: JsonElement) =
                    // If any of nodes
                    match Json.tryGetProperty name currNode, currNode.ValueKind = JsonValueKind.Object with
                    | Some p, _ -> [ Some p ]
                    | None, true ->
                        currNode.EnumerateObject()
                        |> List.ofSeq
                        |> List.collect (fun p -> search p.Value)
                    | None, false -> [ None ]

                node |> search
            | DeepScanUnion names ->
                let rec search (currNode: JsonElement) =
                    // If any of nodes
                    let foundNodes = names |> List.map (fun n -> Json.tryGetProperty n node)

                    match currNode.ValueKind = JsonValueKind.Object with
                    | true ->
                        foundNodes
                        @ (currNode.EnumerateObject()
                           |> List.ofSeq
                           |> List.collect (fun p -> search p.Value))
                    | false -> [ None ]

                node |> search

            | DeepScanWildCard ->
                let rec search (currNode: JsonElement) =
                    // If any of nodes
                    match currNode.ValueKind = JsonValueKind.Object with
                    | true ->
                        let cn = currNode.EnumerateObject() |> List.ofSeq

                        let curr = cn |> List.map (fun i -> Some i.Value)

                        curr
                        @ (currNode.EnumerateObject()
                           |> List.ofSeq
                           |> List.collect (fun p -> search p.Value))
                    | false -> [ None ]

                node.EnumerateObject() |> List.ofSeq |> List.map (fun n -> Some n.Value)

            |> List.choose id

        member jps.RunScalar(node: JsonElement) = jps.Run(node) |> List.tryHead

        member s.Handler(nodes: JsonElement list) = nodes |> List.collect s.Run

    and JPathFilterOperator =
        { FilterOperator: FilterOperator }

        static member Create(operator: FilterOperator) = { FilterOperator = operator }

        member jpfo.Evaluate(currentNode: JsonElement, rootNode: JsonElement) =

            let resolveStringValue (fv: FilterValue) =
                match fv with
                | FilterValue.String s -> Ok s
                | FilterValue.Numeric n -> n.ToString() |> Ok
                | FilterValue.CurrentNode p ->
                    let jPath: JPath = JPath.Create(p)

                    jPath.RunScalar(currentNode)
                    |> Option.map (fun (el: JsonElement) ->
                        match el.ValueKind with
                        | JsonValueKind.String -> el.GetString() |> Ok
                        | JsonValueKind.True -> Ok "true"
                        | JsonValueKind.False -> Ok "false"
                        | JsonValueKind.Number -> el.GetDecimal() |> string |> Ok
                        | _ -> Error EvaluationResult.NonScalarValue)
                    |> Option.defaultWith (fun _ -> Error EvaluationResult.ValueNotFound)
                | FilterValue.RootNode p ->
                    let jPath: JPath = JPath.Create(p)

                    jPath.RunScalar(rootNode)
                    |> Option.map (fun (el: JsonElement) ->
                        match el.ValueKind with
                        | JsonValueKind.String -> el.GetString() |> Ok
                        | JsonValueKind.True -> Ok "true"
                        | JsonValueKind.False -> Ok "false"
                        | JsonValueKind.Number -> el.GetDecimal() |> string |> Ok
                        | _ -> Error EvaluationResult.NonScalarValue)
                    |> Option.defaultWith (fun _ -> Error EvaluationResult.ValueNotFound)

            let resolveNumericValue (fv: FilterValue) =
                match fv with
                | FilterValue.String s ->
                    match Decimal.TryParse s with
                    | true, v -> Ok v
                    | false, _ -> Error EvaluationResult.WrongType
                | FilterValue.Numeric n -> Ok n
                | FilterValue.CurrentNode p ->
                    let jPath: JPath = JPath.Create(p)

                    jPath.RunScalar(currentNode)
                    |> Option.map (fun (el: JsonElement) ->
                        match el.ValueKind with
                        | JsonValueKind.String ->
                            el.GetString()
                            |> fun s ->
                                match Decimal.TryParse s with
                                | true, v -> Ok v
                                | false, _ -> Error EvaluationResult.WrongType
                        | JsonValueKind.True -> Ok 1m
                        | JsonValueKind.False -> Ok 0m
                        | JsonValueKind.Number -> el.GetDecimal() |> Ok
                        | _ -> Error EvaluationResult.NonScalarValue)
                    |> Option.defaultWith (fun _ -> Error EvaluationResult.ValueNotFound)
                | FilterValue.RootNode p ->
                    let jPath: JPath = JPath.Create(p)

                    jPath.RunScalar(rootNode)
                    |> Option.map (fun (el: JsonElement) ->
                        match el.ValueKind with
                        | JsonValueKind.String ->
                            el.GetString()
                            |> fun s ->
                                match Decimal.TryParse s with
                                | true, v -> Ok v
                                | false, _ -> Error EvaluationResult.WrongType
                        | JsonValueKind.True -> Ok 1m
                        | JsonValueKind.False -> Ok 0m
                        | JsonValueKind.Number -> el.GetDecimal() |> Ok
                        | _ -> Error EvaluationResult.NonScalarValue)
                    |> Option.defaultWith (fun _ -> Error EvaluationResult.ValueNotFound)

            //|> Option.map (fun n -> n )

            let boolToResult (v: bool) =
                match v with
                | true -> EvaluationResult.True
                | false -> EvaluationResult.False

            let numericOperator (fv1: FilterValue) (fv2: FilterValue) =
                match resolveNumericValue fv1, resolveNumericValue fv2 with
                | Ok v1, Ok v2 -> Ok(v1, v2)
                | Error e, _ -> Error e
                | _, Error e -> Error e

            let stringOperator (fv1: FilterValue) (fv2: FilterValue) =
                match resolveStringValue fv1, resolveStringValue fv2 with
                | Ok v1, Ok v2 -> Ok(v1, v2)
                | Error e, _ -> Error e
                | _, Error e -> Error e

            match jpfo.FilterOperator with
            | FilterOperator.Exists fv ->

                EvaluationResult.NotImplemented
            | FilterOperator.Equal (fv1, fv2) ->
                // TODO Check if number or string?
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 = v2 |> boolToResult
                | Error e -> e

            | FilterOperator.NotEqual (fv1, fv2) ->
                // TODO Check if number or string?
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 <> v2 |> boolToResult
                | Error e -> e
            | FilterOperator.LessThan (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 < v2 |> boolToResult
                | Error e -> e
            | FilterOperator.LessThanOrEqual (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 <= v2 |> boolToResult
                | Error e -> e
            | FilterOperator.GreaterThan (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 > v2 |> boolToResult
                | Error e -> e
            | FilterOperator.GreaterThanOrEqual (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 >= v2 |> boolToResult
                | Error e -> e
            | FilterOperator.RegexMatch (fv, pattern) ->
                match stringOperator fv pattern with
                | Ok (v1, v2) -> Regex.IsMatch(v1, v2) |> boolToResult
                | Error e -> e
            | FilterOperator.In (fv, fvSet) -> EvaluationResult.NotImplemented
            | FilterOperator.NotIn (fv, fvSet) -> EvaluationResult.NotImplemented
            | FilterOperator.SubsetOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | FilterOperator.AnyOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | FilterOperator.NoneOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | FilterOperator.Size (fv1, fv2) -> EvaluationResult.NotImplemented
            | FilterOperator.Empty fv ->

                EvaluationResult.NotImplemented

    and [<RequireQualifiedAccess>] JPathFilterExpression =
        | Operator of JPathFilterOperator
        | And of JPathFilterExpression * JPathFilterExpression
        | Or of JPathFilterExpression * JPathFilterExpression
        | All of JPathFilterExpression list
        | Any of JPathFilterExpression list

        static member Create(expr: FilterExpression) =
            match expr with
            | FilterExpression.Operator op -> JPathFilterOperator.Create op |> JPathFilterExpression.Operator
            | FilterExpression.And (op1, op2) ->
                (JPathFilterExpression.Create op1, JPathFilterExpression.Create op2)
                |> JPathFilterExpression.And
            | FilterExpression.Or (op1, op2) ->
                (JPathFilterExpression.Create op1, JPathFilterExpression.Create op2)
                |> JPathFilterExpression.Or
            | FilterExpression.All ops -> ops |> List.map JPathFilterExpression.Create |> JPathFilterExpression.All
            | FilterExpression.Any ops -> ops |> List.map JPathFilterExpression.Create |> JPathFilterExpression.Any

        member jfe.Evaluate(currentNode: JsonElement, rootNode: JsonElement) =
            let resultToBool (r: EvaluationResult) =
                match r with
                | EvaluationResult.True -> true
                | _ -> false

            match jfe with
            | Operator expr -> expr.Evaluate(currentNode, rootNode) |> resultToBool
            | And (expr1, expr2) -> expr1.Evaluate(currentNode, rootNode) && expr2.Evaluate(currentNode, rootNode)
            | Or (expr1, expr2) -> expr1.Evaluate(currentNode, rootNode) || expr2.Evaluate(currentNode, rootNode)
            | All (exprs) ->
                exprs
                |> List.map (fun expr -> expr.Evaluate(currentNode, rootNode))
                |> List.exists (fun r -> r = false)
                |> not
            | Any (exprs) ->
                exprs
                |> List.map (fun expr -> expr.Evaluate(currentNode, rootNode))
                |> List.exists (fun r -> r = true)

        member jfe.Handle(nodes: JsonElement list, rootNode: JsonElement) =
            nodes
            |> List.collect (fun el ->
                match el.ValueKind with
                | JsonValueKind.Array ->
                    el.EnumerateArray()
                    |> List.ofSeq
                    |> List.filter (fun el -> jfe.Evaluate(el, rootNode))
                | _ -> [])


    and JPathArraySelector =
        { ArraySelector: ArraySelector }

        static member Create(selector: ArraySelector) = { ArraySelector = selector }

        member jpas.Run(array: JsonElement) =
            match array.ValueKind with
            | JsonValueKind.Array ->
                let nodes = array.EnumerateArray() |> List.ofSeq

                match jpas.ArraySelector with
                | Index i ->
                    match nodes.Length > i with
                    | true -> [ nodes.[i] ]
                    | false -> []
                | Indexes indexes ->
                    nodes
                    |> List.mapi (fun i n ->
                        match List.contains i indexes with
                        | true -> Some n
                        | false -> None)
                    |> List.choose id
                | Slice (s, e) ->
                    match s, e with
                    | Some i1, Some i2 ->
                        match nodes.Length > i1, nodes.Length > i2 with
                        | true, true -> nodes.[i1..i2]
                        | _ -> []
                    | Some i, None ->
                        match nodes.Length > i with
                        | true -> nodes.[i..]
                        | false -> []
                    | None, Some i ->
                        match nodes.Length > i with
                        | true -> nodes.[..i]
                        | false -> []
                    | None, None -> nodes
            | _ -> []

        member s.Handle(nodes: JsonElement list) = nodes |> List.collect s.Run

    and JPathSection =
        { Selector: JPathSelector
          FilterExpression: JPathFilterExpression option
          ArraySelector: JPathArraySelector option }

        static member Create(section: PathSection) =
            ({ Selector = JPathSelector.Create section.Selector
               FilterExpression = section.FilterExpression |> Option.map JPathFilterExpression.Create
               ArraySelector = section.ArraySelector |> Option.map JPathArraySelector.Create }: JPathSection)

    and JPath =
        { Sections: JPathSection list }

        static member Create(sections: JPathSection list) = { Sections = sections }

        static member Create(path: Path) =
            { Sections = path.Sections |> List.map JPathSection.Create }

        static member Compile(path: string) =
            Path.Compile path |> Result.map JPath.Create

        member p.Run(root: JsonElement) =
            p.Sections
            |> List.fold
                (fun els s ->
                    s.Selector.Handler els
                    |> fun r ->
                        match s.ArraySelector with
                        | Some arrs -> arrs.Handle r
                        | None -> r
                    |> fun r ->
                        match s.FilterExpression with
                        | Some expr -> expr.Handle(r, root)
                        | None -> r)
                [ root ]


        member p.RunScalar(root: JsonElement) = p.Run(root) |> List.tryHead
