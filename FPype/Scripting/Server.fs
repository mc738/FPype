namespace FPype.Scripting

open System.IO
open FPype.Data
open FPype.Data.Store
open FPype.Scripting.Core
open FsToolbox.Core

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

    open System.IO.Pipes

    (*
    type ServerState =
        { TableIterators: Map<string, Iterators.Table> }

        static member Create() = { TableIterators = Map.empty }

        member ss.AddTableIterator(iterator: Iterators.Table) =
            { ss with
                TableIterators = ss.TableIterators.Add(iterator.Id, iterator) }

        member ss.GetTableIterator(id: string) = ss.TableIterators.TryFind id

        member ss.BreakTableIterator(id: string) =
            { ss with
                TableIterators = ss.TableIterators.Remove id }

        member ss.ProgressTableIterator(id: string) =
            match ss.TableIterators.TryFind id with
            | Some ti ->
                { ss with
                    TableIterators = ss.TableIterators.Add(ti.Id, ti.Next()) }
            | None -> ss
    *)

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
            match store.TryAddStateValue(request.Key, request.Value) with
            | Ok _ -> IPC.ResponseMessage.Acknowledge
            | Error e -> IPC.ResponseMessage.Error e
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
            match
                store.TryAddArtifact(
                    request.Name,
                    request.Bucket,
                    request.Type,
                    Conversions.fromBase64 request.Base64Data
                )
            with
            | Ok _ -> IPC.ResponseMessage.Acknowledge
            | Error e -> IPC.ResponseMessage.Error e
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
            match store.TryAddResource(request.Name, request.Type, Conversions.fromBase64 request.Base64Data) with
            | Ok _ -> IPC.ResponseMessage.Acknowledge
            | Error e -> IPC.ResponseMessage.Error e
        | IPC.RequestMessage.GetResource request ->
            store.GetResourceEntity(request.Name)
            |> function
                | Some r -> (Models.Resource.FromEntity r).Serialize()
                | None -> System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.AddCacheItem request ->
            store.AddCacheItem(request.Name, request.Base64Data |> Conversions.fromBase64, request.Ttl)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.GetCacheItem request ->
            store.GetCacheItemEntity(request.Name)
            |> function
                | Some ci -> (Models.CacheItem.FromEntity ci).Serialize()
                | None -> System.String.Empty
            |> IPC.ResponseMessage.String
        | IPC.RequestMessage.DeleteCacheItem request ->
            store.DeleteCacheItem(request.Name)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.AddResult request ->
            store.AddResult(request.Step, request.Result, request.StartUtc, request.EndUtc, request.Serial)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.AddImportError request ->
            store.AddImportError(request.Step, "script", request.Error, request.Value)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.AddVariable request ->
            store.AddVariable(request.Name, request.Value)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.SubstituteValues request ->
            store.SubstituteValues(request.Value) |> IPC.ResponseMessage.String
        | IPC.RequestMessage.CreateTable request ->
            store.CreateTable(request.Table) |> ignore
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.InsertRows request ->
            store.InsertRows(request.Table) |> ignore
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.SelectRows request ->
            store.BespokeSelectRows(request.Table, request.QuerySql, request.Parameters |> List.map (fun p -> p.Box()))
            |> fun t -> IPC.ResponseMessage.Rows t.Rows
        | IPC.RequestMessage.Log request ->
            store.Log(request.Step, "script", request.Message)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.LogError request ->
            store.LogError(request.Step, "script", request.Message)
            IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.LogWarning request ->
            store.LogWarning(request.Step, "script", request.Message)
            IPC.ResponseMessage.Acknowledge
        //| IPC.RequestMessage.IteratorNext -> failwith "todo"
        //| IPC.RequestMessage.IteratorBreak -> IPC.ResponseMessage.Acknowledge
        | IPC.RequestMessage.Close -> IPC.ResponseMessage.Close

    let start (store: PipelineStore) (pipeName: string) =
        let stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut)

        // NOTE - need a timeout?
        stream.WaitForConnection()

        let rec run ( (*state: ServerState*) ) =
            try
                match stream.IsConnected with
                | true ->

                    let cont =
                        readMessage stream
                        |> Result.bind IPC.RequestMessage.FromMessage
                        |> Result.bind (fun req ->
                            match req with
                            | IPC.RequestMessage.Close -> Ok false
                            | _ -> handleRequest store req |> sendResponse stream)

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

        run ( (*ServerState.Create()*) )
