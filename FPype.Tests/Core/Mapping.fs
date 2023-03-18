namespace FPype.Tests.Core

open FPype.Core.Types
open FPype.Data
open FPype.Data.Models
open Microsoft.VisualStudio.TestTools.UnitTesting

module Mapping =

    open FPype.Core
    open FPype.Actions

    module private Resources =

        let unwrap (r: Result<'a, 'b>) =
            match r with
            | Ok v -> v
            | Error _ -> failwith "Error"

        let exampleObj =
            """
{
    "id": "test_id",
    "name": "Test",
    "items": [
        {
            "type": "type1",
            "subId": "sub_id_1",
            "values": [
                {
                    "name": "Item value 1",
                    "value": "Hello, World!"
                },
                {
                    "name": "Item value 2",
                    "value": "Another value"
                }
            ]
        },
        {
            "type": "type2",
            "subId": "sub_id_2",
            "values": [
                {
                    "name": "Item value 3",
                    "value": "Hello, again!"
                },
                {
                    "name": "Item value 4",
                    "value": "Another other value"
                }
            ]
        },
        {
            "type": "type1",
            "subId": "sub_id_3",
            "values": [
                {
                    "name": "Item value 5",
                    "value": "Hello, once more!"
                },
                {
                    "name": "Item value 6",
                    "value": "Another other other value"
                }
            ]
        }
    ]
}
        """

        let exampleMap =
            """
{
    "selector": "$",
    "columns": [
        {
            "type": "selector",
            "selector": "$.id",
            "name": "item_id"
        },
        {
            "type": "selector",
            "selector": "$.name",
            "name": "name"
        }
    ],
    "innerScopes": [
        {
            "selector": "$.items[?(@.type =~ '^type1$')]",
            "columns": [
                {
                    "type": "selector",
                    "selector": "$.subId",
                    "name": "sub_id"
                },
                {
                    "type": "constant",
                    "value": "type_1",
                    "name": "type"
                }
            ],
            "innerScopes": [
                {
                    "selector": "$.values[0]",
                    "columns": [
                        {
                            "type": "selector",
                            "selector": "$.name",
                            "name": "inner_name"
                        },
                        {
                            "type": "selector",
                            "selector": "$.value",
                            "name": "inner_value"
                        }
                    ]
                }
            ]
        },
        {
            "selector": "$.items[?(@.type =~ '^type2$')]",
            "columns": [
                {
                    "type": "selector",
                    "selector": "$.subId",
                    "name": "sub_id"
                },
                {
                    "type": "constant",
                    "value": "type_2",
                    "name": "type"
                }
            ],
            "innerScopes": [
                {
                    "selector": "$.values[0]",
                    "columns": [
                        {
                            "type": "selector",
                            "selector": "$.name",
                            "name": "inner_name"
                        },
                        {
                            "type": "selector",
                            "selector": "$.value",
                            "name": "inner_value"
                        }
                    ]
                }
            ]
        }   
    ]
}
"""

        let exampleObjJson _ =
            exampleObj.Trim() |> toJsonElement |> unwrap

        let exampleMapJson _ =
            exampleMap.Trim() |> toJsonElement |> unwrap

        let table =
            ({ Name = "Test"
               Columns =
                 [ { Name = "item_id"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "name"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "sub_id"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "type"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "inner_name"
                     Type = BaseType.String
                     ImportHandler = None }
                   { Name = "inner_value"
                     Type = BaseType.String
                     ImportHandler = None } ]
               Rows = [] }: TableModel)

        let scope _ =
            exampleMapJson () |> ObjectTableMapScope.FromJson |> unwrap

        let map _ =
            ({ Table = table; RootScope = scope () }: ObjectTableMap)

    [<TestClass>]
    type ObjectTableTests() =

        [<TestMethod>]
        member _.``Parse TableObjectMapScope``() =

            let expected : Result<TableRow, string> list =
                [ Ok(
                      { Values =
                          [ Value.String "test_id"
                            Value.String "Test"
                            Value.String "sub_id_1"
                            Value.String "type_1"
                            Value.String "Item value 1"
                            Value.String "Hello, World!" ] }: TableRow
                  )
                  Ok(
                      { Values =
                          [ Value.String "test_id"
                            Value.String "Test"
                            Value.String "sub_id_3"
                            Value.String "type_1"
                            Value.String "Item value 5"
                            Value.String "Hello, once more!" ] }: TableRow
                  )
                  Ok(
                      { Values =
                          [ Value.String "test_id"
                            Value.String "Test"
                            Value.String "sub_id_2"
                            Value.String "type_2"
                            Value.String "Item value 3"
                            Value.String "Hello, again!" ] }: TableRow
                  ) ]


            let map = Resources.map ()

            let actual = Mapping.ObjectTable.run map (Resources.exampleObjJson ())

            Assert.AreEqual(expected, actual)