namespace FPype.Core

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
            | ChildUnion names ->
                names
                |> List.map (fun n -> Json.tryGetProperty n node)
            | ChildWildCard ->
                node.EnumerateObject()
                |> List.ofSeq
                |> List.map (fun n -> Some n.Value)
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
                    let foundNodes =
                        names
                        |> List.map (fun n -> Json.tryGetProperty n node)

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
                        let cn =
                            currNode.EnumerateObject() |> List.ofSeq

                        let curr =
                            cn |> List.map (fun i -> Some i.Value)

                        curr
                        @ (currNode.EnumerateObject()
                           |> List.ofSeq
                           |> List.collect (fun p -> search p.Value))
                    | false -> [ None ]

                node.EnumerateObject()
                |> List.ofSeq
                |> List.map (fun n -> Some n.Value)

            |> List.choose id

        member s.Handler(nodes: JsonElement list) = nodes |> List.collect s.Run

    type JPathFilterOperator =
        { FilterOperator: FilterOperator }

        static member Create(operator: FilterOperator) = { FilterOperator = operator }

        member jpfo.Evaluate(currentNode: JsonElement) =

            let boolToResult (v: bool) =
                match v with
                | true -> EvaluationResult.True
                | false -> EvaluationResult.False

            let numericOperator (fv1: FilterValue) (fv2: FilterValue) =
                match fv1.GetNumericValue(), fv2.GetNumericValue() with
                | Ok v1, Ok v2 -> Ok(v1, v2)
                | Error e, _ -> Error e
                | _, Error e -> Error e

            let stringOperator (fv1: FilterValue) (fv2: FilterValue) =
                match fv1.GetNumericValue(), fv2.GetNumericValue() with
                | Ok v1, Ok v2 -> Ok(v1, v2)
                | Error e, _ -> Error e
                | _, Error e -> Error e

            match jpfo.FilterOperator with
            | Exists fv -> EvaluationResult.NotImplemented
            | Equal (fv1, fv2) -> EvaluationResult.NotImplemented
            | NotEqual (fv1, fv2) -> EvaluationResult.NotImplemented
            | LessThan (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 < v2 |> boolToResult
                | Error e -> e
            | LessThanOrEqual (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 <= v2 |> boolToResult
                | Error e -> e
            | GreaterThan (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 > v2 |> boolToResult
                | Error e -> e
            | GreaterThanOrEqual (fv1, fv2) ->
                match numericOperator fv1 fv2 with
                | Ok (v1, v2) -> v1 >= v2 |> boolToResult
                | Error e -> e
            | RegexMatch (fv, pattern) -> EvaluationResult.NotImplemented
            | In (fv, fvSet) -> EvaluationResult.NotImplemented
            | NotIn (fv, fvSet) -> EvaluationResult.NotImplemented
            | SubsetOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | AnyOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | NoneOf (fv, fvSet) -> EvaluationResult.NotImplemented
            | Size (fv1, fv2) -> EvaluationResult.NotImplemented
            | Empty fv ->

                EvaluationResult.NotImplemented

    type JPathArraySelector =
        { ArraySelector: ArraySelector }

        static member Create(selector: ArraySelector) = { ArraySelector = selector }

        member jpas.Run(array: JsonElement) =
            let nodes =
                array.EnumerateArray() |> List.ofSeq

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

        member s.Handle(nodes: JsonElement list) =
            nodes |> List.collect s.Run

    and JPathSection =
        { Selector: JPathSelector
          FilterExpression: FilterExpression option
          ArraySelector: JPathArraySelector option }

        static member Create(section: PathSection) =
            ({ Selector = JPathSelector.Create section.Selector
               FilterExpression = section.FilterExpression
               ArraySelector =
                 section.ArraySelector
                 |> Option.map JPathArraySelector.Create }: JPathSection)

    and JPath =
        { Sections: JPathSection list }

        static member Create(sections: JPathSection list) = { Sections = sections }

        static member Create(path: Path) = { Sections = path.Sections |> List.map JPathSection.Create }
        
        static member Compile(path: string) =
            Path.Compile path
            |> Result.map JPath.Create
            
        member p.Run(root: JsonElement) =
            p.Sections
            |> List.fold
                (fun els s ->
                    s.Selector.Handler els
                    |> fun r ->
                        match s.ArraySelector with
                        | Some arrs -> arrs.Handle r
                        | None -> r)
                [ root ]

