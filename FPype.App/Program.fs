open FPype
open FPype.Configuration
open FPype.Data

module Maths =
    
    let t _ =
        
        let values = [ 1m; 2m; 3m; 3m; 9m; 10m  ]
        
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
            |> Result.bind (fun _ -> cfg.AddResourceFile(IdType.Generated, "grok_patterns", "text", "C:\\ProjectData\\Fgrok\\patterns.txt", ItemVersion.Specific 1))
            
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


Maths.t ()

//Example.import ()
//Example.run ()

// ServerReport.import ()
ServerReport.run ()
// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"
