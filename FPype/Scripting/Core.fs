namespace FPype.Scripting

open System
open System.IO.Pipes
open System.Text
open System.Threading.Channels
open FPype.Data.Store

module Core =

    // Notes
    //
    // Need to handle 2 types of response (?):
    // Fixed length (with a length value)
    // Streams (?) (with length value -1 - send data until 0uy reached)

    [<RequireQualifiedAccess>]
    module IPC =

        open System.IO
        open System.IO.Pipes

        let magicBytes = [ 14uy; 06uy ]

        type Header =
            { Length: int
              MessageTypeByte: byte
              MaskByte: byte }

            static member Create(length: int, messageTypeByte: byte, ?maskByte: byte) =
                { Length = length
                  MessageTypeByte = messageTypeByte
                  MaskByte = maskByte |> Option.defaultValue 0uy }

            static member TryDeserialize(bytes: byte array) =
                match bytes.Length >= 8 with
                | true ->
                    match [ bytes[0]; bytes[1] ] = magicBytes with
                    | true ->
                        { Length = BitConverter.ToInt32(bytes[4..])
                          MessageTypeByte = bytes[2]
                          MaskByte = bytes[3] }
                        |> Ok
                    | false -> Error "Magic bytes do not match."
                | false -> Error $"Header length ({bytes.Length}) is too short."

            member h.Serialize() =
                [| yield! magicBytes
                   h.MessageTypeByte
                   h.MaskByte
                   yield! BitConverter.GetBytes(h.Length) |]

        type Message =
            { Header: Header
              Body: byte array }

            static member Create(header: Header, body: byte array) = { Header = header; Body = body }

            static member Create(body: string, messageTypeByte: byte) =
                let b = Encoding.UTF8.GetBytes(body)

                { Header = Header.Create(b.Length, messageTypeByte)
                  Body = b }

            static member CreateEmpty(messageTypeByte: byte) =
                { Header = Header.Create(0, messageTypeByte)
                  Body = [||] }


            member m.Serialize() =
                [| yield! m.Header.Serialize(); yield! m.Body |]

            member m.BodyAsUtf8String() = Encoding.UTF8.GetString m.Body

        [<RequireQualifiedAccess>]
        type RequestMessage =
            | RawMessage of Body: string
            | Close

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> RequestMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> RequestMessage.RawMessage |> Ok
                | _ -> Error $"Unknown message type ({message})"

            member rm.GetMessageTypeByte() =
                match rm with
                | RawMessage _ -> 1uy
                | Close -> 0uy

            member rm.ToMessage() =
                match rm with
                | RawMessage body -> Message.Create(body, rm.GetMessageTypeByte())
                | Close -> Message.CreateEmpty(rm.GetMessageTypeByte())

            member rm.Serialize() = rm.ToMessage().Serialize()


        [<RequireQualifiedAccess>]
        type ResponseMessage =
            | RawMessage of Body: string
            | Close

            static member FromMessage(message: Message) =
                match message.Header.MessageTypeByte with
                | 0uy -> ResponseMessage.Close |> Ok
                | 1uy -> message.Body |> Encoding.UTF8.GetString |> ResponseMessage.RawMessage |> Ok
                | _ -> Error $"Unknown message type ({message})"

            member rm.GetMessageTypeByte() =
                match rm with
                | RawMessage _ -> 1uy
                | Close -> 0uy

            member rm.ToMessage() =
                match rm with
                | RawMessage body -> Message.Create(body, rm.GetMessageTypeByte())
                | Close -> Message.CreateEmpty(rm.GetMessageTypeByte())

            member rm.Serialize() = rm.ToMessage().Serialize()

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

    [<RequireQualifiedAccess>]
    module Client =

        open System.IO
        open System.IO.Pipes

        type Context = { Stream: NamedPipeClientStream }

        let readResponse (ctx: Context) =

            let headerBuffer: byte array = Array.zeroCreate 8

            ctx.Stream.Read(headerBuffer) |> ignore

            match IPC.Header.TryDeserialize headerBuffer with
            | Ok h ->

                let buffer: byte array = Array.zeroCreate h.Length

                ctx.Stream.Read(buffer) |> ignore

                let message = IPC.Message.Create(h, buffer)
                

                IPC.ResponseMessage.FromMessage message
            
            | Error e -> Error $"Error reading response: {e}"

        let start (pipeName: string) =
            let client = new NamedPipeClientStream(pipeName)
            client.Connect()

            { Stream = client }

        let close (ctx: Context) =
            if ctx.Stream.IsConnected then
                ctx.Stream.Write(IPC.RequestMessage.Close.Serialize())

                ctx.Stream.Close()

        let sendRequest (ctx: Context) (request: IPC.RequestMessage) =
            ctx.Stream.Write(request.Serialize())
            readResponse ctx


    /// <summary>
    /// The script context.
    /// To be used in scripts as a way to request data and run actions on a Pipeline store.
    /// </summary>
    type ScriptContext(pipeName) =
        let clientCtx = Client.start pipeName

        interface IDisposable with

            member sc.Dispose() =
                printfn "closing client..."
                // On dispose (i.e. when script has ended) send close message and close the stream.
                Client.close clientCtx

        member _.SendRequest(request: IPC.RequestMessage) = Client.sendRequest clientCtx request


    let executeScript (store: PipelineStore) () =

        // Start pipe server

        // Run script (on background thread?)

        // On main handle requests etc

        // Once script is complete a close signal should be sent

        // Main thread handles close.


        ()
