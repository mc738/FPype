﻿namespace FPype.Scripting

open System.IO
open FPype.Data
open FPype.Data.Store
open FPype.Scripting.Core
open FsToolbox.Core
open Microsoft.VisualBasic

// Start server should
// 1. Create the message ctxs
// 2. start server (perferrable in back ground)
// 3. handle receiving requests and passing them to ctx
//
// How should this work?
// - blocking function that accepts store
// - internally it can response to requests
// - once script is complete it returns (like action)
// - all logic, data etc is internal
//
// Channels - not needed, all handled here?

[<RequireQualifiedAccess>]
module Server =

    open System.IO
    open System.IO.Pipes
    open FPype.Scripting.Core

    let readMessage (stream: NamedPipeServerStream) =
        try
            let headerBuffer: byte array = Array.zeroCreate 8

            stream.Read(headerBuffer) |> ignore

            IPC.Header.TryDeserialize headerBuffer
            |> Result.bind (fun h ->
                try
                    let buffer: byte array = Array.zeroCreate h.Length

                    stream.Read(buffer) |> ignore

                    IPC.Message.Create(h, buffer) |> Ok
                with ex ->
                    Error $"Unhandled except while reading message body: {ex.Message}")
        with ex ->
            Error $"Unhandled except while reading message header: {ex.Message}"

    let sendResponse (stream: NamedPipeServerStream) (response: IPC.ResponseMessage) =
        try
            stream.Write(response.Serialize())
            Ok true
        with ex ->
            Error $"Unhandled exception while writing response: {ex.Message}"

    let handleRequest (store: PipelineStore) (request: IPC.RequestMessage) =
        match request with
        | IPC.RequestMessage.RawMessage body -> failwith "todo"
        | IPC.RequestMessage.AddStateValue request ->
            store.AddStateValue(request.Key, request.Value)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.UpdateStateValue request ->
            store.UpdateStateValue(request.Key, request.Value) |> ignore
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.GetState ->
            store.GetState()
            |> Models.State.FromEntities
            |> fun r -> r.Serialize()
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetStateValue request ->
            store.GetStateValue(request.Key)
            |> Option.defaultValue System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.StateValueExists requestMessage ->
            store.StateValueExists(requestMessage.Key) |> IPC.ResponseMessage.Bool
        | IPC.RequestMessage.GetId -> store.Id |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetComputerName ->
            store.GetComputerName()
            |> Option.defaultValue System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetUserName ->
            store.GetUserName()
            |> Option.defaultValue System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetBasePath -> store.BasePath |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetImportsPath ->
            store.GetImportsPath()
            |> Option.defaultValue (Path.Combine(store.BasePath, StateNames.importsPath))
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetExportsPath ->
            store.GetExportsPath()
            |> Option.defaultValue (Path.Combine(store.BasePath, StateNames.exportsPath))
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetTmpPath ->
            store.GetTmpPath()
            |> Option.defaultValue (Path.Combine(store.BasePath, StateNames.tmpPath))
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetStorePath -> store.StorePath |> IPC.ResponseMessage.String
        | IPC.RequestMessage.AddDataSource request ->
            store.AddDataSource(
                request.Name,
                // NOTE should this default?
                DataSourceType.Deserialize request.DataSourceType
                |> Option.defaultValue DataSourceType.File,
                request.Uri,
                request.CollectionName
            )

            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.GetDataSource request ->
            store.GetDataSource(request.Name)
            |> function
                | Some ds -> (Models.DataSource.FromEntity ds).Serialize()
                | None -> System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.AddArtifact request ->
            // TODO what if error?
            store.AddArtifact(request.Name, request.Bucket, request.Type, Conversions.fromBase64 request.Base64Data)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.GetArtifact request ->
            store.GetArtifact(request.Name)
            |> function
                | Some ds -> (Models.Artifact.FromEntity ds).Serialize()
                | None -> System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.GetArtifactBucket request ->
            store.GetArtifactBucket request.Name
            |> fun ab -> (Models.ArtifactBucket.FromEntities ab).Serialize()
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.AddResource request ->
            // TODO what if error?
            store.AddResource(request.Name, request.Type, Conversions.fromBase64 request.Base64Data)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.GetResource request ->
            store.GetResourceEntity(request.Name)
            |> function
                | Some r -> (Models.Resource.FromEntity r).Serialize()
                | None -> System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.AddCacheItem request -> failwith "todo"
        | IPC.RequestMessage.GetCacheItem request -> failwith "todo"
        | IPC.RequestMessage.DeleteCacheItem request -> failwith "todo"
        | IPC.RequestMessage.AddResult request -> failwith "todo"
        | IPC.RequestMessage.AddImportError request -> failwith "todo"
        | IPC.RequestMessage.AddVariable request -> failwith "todo"
        | IPC.RequestMessage.SubstituteValues request -> failwith "todo"
        | IPC.RequestMessage.CreateTable -> failwith "todo"
        | IPC.RequestMessage.InsertRows -> failwith "todo"
        | IPC.RequestMessage.SelectRows -> failwith "todo"
        | IPC.RequestMessage.SelectBespokeRows -> failwith "todo"
        | IPC.RequestMessage.Log request -> failwith "todo"
        | IPC.RequestMessage.LogError request -> failwith "todo"
        | IPC.RequestMessage.LogWarning request -> failwith "todo"
        | IPC.RequestMessage.IteratorNext -> failwith "todo"
        | IPC.RequestMessage.IteratorBreak -> failwith "todo"
        | IPC.RequestMessage.Close -> IPC.ResponseMessage.Close



    let start (handler: IPC.RequestMessage -> IPC.ResponseMessage option) (pipeName: string) =
        let stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut)

        // NOTE - need a timeout?
        stream.WaitForConnection()

        let rec run () =
            try
                match stream.IsConnected with
                | true ->

                    let cont =
                        readMessage stream
                        |> Result.bind IPC.RequestMessage.FromMessage
                        |> Result.bind (fun req ->
                            match req with
                            | IPC.RequestMessage.Close -> Ok false
                            | _ ->
                                match handler req with
                                | Some res -> sendResponse stream res
                                | None -> Ok true)

                    match cont, stream.IsConnected with
                    | Ok true, true -> run ()
                    | Ok false, _
                    | _, false ->
                        printfn "Server complete. closing."
                        Ok()
                    | Error e, _ ->
                        // TODO handle error
                        Ok()
                | false ->
                    printfn "Server complete. closing."
                    Ok()
            with ex ->
                Error $"Server error - {ex}"

        run ()