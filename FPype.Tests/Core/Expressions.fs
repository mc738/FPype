namespace FPype.Tests.Core

open FPype.Core.Expressions
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type ExpressionOperatorTests() =

    [<TestMethod>]
    member this.Equal() =
        let expected = ExpressionOperatorToken.Equal("i", "1")
        let op = "i == 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member this.``Not equal``() =
        let expected = ExpressionOperatorToken.NotEqual("i", "1")

        let op = "i != 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member this.``Less than``() =
        let expected = ExpressionOperatorToken.LessThan("i", "1")

        let op = "i < 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member this.``Less than of equal``() =
        let expected = ExpressionOperatorToken.LessThanOrEqual("i", "1")

        let op = "i <= 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member this.``Greater than``() =
        let expected = ExpressionOperatorToken.GreaterThan("i", "1")

        let op = "i > 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)


    [<TestMethod>]
    member this.``Greater than or equal``() =
        let expected = ExpressionOperatorToken.GreaterThanOrEqual("i", "1")

        let op = "i >= 1"
        let actual = ExpressionOperatorToken.Deserialize op
        Assert.AreEqual(expected, actual)

[<TestClass>]
type ExpressionBuilderTests() =

    [<TestMethod>]
    member this.``Test expression statement``() =
        let expected =
            ExpressionStatement.And(
                ExpressionStatement.And(
                    ExpressionStatement.Operator <| ExpressionOperatorToken.GreaterThan("x", "1"),
                    (ExpressionStatement.Operator <| ExpressionOperatorToken.LessThan("y", "z"))
                ),
                ExpressionStatement.Or(
                    ExpressionStatement.Operator
                    <| ExpressionOperatorToken.LessThanOrEqual("y", "1"),
                    (ExpressionStatement.Operator <| ExpressionOperatorToken.Equal("z", "1"))
                )
            )

        let input = "(x > 1 && y < z) && y <= 1 || z == 1"

        match ExpressionBuilder.TryParse input with
        | Ok actual -> Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail(e)


    [<TestMethod>]
    member this.``Test expression statement with extra brackets``() =
        let expected =
            ExpressionStatement.And(
                ExpressionStatement.And(
                    ExpressionStatement.Operator <| ExpressionOperatorToken.GreaterThan("x", "1"),
                    (ExpressionStatement.Operator <| ExpressionOperatorToken.LessThan("y", "z"))
                ),
                ExpressionStatement.Or(
                    ExpressionStatement.Operator
                    <| ExpressionOperatorToken.LessThanOrEqual("y", "1"),
                    (ExpressionStatement.Operator <| ExpressionOperatorToken.Equal("z", "1"))
                )
            )

        let input = "((x > 1) && (y < z)) && y <= 1 || z == 1"

        match ExpressionBuilder.TryParse input with
        | Ok actual -> Assert.AreEqual(expected, actual)
        | Error e -> Assert.Fail(e)
