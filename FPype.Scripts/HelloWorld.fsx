#r "../FPype/bin/Debug/net6.0/FPype.dll"

open FPype.Core.Types
open FPype.Data.Models
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

        let query =
            "SELECT DISTINCT(sp.gics_sub_industry) as name FROM sp500_prices sp WHERE sp.gics_sector = @0"

        let parameters = [ Value.String "Industrials" ]

        let table =
            ({ Name = "industries_test"
               Columns =
                 [ { Name = "name"
                     Type = BaseType.String
                     ImportHandler = None } ]
               Rows = [] }
            : TableModel)

        let rows = store.SelectRows(table, query, parameters) |> unwrap
        
        rows |> List.iter (fun r -> printfn $"{r}")
        
        let r = table.SetRows(rows) |> store.InsertRows |> unwrap
        
        ()

let execute (pipeName: string) =

    // Request parameters

    use store = new FSharp.PipelineStoreProxy(pipeName)



    HelloWorld.run store

    ()
