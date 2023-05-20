namespace FPype.Tests.Core.Models

open FPype.Core.Types
open FPype.Data.Models
open Microsoft.VisualStudio.TestTools.UnitTesting


[<TestClass>]
type TablesTests() =

    [<TestMethod>]
    member _.``Serialize and deserialize row``() =
        let row = ({ Values = [ Value.String "Hello, World!"; Value.Int 42 ] }: TableRow)
        
        let expected: Result<TableRow * byte array, string> = Ok (row, [||])
        
        let actual = row.Serialize() |> TableRow.TryDeserialize
        
        Assert.AreEqual(expected, actual)