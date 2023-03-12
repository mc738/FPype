namespace FPype.Tests.Core

open FPype.Core.Paths.Parsing
open Microsoft.VisualStudio.TestTools.UnitTesting

[<TestClass>]
type PathTests() =

    [<TestMethod>]
    member this.``Parse basic path``() =
        let expected =
            [ { Selector = SelectorToken.Child "foo"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = None } ]

        let path = "$.foo.bar"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member this.``Parse array slice``() =
        let expected =
            [ { Selector = SelectorToken.Child "foo"
                Filter = None
                ArraySelector = Some "2:4" }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = None } ]

        let path = "$.foo[2:4].bar"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member this.``Parse filter``() =
        let expected =
            [ { Selector = SelectorToken.Child "foo"
                Filter = Some "@.bar =~ '^TEST%'"
                ArraySelector = None }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = None } ]

        let path = "$.foo[?(@.bar =~ '^TEST%')].bar"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member this.``Parse filter and array slice``() =
        let expected =
            [ { Selector = SelectorToken.Child "foo"
                Filter = Some "@.bar =~ '^TEST%'"
                ArraySelector = None }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = Some "1:4" }
              { Selector = SelectorToken.Child "baz"
                Filter = None
                ArraySelector = None } ]

        let path = "$.foo[?(@.bar =~ '^TEST%')].bar[1:4].baz"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member this.``Parse delimited filter value``() =
        let expected =
            [ { Selector = SelectorToken.Child "foo"
                Filter = Some "@.bar =~ '^TEST)]%'"
                ArraySelector = None }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = Some "1:4" }
              { Selector = SelectorToken.Child "baz"
                Filter = None
                ArraySelector = None } ]

        let path = "$.foo[?(@.bar =~ '^TEST)]%')].bar[1:4].baz"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member this.``Parse delimited name``() =
        let expected =
            [ { Selector = SelectorToken.Child "'foo.baz'"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "bar"
                Filter = None
                ArraySelector = None } ]

        let path = "$.'foo.baz'.bar"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]
    member _.``Parse single character selector name``() =
        // $.store.f.book
        
        let expected =
            [ { Selector = SelectorToken.Child "store"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "f"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "book"
                Filter = None
                ArraySelector = None } ]

        let path = "$.store.f.book"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")

    [<TestMethod>]        
    member _.``Parse single character selector name at end``() =
        // $.store.f.book
        
        let expected =
            [ { Selector = SelectorToken.Child "store"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "books"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "f"
                Filter = None
                ArraySelector = None } ]

        let path = "$.store.books.f"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")
        
    [<TestMethod>]
    member _.``Parse single character selector name with filter``() =
        // $.store.f.book
        
        let expected =
            [ { Selector = SelectorToken.Child "store"
                Filter = None
                ArraySelector = None }
              { Selector = SelectorToken.Child "f"
                Filter = Some "@.price<10"
                ArraySelector = None }
              { Selector = SelectorToken.Child "book"
                Filter = None
                ArraySelector = None } ]

        let path = "$.store.f[?(@.price<10)].book"
        let result = parse path

        match result with
        | ParserResult.Success actual -> Assert.AreEqual(expected, actual)
        | _ -> Assert.Fail($"Parsing failed. Error: '{result}'")
    