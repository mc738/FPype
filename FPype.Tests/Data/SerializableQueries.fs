module FPype.Tests.Data

open System.IO
open System.Text
open System.Text.Json
open FPype.Data
open Microsoft.VisualStudio.TestTools.UnitTesting

[<AutoOpen>]
module private Utils =

    let writeToJson (fn: Utf8JsonWriter -> unit) =
        use ms = new MemoryStream()

        let mutable opts = JsonWriterOptions()
        opts.Indented <- true

        use writer = new Utf8JsonWriter(ms, opts)

        fn writer

        writer.Flush()

        ms.ToArray() |> Encoding.UTF8.GetString

    let loadJson (str: string) = (JsonDocument.Parse str).RootElement

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
                 ) }
            : SerializableQueries.Join)
                .ToSql()

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Basic select query with join``() =
        let expected =
            "SELECT `a`.`foo`, `a`.`bar`, `b`.`baz` FROM `a_table` `a` JOIN `b_table` `b` ON `a`.`id` = `b`.`a_id`"

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

    [<TestMethod>]
    member _.``Condition.Equals serialization``() =
        let expected = "`a`.`id` = `b`.`a_id`"

        let actual =
            SerializableQueries.Condition
                .Equals(
                    SerializableQueries.Value.Field { Field = "id"; TableName = "a" },
                    SerializableQueries.Value.Field { Field = "a_id"; TableName = "b" }
                )
                .ToSql()

        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member _.``Condition.GreaterThan serialization``() =
        let expected = "`a`.`id` > `b`.`a_id`"

        let actual =
            SerializableQueries.Condition
                .GreaterThan(
                    SerializableQueries.Value.Field { Field = "id"; TableName = "a" },
                    SerializableQueries.Value.Field { Field = "a_id"; TableName = "b" }
                )
                .ToSql()

        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member _.``Condition.GreaterThanOrEquals serialization``() =
        let expected = "`a`.`id` >= `b`.`a_id`"

        let actual =
            SerializableQueries.Condition
                .GreaterThanOrEquals(
                    SerializableQueries.Value.Field { Field = "id"; TableName = "a" },
                    SerializableQueries.Value.Field { Field = "a_id"; TableName = "b" }
                )
                .ToSql()

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert table field to and from json``() =
        let tf = ({ TableName = "table_1"; Field = "foo" }: SerializableQueries.TableField)

        let expected: Result<SerializableQueries.TableField, string> = Ok tf

        let actual =
            writeToJson tf.WriteToJson
            |> loadJson
            |> SerializableQueries.TableField.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert table to and from json (no alias)``() =
        let t = ({ Name = "table_1"; Alias = None }: SerializableQueries.Table)

        let expected: Result<SerializableQueries.Table, string> = Ok t

        let actual =
            writeToJson t.WriteToJson |> loadJson |> SerializableQueries.Table.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert table to and from json (with alias)``() =
        let t = ({ Name = "table_1"; Alias = Some "t" }: SerializableQueries.Table)

        let expected: Result<SerializableQueries.Table, string> = Ok t

        let actual =
            writeToJson t.WriteToJson |> loadJson |> SerializableQueries.Table.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert literal value to and from json``() =
        let value = SerializableQueries.Value.Literal "Hello, World!"

        let expected: Result<SerializableQueries.Value, string> = Ok value

        let actual =
            writeToJson value.WriteToJson |> loadJson |> SerializableQueries.Value.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert number value to and from json``() =
        let value = SerializableQueries.Value.Number 42m

        let expected: Result<SerializableQueries.Value, string> = Ok value

        let actual =
            writeToJson value.WriteToJson |> loadJson |> SerializableQueries.Value.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert boolean value to and from json``() =
        let value = SerializableQueries.Value.Boolean true

        let expected: Result<SerializableQueries.Value, string> = Ok value

        let actual =
            writeToJson value.WriteToJson |> loadJson |> SerializableQueries.Value.FromJson

        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member _.``Convert field value to and from json``() =
        let value = SerializableQueries.Value.Field { TableName = "table_1"; Field = "foo" }

        let expected: Result<SerializableQueries.Value, string> = Ok value

        let actual =
            writeToJson value.WriteToJson |> loadJson |> SerializableQueries.Value.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert parameter value to and from json``() =
        let value = SerializableQueries.Value.Parameter "foo"

        let expected: Result<SerializableQueries.Value, string> = Ok value

        let actual =
            writeToJson value.WriteToJson |> loadJson |> SerializableQueries.Value.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert equals condition to and from json``() =
        let condition =
            SerializableQueries.Condition.Equals(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" },
                SerializableQueries.Value.Boolean true
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert greater than condition to and from json``() =
        let condition =
            SerializableQueries.Condition.GreaterThan(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" },
                SerializableQueries.Value.Boolean true
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert greater than or equals condition to and from json``() =
        let condition =
            SerializableQueries.Condition.GreaterThanOrEquals(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" },
                SerializableQueries.Value.Boolean true
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert less than condition to and from json``() =
        let condition =
            SerializableQueries.Condition.LessThan(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" },
                SerializableQueries.Value.Boolean true
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert less than or equals condition to and from json``() =
        let condition =
            SerializableQueries.Condition.LessThanOrEquals(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" },
                SerializableQueries.Value.Boolean true
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert is null condition to and from json``() =
        let condition =
            SerializableQueries.Condition.IsNull(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" }
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Convert is not null condition to and from json``() =
        let condition =
            SerializableQueries.Condition.IsNotNull(
                SerializableQueries.Value.Field
                    { TableName = "test_table"
                      Field = "foo" }
            )

        let expected: Result<SerializableQueries.Condition, string> = Ok condition

        let actual =
            writeToJson condition.WriteToJson
            |> loadJson
            |> SerializableQueries.Condition.FromJson

        Assert.AreEqual(expected, actual)
