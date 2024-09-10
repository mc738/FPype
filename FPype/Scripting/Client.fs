namespace FPype.Scripting

[<RequireQualifiedAccess>]
module Client =

    open System.IO.Pipes
    open FPype.Scripting.Core

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
