﻿namespace FPype.Actions

module Utils =

    open System.IO
    open FPype.Data.Store
    
    module ``create-directory`` =

        let name = "create_directory"

        type Parameters = { Path: string; Name: string }

        let run (parameters: Parameters) (store: PipelineStore) =
            let fullPath = store.ExpandPath parameters.Path

            try
                match Directory.Exists fullPath with
                | true -> ()
                | false -> Directory.CreateDirectory fullPath |> ignore

                store.AddVariable(parameters.Name, fullPath)

                Ok store
            with ex ->
                Error $"Failed to create directory `{fullPath}` - {ex.Message}"
        
        let createAction parameters = run parameters |> createAction name
