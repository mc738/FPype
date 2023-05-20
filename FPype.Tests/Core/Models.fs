namespace FPype.Tests.Core.Models

open FPype.Core.Types
open FPype.Data.Models
open Microsoft.VisualStudio.TestTools.UnitTesting


[<TestClass>]
type TablesTests() =

    [<TestMethod>]
    member _.``Serialize and deserialize row``() =
        let row = ({ Values = [ Value.String "Hello, World!"; Value.Int 42 ] }: TableRow)

        let expected: Result<TableRow * byte array, string> = Ok(row, [||])

        let actual = row.Serialize() |> TableRow.TryDeserialize

        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member _.``Serialize and deserialize column``() =
        let column =
            ({ Name = "test_column"
               Type = BaseType.String
               ImportHandler = None })

        //let expected: Result<TableColumn * byte array, string> = Ok (column, [||])

        let actual = column.Serialize() |> TableColumn.TryDeserialize

        // NOTE - Manual assertions because the standard Assert.AreEqual doesn't work.
        //Assert.AreEqual(expected, actual)

        match actual with
        | Ok(tc, r) ->
            Assert.AreEqual(column.Name, tc.Name)
            Assert.AreEqual(column.Type, tc.Type)
            Assert.AreEqual(column.ImportHandler, None)
            Assert.AreEqual(0, r.Length)
        | Error e -> Assert.Fail(e)

    [<TestMethod>]
    member _.``Serialize and deserialize column with optional type``() =
        let column =
            ({ Name = "test_column"
               Type = BaseType.Option BaseType.String
               ImportHandler = None })

        //let expected: Result<TableColumn * byte array, string> = Ok (column, [||])

        let actual = column.Serialize() |> TableColumn.TryDeserialize

        // NOTE - Manual assertions because the standard Assert.AreEqual doesn't work.
        //Assert.AreEqual(expected, actual)

        match actual with
        | Ok(tc, r) ->
            Assert.AreEqual(column.Name, tc.Name)
            Assert.AreEqual(column.Type, tc.Type)
            Assert.AreEqual(column.ImportHandler, None)
            Assert.AreEqual(0, r.Length)
        | Error e -> Assert.Fail(e)

    [<TestMethod>]
    member _.``Serialize and deserialize table``() =
        let table =
            ({ Name = "test_table"
               Columns =
                 [ { Name = "col_a"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "col_b"
                     Type = BaseType.Int
                     ImportHandler = None } ]
               Rows =
                 [ { Values = [ Value.String "Hello, World!"; Value.Int 42 ] }
                   { Values = [ Value.String "Hello, again!"; Value.Int 100 ] } ] })

        match table.Serialize() |> TableModel.TryDeserialize with
        | Ok(actual, data) ->
            Assert.AreEqual(0, data.Length)
            Assert.AreEqual(table.Name, actual.Name)
            Assert.AreEqual(table.Columns.Length, actual.Columns.Length)
            Assert.AreEqual(table.Rows.Length, actual.Rows.Length)

            table.Columns
            |> List.iteri (fun i c ->
                let ac = actual.Columns |> List.item i
                Assert.AreEqual(c.Name, ac.Name)
                Assert.AreEqual(c.Type, ac.Type)
                Assert.AreEqual(c.ImportHandler, None))

            table.Rows
            |> List.iteri (fun i r ->
                let ar = actual.Rows |> List.item i
                Assert.AreEqual(r, ar))
        | Error e -> Assert.Fail(e)


    [<TestMethod>]
    member _.``Serialize and deserialize empty table``() =
        let table =
            ({ Name = "test_table"
               Columns =
                 [ { Name = "col_a"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "col_b"
                     Type = BaseType.Int
                     ImportHandler = None } ]
               Rows = [] })

        match table.Serialize() |> TableModel.TryDeserialize with
        | Ok(actual, data) ->
            Assert.AreEqual(0, data.Length)
            Assert.AreEqual(table.Name, actual.Name)
            Assert.AreEqual(table.Columns.Length, actual.Columns.Length)
            Assert.AreEqual(table.Rows.Length, actual.Rows.Length)

            table.Columns
            |> List.iteri (fun i c ->
                let ac = actual.Columns |> List.item i
                Assert.AreEqual(c.Name, ac.Name)
                Assert.AreEqual(c.Type, ac.Type)
                Assert.AreEqual(c.ImportHandler, None))

            table.Rows
            |> List.iteri (fun i r ->
                let ar = actual.Rows |> List.item i
                Assert.AreEqual(r, ar))
        | Error e -> Assert.Fail(e)
