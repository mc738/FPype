﻿namespace FPype.Actions

open System.Text.Json
open FPype.Core.Types
open FPype.Data.Models
open FPype.Data.Store
open FsToolbox.Core
open FsToolbox.Tools

[<RequireQualifiedAccess>]
module Extract =
    
    open System.IO
    open Freql.Csv
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store
    
    module Internal =
        
            
        type ErrorMessage =
            { Message: string
              LineNumber: int
              ColumnNumber: int
              Line: string }

        type ParseResult =
            | Success of TableRow
            | Warning of TableRow
            | Failure of ErrorMessage

        type ParseResults =
            { Rows: TableRow list
              Errors: ErrorMessage list }
        
        
        let createRows (columns: TableColumn list) (lines: string array)  =
            lines
            |> Array.mapi (fun lineNo l ->
                let splitLine = CsvParser.parseLine l

                match columns.Length <= splitLine.Length with
                | true ->
                    columns
                    |> List.mapi (fun colNo c ->
                        // Normalizers override default value concretion.
                        match c.ImportHandler with
                        | Some ih -> ih splitLine.[colNo]
                        | None -> Value.CoerceValueToType(splitLine.[colNo], c.Type)
                        |> fun r ->
                            match r with
                            | CoercionResult.Success fv -> Ok fv
                            | _ -> Error("Coerce failure.", lineNo, colNo))
                    |> List.fold
                        (fun (s, f) r ->
                            match r with
                            | Ok fv -> s @ [ fv ], f
                            | Error (m, l, c) -> s, f @ [ m, l, c ])
                        ([], [])
                    |> fun (s, f) ->
                        match f.Length = 0 with
                        | true -> { Values = s } |> ParseResult.Success
                        | false ->
                            let (m, ln, i) = f.Head

                            { Message = m
                              LineNumber = ln
                              ColumnNumber = i
                              Line = l }
                            |> ParseResult.Failure
                | false ->
                    ParseResult.Failure
                        { Message = "Split line is shorter than the column list"
                          LineNumber = lineNo
                          ColumnNumber = 0
                          Line = l })
            |> Array.fold
                (fun (s, f) r ->
                    match r with
                    | ParseResult.Success tr -> s @ [ tr ], f
                    | ParseResult.Warning tr -> s @ [ tr ], f
                    | ParseResult.Failure em -> s, f @ [ em ])
                ([], [])
            |> (fun (s, e) -> { Rows = s; Errors = e })
            
        let createGrokRows (columns: TableColumn list) (grok: Grok.GrokContext) (lines: string array)  =
            lines
            |> Array.mapi (fun lineNo l ->
                let splitLine = Grok.run grok l

                columns
                |> List.mapi (fun colNo c ->
                    
                    splitLine.TryFind c.Name
                    |> Option.map (fun v -> Value.CoerceValueToType(v, c.Type))
                    |> Option.defaultWith (fun _ ->
                        match c.Type with
                        | BaseType.Option _ -> CoercionResult.Success (Value.Option None)
                        | _ -> CoercionResult.Failure $"Column `{c.Name}` value not found")
                    |> fun r ->
                        match r with
                        | CoercionResult.Success fv -> Ok fv
                        | _ -> Error("Coerce failure.", lineNo, colNo))
                |> List.fold
                    (fun (s, f) r ->
                        match r with
                        | Ok fv -> s @ [ fv ], f
                        | Error (m, l, c) -> s, f @ [ m, l, c ])
                    ([], [])
                |> fun (s, f) ->
                    match f.Length = 0 with
                    | true -> { Values = s } |> ParseResult.Success
                    | false ->
                        let (m, ln, i) = f.Head

                        { Message = m
                          LineNumber = ln
                          ColumnNumber = i
                          Line = l }
                        |> ParseResult.Failure)
            |> Array.fold
                (fun (s, f) r ->
                    match r with
                    | ParseResult.Success tr -> s @ [ tr ], f
                    | ParseResult.Warning tr -> s @ [ tr ], f
                    | ParseResult.Failure em -> s, f @ [ em ])
                ([], [])
            |> (fun (s, e) -> { Rows = s; Errors = e })
      
    [<RequireQualifiedAccess>]
    module ``parse-csv`` =
        let name = "parse-csv"
        
        type Parameters =
            {
                DataSource: string
                Table: TableModel
            }
        
        let run (parameters: Parameters) (store: PipelineStore) =
            store.GetDataSource parameters.DataSource
            |> Option.map (fun ds ->
                match ds.Type with
                | "file" ->
                    try
                        File.ReadAllLines ds.Uri |> Ok
                    with
                    | exn -> Error $"Could not load file `{ds.Uri}`: {exn.Message}"
                | _ -> Error $"Unsupported source type: `{ds.Type}`")
            |> Option.defaultWith (fun _ -> Error $"Data source `{name}` not found.")
            |> Result.map (Internal.createRows parameters.Table.Columns)
            |> Result.bind (fun r ->
                match r.Errors |> List.isEmpty |> not with
                | true ->
                    store.LogWarning("parse_csv", $"Error parsing {r.Errors.Length} row(s)")
                    r.Errors |> List.map (fun e -> store.AddImportError("parse_csv", e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}")) |> ignore
                | false -> ()
                store.CreateTable(parameters.Table)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log("parse_csv", $"Imported {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)
    
        let createAction (parameters: Parameters) = run parameters |> createAction name
    
    /// Split a source into individual chucks for processing.
    /// This is essentially a preprocessing action.
    let splitSource (path: string) =
        
        ()
    
    [<RequireQualifiedAccess>]
    module Grok =
        
        let name = "grok"
        
        
        let run (name: string) (columns: TableColumn list) (tableName: string) (store: PipelineStore) =
            //store
            
            
            let grok = Grok.create ([] |> Map.ofList) ""
            
            store.GetDataSource name
            |> Option.map (fun ds ->
                match ds.Type with
                | "file" ->
                    try
                        File.ReadAllLines ds.Uri |> Ok
                    with
                    | exn -> Error $"Could not load file `{ds.Uri}`: {exn.Message}"
                | _ -> Error $"Unsupported source type: `{ds.Type}`")
            |> Option.defaultWith (fun _ -> Error $"Data source `{name}` not found.")
            |> Result.map (Internal.createGrokRows columns grok)
            |> Result.bind (fun r ->
                match r.Errors |> List.isEmpty |> not with
                | true ->
                    store.LogWarning("grok", $"Error parsing {r.Errors.Length} row(s)")
                    r.Errors |> List.map (fun e -> store.AddImportError("parse_csv", e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}")) |> ignore
                | false -> ()
                store.CreateTable(tableName, columns)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log("grok", $"Imported {r.Length} row(s) to table `{tableName}`.")
                store) 
        
        (*
        let deserialize (element: JsonElement) =
            match Json.tryGetStringProperty "source" element, Json.tryGetStringProperty "table" element with
            | Some source, Some tableName ->
                Tables.getTable ctx tableName
                |> Option.map (fun t ->
                    Tables.createColumns ctx t.Name
                    |> Result.map (fun tc -> run source tc t.Name))
                |> Option.defaultValue (Error $"Table `{tableName}` not found")
            | None, _ -> Error "Missing source property"
            | _, None -> Error "Missing table property"
        *)
        