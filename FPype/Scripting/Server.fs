namespace FPype.Scripting

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
