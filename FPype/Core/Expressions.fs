namespace FPype.Core

module Expressions =
 
    open FPype.Core.Parsing
    
    type ExpressionValueToken =
        | Literal of string
        | Reference
           
    type EvaluationResult =
        | True
        | False
        | WrongType
        | NonScalarValue
        | ValueNotFound
        | NotImplemented
    
    type ExpressionOperatorToken =
        | Exists of string
        | Equal of string * string
        | NotEqual of string * string
        | LessThan of string * string
        | LessThanOrEqual of string * string
        | GreaterThan of string * string
        | GreaterThanOrEqual of string * string
        | RegexMatch of string * string
        | In of string * string list
        | NotIn of string * string list
        | SubsetOf of string * string list
        | AnyOf of string * string list
        | NoneOf of string * string list
        | Size of string * string
        | Empty of string
        
        static member Deserialize(value: string) =
            
            let tryParse (operator: string) (handler: string -> string -> ExpressionOperatorToken) (result: ExpressionOperatorToken option) =
                match result, value.Contains operator with
                | Some v, _ -> Some v
                | None, true -> handler value operator |> Some
                | None, false -> None
                
            let split (separator: string) (str: string) =
                let s = str.Split(separator)
                s.[0].Trim(), s.[1].Trim()
                
            let splitList (separator: string) (str: string) =
                let (s1, s2) = split separator str
                // BUG what if comma in speech marks?
                // TODO add test for value ['a,b',c]
                s1, s2.Replace("[", "").Replace("]", "").Split(',') |> List.ofArray 
                
            // BUG this should pick up the wrong operator (such as 'in' in a delimited string).
            // TODO add test for value "'i in [1,2,3]' == j"
            None
            |> tryParse "==" (fun v op -> split op v |> ExpressionOperatorToken.Equal)
            |> tryParse "!=" (fun v op -> split op v |> ExpressionOperatorToken.NotEqual)
            |> tryParse "<=" (fun v op -> split op v |> ExpressionOperatorToken.LessThanOrEqual)
            |> tryParse ">=" (fun v op -> split op v |> ExpressionOperatorToken.GreaterThanOrEqual)
            |> tryParse "in" (fun v op -> splitList op v |> ExpressionOperatorToken.In)
            |> tryParse "nin" (fun v op -> splitList op v |> ExpressionOperatorToken.NotIn)
            |> tryParse "anyof" (fun v op -> splitList op v |> ExpressionOperatorToken.AnyOf)
            |> tryParse "noneof" (fun v op -> splitList op v |> ExpressionOperatorToken.NoneOf)
            |> tryParse "size" (fun v op -> split op v |> ExpressionOperatorToken.Size)
            |> tryParse "empty" (fun v _ -> ExpressionOperatorToken.Empty v)
            |> tryParse "<" (fun v op -> split op v |> ExpressionOperatorToken.LessThan)
            |> tryParse ">" (fun v op -> split op v |> ExpressionOperatorToken.GreaterThan)
            |> Option.defaultWith (fun _ -> ExpressionOperatorToken.Exists value)
            
    type ExpressionStatement =
        | Operator of ExpressionOperatorToken
        | And of ExpressionStatement * ExpressionStatement
        | Or of ExpressionStatement * ExpressionStatement
        | Any of ExpressionStatement list
        | All of ExpressionStatement list
        //| None of ExpressionStatement list
        

    module Parsing =
        
        type ExpressionStatementParseResult =
            | Success of ExpressionStatement
            | Failure of string
    
        // i == 1 && j > 2 
        //
        //          AND
        //           |
        //   Equal__/\__GreaterThan
        //   i_|_1        j_|_2
        //
        
        // ( i == 1 && j > i) || k > i
        //
        //            OR
        //            |    
        //       AND_/\_GreaterThan
        //        |       k_/\_i
        //Equals_/\_GreaterThan
        // i_/\_1    j_/\_i
        
        // Strat -
        //  1. sweep top layer (i.e. (...), ... ||, ... &&, ...<EOS>.
        //      a. '(', ')', ''' and '"' are delimiters for this stage.
        //  2. Create top layer tokens.
        //      a. If ...||, ...&& or ...<EOS> then expression operation, parse symbol and sides
        //      b. if (..) then nested expression. 
        //  3. Pass all nested expressions into the same function and repeat until all parsed.        
        
        let rec parseHandler value =
            let marks = [
                MarkDefinition.TwoCharSymbol ('&', '&')
                MarkDefinition.TwoCharSymbol ('|', '|')
                MarkDefinition.Nested ('(', ')')
            ]
            
            let input = ParsableInput.Create(value)
            let r = input.Mark(marks)
            r
            |> List.fold (fun (acc, lastSlice) m ->
                match m.Definition with
                | TwoCharSymbol (c1, c2) when c1 = '&' && c2 = '&' ->
                    (acc @ [ input.GetSlice(lastSlice, m.EndIndex - 2) |> Option.defaultValue "",
                             input.GetSlice(m.StartIndex, m.EndIndex) |> Option.defaultValue "" |> Some ], m.EndIndex + 1)
                | TwoCharSymbol (c1, c2) when c1 = '|' && c2 = '|' ->
                    (acc @ [ input.GetSlice(lastSlice, m.EndIndex - 2) |> Option.defaultValue "",
                             input.GetSlice(m.StartIndex, m.EndIndex) |> Option.defaultValue "" |> Some ], m.EndIndex + 1)
                | _ -> (acc, lastSlice)) ([], 0)
            |> fun (r, i) ->
                let eo =
                    input.GetSliceFromEnd i
                    |> Option.defaultValue ""
                    |> fun s -> s.Trim()
                    |> fun s ->
                        match s.StartsWith('(') with
                        | true -> parseHandler (s.Substring(1, s.Length - 2))
                        | false -> ExpressionOperatorToken.Deserialize s |> ExpressionStatement.Operator
                
                r
                |> List.rev
                |> List.fold (fun acc (slice, op) ->
                    match op with
                    | Some o when o = "&&" ->
                        // TODO handle better, is the slice a nested statement?
                        match slice.StartsWith('(') with
                        // -3 because the trim removes the last space but the length of slice doesnt change.
                        | true -> ExpressionStatement.And (parseHandler (slice.Substring(1, slice.Length - 3)), acc)
                        | false ->    
                            ExpressionStatement.And (ExpressionOperatorToken.Deserialize slice |> ExpressionStatement.Operator, acc)
                    | Some o when o = "||" ->
                        match slice.StartsWith('(') with
                        // -3 because the trim removes the last space but the length of slice doesnt change.
                        | true -> ExpressionStatement.Or (parseHandler (slice.Substring(1, slice.Length - 3)), acc)
                        | false ->
                            ExpressionStatement.Or (ExpressionOperatorToken.Deserialize slice |> ExpressionStatement.Operator, acc)
                    | _ -> failwith "Error") eo
        
        let parse input =
            try
                parseHandler input |> ExpressionStatementParseResult.Success
            with
            | ex -> ExpressionStatementParseResult.Failure ex.Message
        
        
    type ExpressionBuilder() =
        
        static member TryParse(input) =
            match Parsing.parse input with
            | Parsing.ExpressionStatementParseResult.Success es -> Ok es
            | Parsing.ExpressionStatementParseResult.Failure e -> Error e
            