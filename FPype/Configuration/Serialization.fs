namespace FPype.Configuration

open System.Text.Json

module Serialization =
    
    let import (itemType: string) (data: JsonElement) =
        match itemType.ToLower() with
        | "object_mapper" -> ()
        | "pipeline_action" -> ()
        | "pipeline_arg" -> ()
        | "pipeline" -> ()
        | "query" -> ()
        | "table_column" -> ()
        | "table_model" -> ()
        | "resource" -> ()
        | _ -> ()
    
    
    
    ()

