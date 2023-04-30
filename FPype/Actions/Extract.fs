namespace FPype.Actions

[<RequireQualifiedAccess>]
module Extract =

    open System
    open Freql.Sqlite
    open System.Text.Json
    open FPype.Core.Types
    open FPype.Data.Models
    open FPype.Data.Store
    open FsToolbox.Core
    open FsToolbox.Tools
    open FsToolbox.Extensions
    open FPype.Core
    open System.IO
    open Freql.Csv

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


        let createRows (columns: TableColumn list) (lines: string array) =
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

        let parseGrokPattern (str: string) =
            match str.StartsWith('#') || String.IsNullOrWhiteSpace(str) with
            | true -> None
            | false ->
                let i = str.IndexOf(' ')
                Some(str.[0 .. i - 1], str.[i + 1 ..])

        let createGrokRows (columns: TableColumn list) (grok: Grok.GrokContext) (lines: string array) =
            lines
            |> Array.mapi (fun lineNo l ->
                let splitLine = Grok.run grok l

                columns
                |> List.mapi (fun colNo c ->

                    splitLine.TryFind c.Name
                    |> Option.map (fun v ->
                        match c.ImportHandler with
                        | Some ih -> ih v
                        | None -> Value.CoerceValueToType(v, c.Type))
                    |> Option.defaultWith (fun _ ->
                        match c.Type with
                        | BaseType.Option _ -> CoercionResult.Success(Value.Option None)
                        | _ -> CoercionResult.Failure $"Column `{c.Name}` value not found")
                    |> fun r ->
                        match r with
                        | CoercionResult.Success fv -> Ok fv
                        | _ -> Error("Coerce failure.", lineNo, colNo))
                |> List.fold
                    (fun (s, f) r ->
                        // PERFORMANCE this used append and then rev because it performs better.
                        match r with
                        | Ok fv -> fv :: s, f
                        | Error (m, l, c) -> s, (m, l, c) :: f)
                    ([], [])
                |> fun (s, f) -> s |> List.rev, f |> List.rev
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
                    // PERFORMANCE this used append and then rev because it performs better.
                    match r with
                    | ParseResult.Success tr -> tr :: s, f
                    | ParseResult.Warning tr -> tr :: s, f
                    | ParseResult.Failure em -> s, em :: f)
                ([], [])
            |> (fun (s, e) ->
                { Rows = s |> List.rev
                  Errors = e |> List.rev })

    [<RequireQualifiedAccess>]
    module ``parse-csv`` =
        let name = "parse_csv"

        type Parameters =
            { DataSource: string
              Table: TableModel }

        let run (parameters: Parameters) (store: PipelineStore) =
            getDataSourceAsLinesByName store parameters.DataSource
            |> Result.map (Internal.createRows parameters.Table.Columns)
            |> Result.bind (fun r ->
                match r.Errors |> List.isEmpty |> not with
                | true ->
                    store.LogWarning("parse_csv", $"Error parsing {r.Errors.Length} row(s)")

                    r.Errors
                    |> List.map (fun e ->
                        store.AddImportError("parse_csv", e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"))
                    |> ignore
                | false -> ()

                store.CreateTable(parameters.Table)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log("parse_csv", $"Imported {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    [<RequireQualifiedAccess>]
    module ``parse-csv-collection`` =
        let name = "parse_csv_collection"

        type Parameters =
            { CollectionName: string
              Table: TableModel }

        let run (parameters: Parameters) (store: PipelineStore) =
            store.GetSourcesByCollection parameters.CollectionName
            |> List.map (fun ds ->
                getDataSourceAsLines store ds
                |> Result.map (Internal.createRows parameters.Table.Columns)
                |> Result.bind (fun r ->
                    match r.Errors |> List.isEmpty |> not with
                    | true ->
                        store.LogWarning("parse_csv", $"Error parsing {r.Errors.Length} row(s) from source `{ds.Name}`")

                        r.Errors
                        |> List.map (fun e ->
                            store.AddImportError(name, e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"))
                        |> ignore
                    | false -> ()

                    store.CreateTable(parameters.Table)
                    |> fun t -> { t with Rows = r.Rows }
                    |> store.InsertRows)
                |> Result.map (fun r ->
                    store.Log(
                        name,
                        $"Imported {r.Length} row(s) from data source `{ds.Name}` to table `{parameters.Table.Name}`."
                    )
                // TODO add result?
                ))
            |> flattenResultList
            |> Result.map (fun _ -> store)

        let createAction (parameters: Parameters) = run parameters |> createAction name

    /// Split a source into individual chucks for processing.
    /// This is essentially a preprocessing action.
    let splitSource (path: string) =

        ()

    [<RequireQualifiedAccess>]
    module ``grok`` =

        let name = "grok"


        type Parameters =
            { DataSource: string
              Table: TableModel
              GrokString: string
              ExtraPatterns: (string * string) list }

        let run (parameters: Parameters) (store: PipelineStore) =
            //store

            let patterns =
                store.GetResource("grok_patterns")
                |> Option.map (fun r ->
                    (String.FromUtfBytes r).Split(Environment.NewLine)
                    |> Array.choose Internal.parseGrokPattern
                    |> List.ofArray)
                |> Option.defaultValue []
                |> List.append parameters.ExtraPatterns
                |> Map.ofList

            let grok = Grok.create patterns parameters.GrokString

            store.GetDataSource parameters.DataSource
            |> Option.map (fun ds ->
                match ds.Type with
                | "file" ->
                    try
                        File.ReadAllLines ds.Uri |> Ok
                    with exn ->
                        Error $"Could not load file `{ds.Uri}`: {exn.Message}"
                | _ -> Error $"Unsupported source type: `{ds.Type}`")
            |> Option.defaultWith (fun _ -> Error $"Data source `{parameters.DataSource}` not found.")
            |> Result.map (Internal.createGrokRows parameters.Table.Columns grok)
            |> Result.bind (fun r ->
                match r.Errors |> List.isEmpty |> not with
                | true ->
                    store.LogWarning("grok", $"Error parsing {r.Errors.Length} row(s)")

                    r.Errors
                    |> List.map (fun e ->
                        store.AddImportError("grok", e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"))
                    |> ignore
                | false -> ()

                store.CreateTable(parameters.Table)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log("grok", $"Imported {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction parameters = run parameters |> createAction name
   
   
    module ``query-sqlite-database`` =
        
        let name = "query_sqlite-database"
        
        type Parameters =
            {
                Path: string
                Table: TableModel
                Sql: string
                Parameters: obj list
            }
        
        
        let run (parameters: Parameters) (store: PipelineStore) =
            use ctx = SqliteContext.Open parameters.Path
            
            
            // Select to table
            
            //parameters.Table.
            
            // Save table and rows to store
            
            
            store.CreateTable(parameters.Table)
            |> store.InsertRows
            |> Result.map (fun rs ->
                
                
                ())
            
            
            
            
            
            
        
        
        
        ()
        
        

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
