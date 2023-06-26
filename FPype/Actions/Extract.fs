namespace FPype.Actions

open FPype.Core.Types
open FPype.Data.Models
open FPype.ML

[<RequireQualifiedAccess>]
module Extract =

    open System
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
    open FPype.Connectors

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
                            | Error(m, l, c) -> s, f @ [ m, l, c ])
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
                        | Error(m, l, c) -> s, (m, l, c) :: f)
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

        let createXlsxRows (columns: TableColumn list) (rows: DocumentFormat.OpenXml.Spreadsheet.Row seq) =
            rows
            |> List.ofSeq
            |> List.mapi (fun rowI row ->
                columns
                |> List.mapi (fun tci tc ->
                    // TODO get column name...
                    match Freql.Xlsx.Common.getCellFromRow row "" with
                    | Some c ->
                        let rec handle (bt: BaseType) =
                            match bt with
                            | BaseType.Boolean ->
                                match Freql.Xlsx.Common.cellToBool c with
                                | Some v -> Value.Boolean v |> Ok
                                | None -> Error("Value could not be extracted as type bool", rowI, tci)
                            | BaseType.Byte ->
                                match Freql.Xlsx.Common.cellToInt c |> Option.map byte with
                                | Some v -> Value.Byte v |> Ok
                                | None -> Error "Value could not be extracted as type byte"
                            | BaseType.Char ->
                                match Freql.Xlsx.Common.cellToString c |> Seq.tryItem 0 with
                                | Some v -> Value.Char v |> Ok
                                | None -> Error "Value could not be extracted as type char"
                            | BaseType.Decimal ->
                                match Freql.Xlsx.Common.cellToDecimal c with
                                | Some v -> Value.Decimal v |> Ok
                                | None -> Error "Value could not be extracted as type decimal"
                            | BaseType.Double ->
                                match Freql.Xlsx.Common.cellToDouble c with
                                | Some v -> Value.Double v |> Ok
                                | None -> Error "Value could not be extracted as type double"
                            | BaseType.Float ->
                                match Freql.Xlsx.Common.cellToDouble c |> Option.map float32 with
                                | Some v -> Value.Float v |> Ok
                                | None -> Error "Value could not be extracted as type float"
                            | BaseType.Int ->
                                match Freql.Xlsx.Common.cellToInt c with
                                | Some v -> Value.Int v |> Ok
                                | None -> Error "Value could not be extracted as type int"
                            | BaseType.Short ->
                                match Freql.Xlsx.Common.cellToInt c |> Option.map int16 with
                                | Some v -> Value.Short v |> Ok
                                | None -> Error "Value could not be extracted as type short"
                            | BaseType.Long ->
                                match Freql.Xlsx.Common.cellToInt c |> Option.map int64 with
                                | Some v -> Value.Long v |> Ok
                                | None -> Error "Value could not be extracted as type long"
                            | BaseType.String -> Freql.Xlsx.Common.cellToString c |> Value.String |> Ok
                            | BaseType.DateTime ->
                                match Freql.Xlsx.Common.cellToOADateTime c with
                                | Some v -> Value.DateTime v |> Ok
                                | None -> Error "Value could not be extracted as type datetime"
                            | BaseType.Guid ->
                                match Guid.TryParse(Freql.Xlsx.Common.cellToString c) with
                                | true, v -> Value.Guid v |> Ok
                                | false, _ -> Error "Value could not be extracted as type guid"
                            | BaseType.Option ibt -> handle ibt |> Result.map (Some >> Value.Option)

                        handle tc.Type
                    | None ->
                        match tc.Type with
                        | BaseType.Option _ -> Ok <| Value.Option None
                        | _ -> Error "")
                |> List.fold
                    (fun (s, f) r ->
                        match r with
                        | Ok fv -> s @ [ fv ], f
                        | Error(message, rowIndex, field) -> s, f @ [ message, rowIndex, field ])
                    ([], [])
                |> fun (s, f) ->
                    match f.Length = 0 with
                    | true -> { Values = s } |> ParseResult.Success
                    | false ->
                        let (m, ln, i) = f.Head

                        { Message = m
                          LineNumber = ln
                          ColumnNumber = i
                          Line = "" }
                        |> ParseResult.Failure)

    [<RequireQualifiedAccess>]
    module ``parse-csv`` =
        let name = "parse_csv"

        type Parameters =
            { DataSource: string
              Table: TableModel }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            getDataSourceAsLinesByName store parameters.DataSource
            |> Result.map (Internal.createRows parameters.Table.Columns)
            |> Result.bind (fun r ->
                match r.Errors |> List.isEmpty |> not with
                | true ->
                    store.LogWarning(stepName, name, $"Error parsing {r.Errors.Length} row(s)")

                    r.Errors
                    |> List.map (fun e ->
                        store.AddImportError(stepName, name, e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"))
                    |> ignore
                | false -> ()

                store.CreateTable(parameters.Table)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log(stepName, name, $"Imported {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    [<RequireQualifiedAccess>]
    module ``parse-csv-collection`` =
        let name = "parse_csv_collection"

        type Parameters =
            { CollectionName: string
              Table: TableModel }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            store.GetSourcesByCollection parameters.CollectionName
            |> List.map (fun ds ->
                getDataSourceAsLines store ds
                |> Result.map (Internal.createRows parameters.Table.Columns)
                |> Result.bind (fun r ->
                    match r.Errors |> List.isEmpty |> not with
                    | true ->
                        store.LogWarning(
                            stepName,
                            name,
                            $"Error parsing {r.Errors.Length} row(s) from source `{ds.Name}`"
                        )

                        r.Errors
                        |> List.map (fun e ->
                            store.AddImportError(
                                stepName,
                                name,
                                e.Message,
                                $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"
                            ))
                        |> ignore
                    | false -> ()

                    store.CreateTable(parameters.Table)
                    |> fun t -> { t with Rows = r.Rows }
                    |> store.InsertRows)
                |> Result.map (fun r ->
                    store.Log(
                        stepName,
                        name,
                        $"Imported {r.Length} row(s) from data source `{ds.Name}` to table `{parameters.Table.Name}`."
                    )
                // TODO add result?
                ))
            |> flattenResultList
            |> Result.map (fun _ -> store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

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

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
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
                    store.LogWarning(stepName, name, $"Error parsing {r.Errors.Length} row(s)")

                    r.Errors
                    |> List.map (fun e ->
                        store.AddImportError(stepName, name, e.Message, $"{e.LineNumber}:{e.ColumnNumber} - {e.Line}"))
                    |> ignore
                | false -> ()

                store.CreateTable(parameters.Table)
                |> fun t -> { t with Rows = r.Rows }
                |> store.InsertRows)
            |> Result.map (fun r ->
                store.Log(stepName, name, $"Imported {r.Length} row(s) to table `{parameters.Table.Name}`.")
                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

    module ``query-sqlite-database`` =

        let name = "query_sqlite_database"

        type Parameters =
            { Path: string
              Table: TableModel
              Sql: string
              Parameters: obj list }

        let run (parameters: Parameters) (stepName: string) (store: PipelineStore) =
            let fullPath = store.SubstituteValues parameters.Path

            // Select rows from external database
            Sqlite.selectBespoke fullPath parameters.Table parameters.Sql parameters.Parameters
            // Set the rows in the model...
            |> parameters.Table.SetRows
            // and insert into the store.
            |> store.InsertRows
            |> Result.map (fun rs ->
                store.Log(stepName, name, $"{rs.Length} rows inserted from {fullPath}.")

                store)

        let createAction stepName parameters =
            run parameters stepName |> createAction name stepName

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

    module ``extract-from-xlsx`` =

        [<AutoOpen>]
        module private Internal =

            type ErrorMessage = { Message: string; LineNumber: int }

            type ParseResult =
                | Success of TableRow
                | Warning of TableRow
                | Failure of ErrorMessage

            type ParseResults =
                { Rows: TableRow list
                  Errors: ErrorMessage list }

            type MappedTableColumn =
                { Column: TableColumn
                  ColumnName: string }
            
            let createXlsxRows (columns: MappedTableColumn list) (rows: DocumentFormat.OpenXml.Spreadsheet.Row seq) =
                rows
                |> List.ofSeq
                |> List.mapi (fun rowI row ->
                    columns
                    |> List.map (fun tc ->
                        // TODO get column name...
                        let columnName = ""

                        match Freql.Xlsx.Common.getCellFromRow row tc.ColumnName with
                        | Some c ->
                            let rec handle (bt: BaseType) =
                                match bt with
                                | BaseType.Boolean ->
                                    match Freql.Xlsx.Common.cellToBool c with
                                    | Some v -> Value.Boolean v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type bool", rowI, tc.Column.Name, columnName)
                                | BaseType.Byte ->
                                    match Freql.Xlsx.Common.cellToInt c |> Option.map byte with
                                    | Some v -> Value.Byte v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type byte", rowI, tc.Column.Name, columnName)
                                | BaseType.Char ->
                                    match Freql.Xlsx.Common.cellToString c |> Seq.tryItem 0 with
                                    | Some v -> Value.Char v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type char", rowI, tc.Column.Name, columnName)
                                | BaseType.Decimal ->
                                    match Freql.Xlsx.Common.cellToDecimal c with
                                    | Some v -> Value.Decimal v |> Ok
                                    | None ->
                                        Error(
                                            "Value could not be extracted as type decimal",
                                            rowI,
                                            tc.Column.Name,
                                            columnName
                                        )
                                | BaseType.Double ->
                                    match Freql.Xlsx.Common.cellToDouble c with
                                    | Some v -> Value.Double v |> Ok
                                    | None ->
                                        Error(
                                            "Value could not be extracted as type double",
                                            rowI,
                                            tc.Column.Name,
                                            columnName
                                        )
                                | BaseType.Float ->
                                    match Freql.Xlsx.Common.cellToDouble c |> Option.map float32 with
                                    | Some v -> Value.Float v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type float", rowI, tc.Column.Name, columnName)
                                | BaseType.Int ->
                                    match Freql.Xlsx.Common.cellToInt c with
                                    | Some v -> Value.Int v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type int", rowI, tc.Column.Name, columnName)
                                | BaseType.Short ->
                                    match Freql.Xlsx.Common.cellToInt c |> Option.map int16 with
                                    | Some v -> Value.Short v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type short", rowI, tc.Column.Name, columnName)
                                | BaseType.Long ->
                                    match Freql.Xlsx.Common.cellToInt c |> Option.map int64 with
                                    | Some v -> Value.Long v |> Ok
                                    | None ->
                                        Error("Value could not be extracted as type long", rowI, tc.Column.Name, columnName)
                                | BaseType.String -> Freql.Xlsx.Common.cellToString c |> Value.String |> Ok
                                | BaseType.DateTime ->
                                    match Freql.Xlsx.Common.cellToOADateTime c with
                                    | Some v -> Value.DateTime v |> Ok
                                    | None ->
                                        Error(
                                            "Value could not be extracted as type datetime",
                                            rowI,
                                            tc.Column.Name,
                                            columnName
                                        )
                                | BaseType.Guid ->
                                    match Guid.TryParse(Freql.Xlsx.Common.cellToString c) with
                                    | true, v -> Value.Guid v |> Ok
                                    | false, _ ->
                                        Error("Value could not be extracted as type guid", rowI, tc.Column.Name, columnName)
                                | BaseType.Option ibt -> handle ibt |> Result.map (Some >> Value.Option)

                            handle tc.Column.Type
                        | None ->
                            match tc.Column.Type with
                            | BaseType.Option _ -> Ok <| Value.Option None
                            | _ -> Error("Value could not be extracted as type guid", rowI, tc.Column.Name, columnName))
                    |> List.fold
                        (fun (s, f) r ->
                            match r with
                            | Ok fv -> s @ [ fv ], f
                            | Error(message, rowIndex, field, column) -> s, f @ [ message, rowIndex, field, column ])
                        ([], [])
                    |> fun (s, f) ->
                        match f.Length = 0 with
                        | true -> { Values = s } |> ParseResult.Success
                        | false ->

                            let (_, ln, _, _) = f.Head

                            { Message =
                                f
                                |> List.map (fun (m, ln, fn, cn) -> $"{m} [field: {fn} column: {cn}]")
                                |> String.concat "; "
                              LineNumber = ln }
                            |> ParseResult.Failure)

        ()
