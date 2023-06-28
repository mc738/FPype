module FPype.Tests.Data

open FPype.Data
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type SerializableQueryTests() =

    [<TestMethod>]
    member _.``Basic select query``() =
        let expected = "SELECT `t`.`foo`, `t`.`bar` FROM `test` `t`"


        let actual =
            ({ Select =
                [ SerializableQueries.Select.Field { Field = "foo"; TableName = "t" }
                  SerializableQueries.Select.Field { Field = "bar"; TableName = "t" } ]
               From = { Name = "test"; Alias = Some "t" }
               Joins = []
               Where = None }
            : SerializableQueries.Query).ToSql()
            
        Assert.AreEqual(expected, actual)
        


        ()
