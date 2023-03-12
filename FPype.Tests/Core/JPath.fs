namespace FPype.Tests.Core

open System.Text.Json
open FPype.Core.JPath
open FPype.Core.Paths
open Microsoft.VisualStudio.TestTools.UnitTesting

module private JPathResources =
    
    let rawJPathTestJson = """
{
    "store": {
        "book": [
            {
                "category": "reference",
                "author": "Nigel Rees",
                "title": "Sayings of the Century",
                "price": 8.95
            },
            {
                "category": "fiction",
                "author": "Evelyn Waugh",
                "title": "Sword of Honour",
                "price": 12.99
            },
            {
                "category": "fiction",
                "author": "Herman Melville",
                "title": "Moby Dick",
                "isbn": "0-553-21311-3",
                "price": 8.99
            },
            {
                "category": "fiction",
                "author": "J. R. R. Tolkien",
                "title": "The Lord of the Rings",
                "isbn": "0-395-19395-8",
                "price": 22.99
            }
        ],
        "bicycle": {
            "color": "red",
            "price": 19.95
        }
    },
    "expensive": 10
}"""

    let jpathJson = rawJPathTestJson.Trim()

[<TestClass>]
type JPathTests() =
    
    [<TestMethod>]
    member this.``Get all book prices`` () =
        let path = "$.store.book.price"
        let root = JsonDocument.Parse JPathResources.jpathJson
        let expected = [ 8.95m; 12.99m; 8.99m; 22.99m ]
        match JPath.Compile(path) with
        | Ok p ->
            let results = p.Run(root.RootElement)
            let actual = results |> List.map (fun r -> r.GetDecimal())
            Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail($"Could not compile path. Error: '{e}'")
        
    [<TestMethod>]
    member this.``Get array index book prices`` () =
        let path = "$.store.book[1].price"
        let root = JsonDocument.Parse JPathResources.jpathJson
        let expected = [ 12.99m ]
        match JPath.Compile(path) with
        | Ok p ->
            let results = p.Run(root.RootElement)
            let actual = results |> List.map (fun r -> r.GetDecimal())
            Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail($"Could not compile path. Error: '{e}'")
        
    [<TestMethod>]
    member this.``Get array sliced book prices`` () =
        let path = "$.store.book[1:3].price"
        let root = JsonDocument.Parse JPathResources.jpathJson
        let expected = [ 12.99m; 8.99m; 22.99m ]
        match JPath.Compile(path) with
        | Ok p ->
            let results = p.Run(root.RootElement)
            let actual = results |> List.map (fun r -> r.GetDecimal())
            Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail($"Could not compile path. Error: '{e}'")
        
    [<TestMethod>]
    member this.``Get array indexes book prices`` () =
        let path = "$.store.book[1,3].price"
        let root = JsonDocument.Parse JPathResources.jpathJson
        let expected = [ 12.99m; 22.99m ]
        match JPath.Compile(path) with
        | Ok p ->
            let results = p.Run(root.RootElement)
            let actual = results |> List.map (fun r -> r.GetDecimal())
            Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail($"Could not compile path. Error: '{e}'")

