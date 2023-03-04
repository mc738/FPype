
open FPype
open FPype.Configuration

module Example =
    
    let import _ =
        
        let cfg = ConfigurationStore.Initialize "C:\\ProjectData\\Fpype\\fpype.config"
        
        let r = cfg.ImportFromFile "C:\\ProjectData\\Fpype\\example_1\\config_v1.json"
                
        ()
        
    let run _ =
        let cfg = ConfigurationStore.Load "C:\\ProjectData\\Fpype\\fpype.config"
        
        match PipelineContext.Create(cfg, "C:\\ProjectData\\Fpype\\runs", true, "test_pipeline", ItemVersion.Specific 1, Map.empty) with
        | Ok ctx ->
            let r = ctx.Run()
            
            ()
        | Error e ->
            
            printfn $"Error: {e}"
        
        
        
Example.import ()
Example.run ()

// For more information see https://aka.ms/fsharp-console-apps
printfn "Hello from F#"