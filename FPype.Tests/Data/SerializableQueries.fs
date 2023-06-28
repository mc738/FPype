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
            : SerializableQueries.Query)
                .ToSql()

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Basic join serialization test``() =
        let expected = "JOIN `b_table` `b` ON `a`.`id` = `b`.`a_id`"
        
        let actual =
            ({ Type = SerializableQueries.JoinType.Inner
               Table = { Name = "b_table"; Alias = Some "b" }
               Condition =
                SerializableQueries.Condition.Equals(
                    SerializableQueries.Value.Field { Field = "id"; TableName = "a" },
                    SerializableQueries.Value.Field { Field = "a_id"; TableName = "b" }
                ) }: SerializableQueries.Join).Serialize()
        
        Assert.AreEqual(expected, actual)
    
    [<TestMethod>]
    member _.``Basic select query with join``() =
        let expected = "SELECT `a`.`foo`, `a`.`bar`, `b`.`baz` FROM `a_table` `a` JOIN `b_table` `b` ON `a`.`id` = `b`.`a_id`"

        let actual =
            ({ Select =
                [ SerializableQueries.Select.Field { Field = "foo"; TableName = "a" }
                  SerializableQueries.Select.Field { Field = "bar"; TableName = "a" }
                  SerializableQueries.Select.Field { Field = "baz"; TableName = "b" } ]
               From = { Name = "a_table"; Alias = Some "a" }
               Joins =
                 [ { Type = SerializableQueries.JoinType.Inner
                     Table = { Name = "b_table"; Alias = Some "b" }
                     Condition =
                       SerializableQueries.Condition.Equals(
                           SerializableQueries.Value.Field { Field = "id"; TableName = "a" },
                           SerializableQueries.Value.Field { Field = "a_id"; TableName = "b" }
                       ) } ]
               Where = None }
            : SerializableQueries.Query)
                .ToSql()

        Assert.AreEqual(expected, actual)
