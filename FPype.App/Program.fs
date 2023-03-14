open System.IO
open System.Text.Json
open FPype
open FPype.Configuration
open FPype.Core
open FPype.Core.Expressions.Parsing
open FPype.Core.JPath
open FPype.Core.Paths
open FPype.Core.Types
open FPype.Data
open Microsoft.FSharp.Core

module Maths =

    let t _ =

        let values = [ 1m; 2m; 3m; 3m; 9m; 10m ]

        let r = Statistics.standardDeviation values

        ()

module Example =

    let import _ =

        let cfg = ConfigurationStore.Initialize "C:\\ProjectData\\Fpype\\fpype.config"

        let r = cfg.ImportFromFile "C:\\ProjectData\\Fpype\\example_1\\config_v1.json"

        ()

    let run _ =
        let cfg = ConfigurationStore.Load "C:\\ProjectData\\Fpype\\fpype.config"

        match
            PipelineContext.Create(
                cfg,
                "C:\\ProjectData\\Fpype\\runs",
                true,
                "test_pipeline",
                ItemVersion.Specific 1,
                Map.empty
            )
        with
        | Ok ctx ->
            let r = ctx.Run()

            ()
        | Error e ->

            printfn $"Error: {e}"

module ServerReport =

    let import _ =

        let cfg = ConfigurationStore.Initialize "C:\\ProjectData\\Fpype\\fpype2.config"

        let r =
            cfg.ImportFromFile "C:\\ProjectData\\Fpype\\server_report\\config_v1.json"
            |> Result.bind (fun _ ->
                cfg.AddResourceFile(
                    IdType.Generated,
                    "grok_patterns",
                    "text",
                    "C:\\ProjectData\\Fgrok\\patterns.txt",
                    ItemVersion.Specific 1
                ))

        ()

    let run _ =
        let cfg = ConfigurationStore.Load "C:\\ProjectData\\Fpype\\fpype2.config"

        match
            PipelineContext.Create(
                cfg,
                "C:\\ProjectData\\Fpype\\runs\\server_report\\v1",
                true,
                "server_report",
                ItemVersion.Specific 1,
                Map.empty
            )
        with
        | Ok ctx ->
            let r = ctx.Run()

            ()
        | Error e ->

            printfn $"Error: {e}"


module PathTest =



    // To create table columns
    // Start at top level and get values
    // Move down a level and fetch more
    // Continue until lowest level
    // Build rows
    
    
    let unwrap (r: Result<'a, 'b>) = match r with | Ok v -> v | Error _ -> failwith "Error"

    let run () =
        
        let expr =
            match Expressions.Parsing.parse "@.price<10 && @.i == 100 && @.i <= 90 && @.i >= 10" with
            | ExpressionStatementParseResult.Success r -> FilterExpression.FromToken r
            | _ -> failwith "Error"
        
        let expr2 = Expressions.Parsing.parse "@.price =~ '^s$'"
        
        //let p = JPath.Compile("$.store.books[?(@.price<10)].face")
        //let p2 = JPath.Compile("$.store.books.face")
        //let p3 = JPath.Compile("$.store.books[?(@.price<10)]")
        //let p4 = JPath.Compile("$.store.books.f")
        //let p5 = JPath.Compile("$.store.f.book")
        //let p6 = JPath.Compile("$.store.f[?(@.price<10)].book")
        
        let json =
            (File.ReadAllText "C:\\ProjectData\\Fpype\\example_data\\example.json"
             |> JsonDocument.Parse)
                .RootElement

        let topLevel = JPath.Compile("$.id") |> Result.map (fun jp -> jp.Run(json)) |> unwrap
        
        let path =  JPath.Compile("$.items[?(@.type =~ '^type1$')]") |> unwrap
        
        let path =  JPath.Compile("$.items[?(@.type =~ '^type1$')].subId") |> unwrap
        
        let p2 = JPath.Compile("$.id[0]") |> unwrap
        
        let itemsSelector = path.Run(json)
        let itemsSelector2 = p2.Run(json)
        
        let here = ()
        
        let r =
            itemsSelector |> List.map (fun el ->
                let sl1 = JPath.Compile("$.type") |> Result.map (fun jp -> jp.Run(el)) |> unwrap
                let sl2 = JPath.Compile("$.subId") |> Result.map (fun jp -> jp.Run(el)) |> unwrap
                let tls = JPath.Compile("$.values") |> Result.map (fun jp -> jp.Run(el)) |> unwrap
                
                tls
                |> List.map (fun el2 ->
                    let tl1 = JPath.Compile("$.name") |> Result.map (fun jp -> jp.Run(el2)) |> unwrap
                    let tl2 = JPath.Compile("$.value") |> Result.map (fun jp -> jp.Run(el2)) |> unwrap
                    
                    // Zip the bottom level elements to create all rows
                    
                    let r =
                        [
                            topLevel |> List.tryHead
                            sl1 |> List.tryHead
                            sl2 |> List.tryHead
                            tl1 |> List.tryHead
                            tl2 |> List.tryHead
                        ]
                    
                    
                    ()))
        
        
        let secondLevel1 = JPath.Compile("$.items.type") |> Result.map (fun jp -> jp.Run(json)) |> unwrap
        let secondLevel2 = JPath.Compile("$.items.subId") |> Result.map (fun jp -> jp.Run(json)) |> unwrap
        
        
        let thirdLevel1 = JPath.Compile("$.items.values.name") |> Result.map (fun jp -> jp.Run(json)) |> unwrap
        let thirdLevel2 = JPath.Compile("$.items.values.value") |> Result.map (fun jp -> jp.Run(json)) |> unwrap
        
        
        let name = topLevel |> List.tryHead |> Option.map (fun v -> Value.FromJsonValue(v, BaseType.String)) 
        
        
        let p1 = JPath.Compile("$.items") |> Result.map (fun jp -> jp.Run(json))
        

        ()

PathTest.run ()
Maths.t ()

//Example.import ()
//Example.run ()

// ServerReport.import ()
ServerReport.run ()
// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
