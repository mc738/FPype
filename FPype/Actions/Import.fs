﻿namespace FPype.Actions

open FPype.Data

[<RequireQualifiedAccess>]
module Import =

    open System.IO
    open System.Net.Http
    open FsToolbox.Extensions
    open FPype.Data.Store

    [<RequireQualifiedAccess>]
    module ``import-file`` =
        let name = "import_file"

        type Parameters = { Path: string; Name: string }

        let run (parameters: Parameters) (store: PipelineStore) =
            let fullPath = store.SubstituteValues parameters.Path

            match File.Exists fullPath, store.GetImportsPath() with
            | true, Some importsPath ->
                //store.GetState()
                //let fi = FileInfo(path)
                let fileName = Path.GetFileName(fullPath)
                let newPath = Path.Combine(importsPath, fileName)
                File.Copy(fullPath, newPath)
                store.AddDataSource(parameters.Name, DataSourceType.File, newPath, "imports")
                Ok store
            | false, _ -> Error $"File `{fullPath}` not found."
            | _, None -> Error "Imports path not found in store state."

        let createAction stepName parameters =
            run parameters |> createAction name stepName

    module ``chunk-file`` =
        let name = "chunk_file"

        type Parameters =
            { Path: string
              CollectionName: string
              ChunkSize: int }

        let run (parameters: Parameters) (store: PipelineStore) =
            match File.Exists parameters.Path, store.GetImportsPath() with
            | true, Some importsPath ->
                let fileName = Path.GetFileNameWithoutExtension(parameters.Path)
                let ext = Path.GetExtension(parameters.Path)

                File.ReadLines(parameters.Path)
                |> Seq.chunkBySize parameters.ChunkSize
                |> Seq.iteri (fun i ls ->
                    let path = Path.Combine(importsPath, $"{fileName}___{i}{ext}")

                    File.WriteAllLines(path, ls)

                    store.AddDataSource($"{fileName}___{i}", DataSourceType.File, path, parameters.CollectionName))

                Ok store
            | false, _ -> Error $"File `{parameters.Path}` not found."
            | _, None -> Error "Imports path not found in store state."

        let createAction stepName parameters =
            run parameters |> createAction name stepName

    module ``unzip-files`` =

        let name = "unzip_files"


        let run () (store: PipelineStore) =

            //FsToolbox.Core.Compression.u

            ()


        ()

    module ``http-get`` =

        let name = "http_get"

        type Parameters =
            { Url: string
              AdditionalHeaders: Map<string, string>
              Name: string
              ResponseType: string option
              CollectionName: string option }

        let run (parameters: Parameters) (store: PipelineStore) =
            use client = new HttpClient()

            parameters.AdditionalHeaders
            |> Map.iter (fun k v -> client.DefaultRequestHeaders.Add(k, v))

            let handler _ =
                task {
                    let! r = client.GetAsync(parameters.Url)

                    let! body = r.Content.ReadAsStringAsync()

                    match r.IsSuccessStatusCode with
                    | true -> return Ok body
                    | false -> return Error body
                }

            handler ()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Result.map (fun r ->
                store.AddArtifact(
                    parameters.Name,
                    "imports",
                    parameters.ResponseType |> Option.defaultValue "txt",
                    r.ToUtf8Bytes()
                )

                let collection = parameters.CollectionName |> Option.defaultValue "imports"

                store.AddDataSource(
                    parameters.Name,
                    DataSourceType.Artifact,
                    $"{collection}/{parameters.Name}",
                    collection
                )

                store)

        let createAction stepName parameters =
            run parameters |> createAction name stepName
