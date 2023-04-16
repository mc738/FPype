namespace FPype.Scripting

open System
open System.Text
open System.Threading.Channels

module Core =

    [<RequireQualifiedAccess>]
    module IPC =

        open System.IO
        open System.IO.Pipes

        let magicBytes = [ 14uy; 06uy ]

        type Header =
            { Length: int
              Byte1: byte
              Byte2: byte }

            static member Create(length: int, ?byte1: byte, ?byte2: byte) =
                { Length = length
                  Byte1 = byte1 |> Option.defaultValue 0uy
                  Byte2 = byte2 |> Option.defaultValue 0uy }

            static member TryDeserialize(bytes: byte array) =
                match bytes.Length >= 8 with
                | true ->
                    match [ bytes[0]; bytes[1] ] = magicBytes with
                    | true ->
                        { Length = BitConverter.ToInt32(bytes[4..])
                          Byte1 = bytes[2]
                          Byte2 = bytes[3] }
                        |> Ok
                    | false -> Error "Magic bytes do not match."
                | false -> Error $"Header length ({bytes.Length}) is too short."

            member h.Serialize() =
                [| yield! magicBytes
                   h.Byte1
                   h.Byte2
                   yield! BitConverter.GetBytes(h.Length) |]

        type Message =
            { Header: Header
              Body: byte array }

            static member Create(header: Header, body: byte array) = { Header = header; Body = body }

            static member Create(body: string) =
                let b = Encoding.UTF8.GetBytes(body)

                { Header = Header.Create(b.Length)
                  Body = b }

            member m.Serialize() =
                [| yield! m.Header.Serialize(); yield! m.Body |]

            member m.BodyAsUtf8String() = Encoding.UTF8.GetString m.Body

        [<RequireQualifiedAccess>]
        type RequestMessage =
            | RawMessage of Message
            | Close

        [<RequireQualifiedAccess>]
        type ResponseMessage =
            | RawMessage of Message
            | Closed

        type MessageContext =
            { RequestChannel: ChannelReader<RequestMessage>
              ResponseChannel: ChannelWriter<ResponseMessage> }
            
            member mc.GetRequest() =
                let rec handler () =
                    match mc.RequestChannel.TryRead() with
                    | true, m -> Ok m
                    | false, _ ->
                        Async.Sleep 100 |> Async.RunSynchronously
                        handler ()

                handler ()

            member mc.SendResponse(response: ResponseMessage) =
                match mc.ResponseChannel.TryWrite(response) with
                | true -> Ok ()
                | false -> Error "Could not send response"
        
        type ServerMessageContext =
            { RequestChannel: ChannelWriter<RequestMessage>
              ResponseChannel: ChannelReader<ResponseMessage> }
            
            member smc.SendRequest(request: RequestMessage) =
                match smc.RequestChannel.TryWrite(request) with
                | true -> Ok ()
                | false -> Error "Could not send response"
        
            member smc.GetResponse() =
                let rec handler () =
                    match smc.ResponseChannel.TryRead() with
                    | true, m -> Ok m
                    | false, _ ->
                        Async.Sleep 100 |> Async.RunSynchronously
                        handler ()

                handler ()
            
        let tryReadMessage () = ()


        let startServer (pipeName: string) =
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
            
            let stream = new NamedPipeServerStream(pipeName, PipeDirection.InOut)

            // NOTE - need a timeout?

            stream.WaitForConnection()

            let rec run () =
                try
                    match stream.IsConnected with
                    | true ->
                        let headerBuffer: byte array = Array.zeroCreate 8

                        stream.Read(headerBuffer) |> ignore

                        match Header.TryDeserialize headerBuffer with
                        | Ok h ->
                            printfn "Header read!"
                            printfn $"{h}"

                            let buffer: byte array = Array.zeroCreate h.Length

                            stream.Read(buffer) |> ignore



                            let message = Message.Create(h, buffer)

                            printfn $"Message: {message.BodyAsUtf8String()}"

                            // Simulate writing something back.

                            Async.Sleep 1000 |> Async.RunSynchronously

                            stream.Write(Message.Create($"Thank you client. {DateTime.UtcNow}").Serialize())

                        | Error e -> printfn $"Error reading header: {e}"

                        match stream.IsConnected with
                        | true -> run ()
                        | false ->
                            printfn "Server complete. closing."
                            Ok()
                    | false ->
                        printfn "Server complete. closing."
                        Ok()
                with ex ->
                    Error $"Server error - {ex}"

            run ()
