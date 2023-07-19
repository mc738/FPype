namespace FPype.Connectors

module MySql =

    open FPype.Core.Types
    open FPype.Data.Models
    open MySql.Data.MySqlClient

    [<AutoOpen>]
    module private Internal =

        let mapper (columns: TableColumn list) (reader: MySqlDataReader) =
            let rec handler t (i: int) =
                // NOTE This uses the int override of Get[type] because IsDBNull requires an int.
                match t with
                | BaseType.Boolean -> reader.GetBoolean i |> Value.Boolean
                | BaseType.Byte -> reader.GetByte i |> Value.Byte
                | BaseType.Char -> reader.GetChar i |> Value.Char
                | BaseType.Decimal -> reader.GetDecimal i |> Value.Decimal
                | BaseType.Double -> reader.GetDouble i |> Value.Double
                | BaseType.Float -> reader.GetFloat i |> Value.Float
                | BaseType.Int -> reader.GetInt32 i |> Value.Int
                | BaseType.Short -> reader.GetInt16 i |> Value.Short
                | BaseType.Long -> reader.GetInt64 i |> Value.Long
                | BaseType.String -> reader.GetString i |> Value.String
                | BaseType.DateTime -> reader.GetDateTime i |> Value.DateTime
                | BaseType.Guid -> reader.GetGuid i |> Value.Guid
                | BaseType.Option it ->
                    match reader.IsDBNull i with
                    | true -> None |> Value.Option
                    | false -> handler it i |> Some |> Value.Option

            [ while reader.Read() do
                  columns
                  |> List.map (fun c ->
                      let i = reader.GetOrdinal c.Name
                      handler c.Type i)
                  |> TableRow.FromValues ]


    ()
