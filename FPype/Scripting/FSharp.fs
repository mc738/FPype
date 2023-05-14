namespace FPype.Scripting

[<RequireQualifiedAccess>]
module FSharp =

    open System
    open FPype.Data.Store
    open FPype.Scripting.Core
    
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
        
        
    
    
    type PipelineStoreProxy() =
        
        member _.a() = ()
    
    
    ()

