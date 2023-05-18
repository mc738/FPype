#r "../FPype/bin/Debug/net6.0/FPype.dll"

open System.Reflection
open FPype.Scripting

module HelloWorld =

    let unwrap<'T> (result: Result<'T, string>) =
        match result with
        | Ok v -> v
        | Error e -> failwith e

    let run (store: FSharp.PipelineStoreProxy) =
        
        let id = store.GetId() |> unwrap
        let computerName = store.GetComputerName() |> unwrap
        let userName = store.GetUserName() |> unwrap

        printfn $"Id: {id}"
        printfn $"Computer name: {computerName}"
        printfn $"Username: {userName}"

        ()

let execute (pipeName: string) =

    // Request parameters
    
    use store = new FSharp.PipelineStoreProxy(pipeName)

    
    HelloWorld.run store

    ()
