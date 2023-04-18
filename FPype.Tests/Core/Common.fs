namespace FPype.Tests.Core

open System
open FPype.Core.Types
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type ValueTests() =
    
    [<TestMethod>]
    member _.``Serialize and deserialize bool``() =
        
        let actual = (Value.Boolean true).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Boolean true, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    
    [<TestMethod>]
    member _.``Serialize and deserialize byte``() =
        
        let actual = (Value.Byte 1uy).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Byte 1uy, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    [<TestMethod>]
    member _.``Serialize and deserialize char``() =
        
        let actual = (Value.Char 'a').Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Char 'a', Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    //[<TestMethod>]
    member _.``Serialize and deserialize decimal``() =
        
        let actual = (Value.Decimal 100m).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Decimal 100m, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    [<TestMethod>]
    member _.``Serialize and deserialize double``() =
        
        let actual = (Value.Double 100.).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Double 100., Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    
    [<TestMethod>]
    member _.``Serialize and deserialize float``() =
        
        let actual = (Value.Float 100f).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Float 100f, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    [<TestMethod>]
    member _.``Serialize and deserialize int``() =
        
        let actual = (Value.Int 42).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Int 42, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
    
    [<TestMethod>]
    member _.``Serialize and deserialize short``() =
        
        let actual = (Value.Short 42s).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Short 42s, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)  
    
    [<TestMethod>]
    member _.``Serialize and deserialize long``() =
        
        let actual = (Value.Long 42L).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Long 42L, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    
    //[<TestMethod>]
    member _.``Serialize and deserialize string``() =
        
        let actual = (Value.String "Hello, World!").Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.String "Hello, World!", Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)  
   
    [<TestMethod>]
    member _.``Serialize and deserialize datetime``() =
        
        let dt = DateTime(2023, 03, 25)
        
        let actual = (Value.DateTime dt).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.DateTime dt, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    [<TestMethod>]
    member _.``Serialize and deserialize guid``() =
        
        let guid = Guid.NewGuid()
        
        
        let actual = (Value.Guid guid).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Guid guid, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
        
    [<TestMethod>]
    member _.``Serialize and deserialize option (some)``() =
        
        let actual = (Value.Option <| Some (Value.Int 42)).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Option <| Some (Value.Int 42), Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)
    
    [<TestMethod>]
    member _.``Serialize and deserialize option (none)``() =
        
        let actual = (Value.Option None).Serialize() |> Value.TryDeserialize
        
        let expected: Result<Value * byte array, string> = Ok (Value.Option None, Array.empty<byte>)
        
        Assert.AreEqual(expected, actual)  