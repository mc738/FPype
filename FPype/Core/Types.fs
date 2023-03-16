namespace FPype.Core

open System
open System.Globalization
open System.Text.Json
open System.Text.RegularExpressions

module Types =

    module private TypeHelpers =

        // An active pattern to get an option value from an object.
        let (|SomeObj|_|) =
            let ty = typedefof<option<_>>

            fun (a: obj) ->
                let aty = a.GetType()
                let v = aty.GetProperty("Value")

                if aty.IsGenericType && aty.GetGenericTypeDefinition() = ty then
                    if a = null then None else Some(v.GetValue(a, [||]))
                else
                    None

        let getName<'T> = typeof<'T>.FullName

        let typeName (t: Type) = t.FullName

        let boolName = getName<bool>

        let uByteName = getName<uint8>

        let uShortName = getName<uint16>

        let uIntName = getName<uint32>

        let uLongName = getName<uint64>

        let byteName = getName<byte>

        let shortName = getName<int16>

        let intName = getName<int>

        let longName = getName<int64>

        let floatName = getName<float>

        let doubleName = getName<double>

        let decimalName = getName<decimal>

        let charName = getName<char>

        let timestampName = getName<DateTime>

        let uuidName = getName<Guid>

        let stringName = getName<string>

        let isOption (name: string) =
            Regex
                .Match(
                    name,
                    "(?<=Microsoft.FSharp.Core.FSharpOption`1\[\[).+?(?=\,)"
                )
                .Success

        let getOptionType name =
            // Maybe a bit wasteful doing this twice.
            Regex
                .Match(
                    name,
                    "(?<=Microsoft.FSharp.Core.FSharpOption`1\[\[).+?(?=\,)"
                )
                .Value


        let convert (value: obj) (target: Type) =
            try
                Convert.ChangeType(value, target) |> Ok
            with ex ->
                Error ex.Message

    /// An internal DU for representing base types.
    [<RequireQualifiedAccess>]
    type BaseType =
        | Boolean
        | Byte
        | Char
        | Decimal
        | Double
        | Float
        | Int
        | Short
        | Long
        | String
        | DateTime
        | Guid
        | Option of BaseType

        static member TryFromName(name: String) =
            match name with
            | t when t = TypeHelpers.boolName -> Ok BaseType.Boolean
            | t when t = TypeHelpers.byteName -> Ok BaseType.Byte
            | t when t = TypeHelpers.charName -> Ok BaseType.Char
            | t when t = TypeHelpers.decimalName -> Ok BaseType.Decimal
            | t when t = TypeHelpers.doubleName -> Ok BaseType.Double
            | t when t = TypeHelpers.floatName -> Ok BaseType.Float
            | t when t = TypeHelpers.intName -> Ok BaseType.Int
            | t when t = TypeHelpers.shortName -> Ok BaseType.Short
            | t when t = TypeHelpers.longName -> Ok BaseType.Long
            | t when t = TypeHelpers.stringName -> Ok BaseType.String
            | t when t = TypeHelpers.timestampName -> Ok BaseType.DateTime
            | t when t = TypeHelpers.uuidName -> Ok BaseType.Guid
            | t when TypeHelpers.isOption t = true ->
                let ot = TypeHelpers.getOptionType t

                match BaseType.TryFromName ot with
                | Ok st -> Ok(BaseType.Option st)
                | Error e -> Error e
            | _ -> Error $"Type `{name}` not supported."

        static member TryFromType(typeInfo: Type) = BaseType.TryFromName(typeInfo.FullName)

        static member FromName(name: string) =
            match BaseType.TryFromName name with
            | Ok st -> st
            | Error _ -> BaseType.String

        static member FromType(typeInfo: Type) = BaseType.FromName(typeInfo.FullName)

        (*
        static member GetDataTypes() =
            seq {
                DataType("bool", "Boolean")
                DataType("byte", "Byte")
                DataType("char", "Character")
                DataType("datetime", "Datetime")
                DataType("decimal", "Decimal")
                DataType("double", "Double")
                DataType("float", "Float")
                DataType("int", "Integer")
                DataType("short", "Short")
                DataType("long", "Long")
                DataType("string", "String")
                DataType("uuid", "Uuid")
            }
        *)

        static member FromId(id: string, optional: bool) =
            match id with
            | "bool" -> Some BaseType.Boolean
            | "byte" -> Some BaseType.Byte
            | "char" -> Some BaseType.Char
            | "datetime" -> Some BaseType.DateTime
            | "decimal" -> Some BaseType.Decimal
            | "double" -> Some BaseType.Double
            | "float" -> Some BaseType.Float
            | "int" -> Some BaseType.Int
            | "short" -> Some BaseType.Short
            | "long" -> Some BaseType.Long
            | "string" -> Some BaseType.String
            | "uuid" -> Some BaseType.Guid
            | _ -> None
            |> Option.map (fun bt ->
                match optional with
                | true -> BaseType.Option bt
                | false -> bt)

        (*   
        static member FromDataType(dataType: DataType, optional: bool) =
            BaseType.FromId(dataType.Id, optional)
        *)

        (*    
        member bt.ToDateType() =
            let rec get (t: BaseType) =
                match t with
                | BaseType.Boolean -> DataType("bool", "Boolean")
                | BaseType.Byte ->  DataType("byte", "Byte")
                | BaseType.Char -> DataType("char", "Character")
                | BaseType.DateTime -> DataType("datetime", "Datetime")
                | BaseType.Decimal -> DataType("decimal", "Decimal")
                | BaseType.Double -> DataType("double", "Double")
                | BaseType.Float -> DataType("float", "Float")
                | BaseType.Int -> DataType("int", "Integer")
                | BaseType.Short -> DataType("short", "Short")
                | BaseType.Long -> DataType("long", "Long")
                | BaseType.Guid -> DataType("uuid", "Uuid")
                | BaseType.Option ibt -> get ibt
                
            get bt
        *)

        member bt.IsOptionType() =
            match bt with
            | BaseType.Option _ -> true
            | _ -> false

    [<RequireQualifiedAccess>]
    type CoercionResult =
        | Success of Value
        | IncompatibleType of string
        | NonScalarValue of string
        | NullValue of string
        | Failure of string

        static member bind (fn: Value -> CoercionResult) (cr: CoercionResult) =
            match cr with
            | Success v -> fn v
            | _ -> cr

        static member map (fn: Value -> Value) (cr: CoercionResult) =
            match cr with
            | Success v -> fn v |> CoercionResult.Success
            | _ -> cr

    and [<RequireQualifiedAccess>] Value =
        | Boolean of bool
        | Byte of byte
        | Char of char
        | Decimal of decimal
        | Double of double
        | Float of float
        | Int of int
        | Short of int16
        | Long of int64
        | String of string
        | DateTime of DateTime
        | Guid of Guid
        | Option of Value option

        static member CoerceValueToType<'T>(value: 'T, baseType: BaseType) =
            let handler value (target: Type) (successHandler: obj -> Value) =
                TypeHelpers.convert value target
                |> fun v ->
                    match v with
                    | Ok fv -> successHandler fv |> CoercionResult.Success
                    | Error e -> CoercionResult.Failure e

            let handler1 = handler value

            match baseType with
            | BaseType.Boolean -> handler1 (typeof<bool>) (fun o -> o :?> bool |> Value.Boolean)
            | BaseType.Byte -> handler1 (typeof<byte>) (fun o -> o :?> byte |> Value.Byte)
            | BaseType.Char -> handler1 (typeof<char>) (fun o -> o :?> char |> Value.Char)
            | BaseType.Decimal -> handler1 (typeof<decimal>) (fun o -> o :?> decimal |> Value.Decimal)
            | BaseType.Double -> handler1 (typeof<double>) (fun o -> o :?> double |> Value.Double)
            | BaseType.Float -> handler1 (typeof<float>) (fun o -> o :?> float |> Value.Float)
            | BaseType.Int -> handler1 (typeof<int>) (fun o -> o :?> int |> Value.Int)
            | BaseType.Short -> handler1 (typeof<int16>) (fun o -> o :?> int16 |> Value.Short)
            | BaseType.Long -> handler1 (typeof<int64>) (fun o -> o :?> int64 |> Value.Long)
            | BaseType.String -> handler1 (typeof<string>) (fun o -> o :?> string |> Value.String)
            | BaseType.DateTime -> handler1 (typeof<DateTime>) (fun o -> o :?> DateTime |> Value.DateTime)
            | BaseType.Guid -> handler1 (typeof<Guid>) (fun o -> o :?> Guid |> Value.Guid)
            | BaseType.Option t ->

                let toOption v = v |> Some |> Value.Option

                match TypeHelpers.isOption (value.GetType().FullName), value with
                | false, _ ->
                    match
                        value.GetType().FullName = TypeHelpers.stringName
                        && String.IsNullOrWhiteSpace(value.ToString())
                    with
                    | true ->
                        // NOTE - Special handling for when a value is passed in as a string and it is  null or whitespace
                        // In this cases it will be treated as none.
                        Value.Option None |> CoercionResult.Success
                    | false ->
                        // `value` is not an options, so it can be passed straight back into `CoerceValueToType`
                        match Value.CoerceValueToType(value, t) with
                        | CoercionResult.Success fv -> fv |> toOption |> CoercionResult.Success
                        | CoercionResult.Failure e ->
                            CoercionResult.Failure $"Could not get optional value. Error: '{e}'"
                        | r -> r
                | true, TypeHelpers.SomeObj (v1) ->
                    match t with
                    | BaseType.Boolean -> handler v1 typeof<bool> (fun o -> o :?> bool |> Value.Boolean |> toOption)
                    | BaseType.Byte -> handler v1 typeof<byte> (fun o -> o :?> byte |> Value.Byte |> toOption)
                    | BaseType.Char -> handler v1 typeof<char> (fun o -> o :?> char |> Value.Char |> toOption)
                    | BaseType.Decimal ->
                        handler v1 typeof<decimal> (fun o -> o :?> decimal |> Value.Decimal |> toOption)
                    | BaseType.Double -> handler v1 typeof<double> (fun o -> o :?> double |> Value.Double |> toOption)
                    | BaseType.Float -> handler v1 typeof<float> (fun o -> o :?> float |> Value.Float |> toOption)
                    | BaseType.Int -> handler v1 typeof<int> (fun o -> o :?> int |> Value.Int |> toOption)
                    | BaseType.Short -> handler v1 typeof<int16> (fun o -> o :?> int16 |> Value.Short |> toOption)
                    | BaseType.Long -> handler v1 typeof<int64> (fun o -> o :?> int64 |> Value.Long |> toOption)
                    | BaseType.String -> handler v1 typeof<string> (fun o -> o :?> string |> Value.String |> toOption)
                    | BaseType.DateTime ->
                        handler v1 typeof<DateTime> (fun o -> o :?> DateTime |> Value.DateTime |> toOption)
                    | BaseType.Guid -> handler v1 typeof<Guid> (fun o -> o :?> Guid |> Value.Guid |> toOption)
                    | BaseType.Option _ -> CoercionResult.Failure "Nested option types not currently supported."
                | _ -> CoercionResult.Failure "Could not get option value from object."

        static member CoerceValue<'T>(value: 'T) =

            let convert (target: Type) =
                try
                    Convert.ChangeType(value, target) |> Ok
                with ex ->
                    Error ex.Message

            let handler (target: Type) (successHandler: obj -> Value) =
                convert target
                |> fun v ->
                    match v with
                    | Ok fv -> successHandler fv |> CoercionResult.Success
                    | Error e -> CoercionResult.Failure e

            match BaseType.TryFromType typeof<'T> with
            | Ok bt -> Value.CoerceValueToType(value, bt)
            | Error e -> CoercionResult.Failure $"Could not get base type. Error '{e}'"

        static member FromJsonValue(json: JsonElement, baseType: BaseType) =
            let handleTypeError (jvk: JsonValueKind) (bt: BaseType) =
                CoercionResult.IncompatibleType $"Json value kind `{jvk}` can not be coerced to type `{bt}`"

            let rec handler (el: JsonElement, bt: BaseType) =
                match baseType with
                | BaseType.Boolean ->
                    match el.ValueKind with
                    | JsonValueKind.True -> Value.Boolean true |> CoercionResult.Success
                    | JsonValueKind.False -> Value.Boolean false |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Byte ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetByte() |> Value.Byte |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Char ->
                    match el.ValueKind with
                    | JsonValueKind.String -> el.GetString().[0] |> Value.Char |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Decimal ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetDecimal() |> Value.Decimal |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Double ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetDouble() |> Value.Double |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Float ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetDouble() |> Value.Float |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Int ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetInt32() |> Value.Int |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Short ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetInt16() |> Value.Short |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Long ->
                    match el.ValueKind with
                    | JsonValueKind.Number -> json.GetInt16() |> Value.Short |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.String ->
                    match el.ValueKind with
                    | JsonValueKind.String -> json.GetString() |> Value.String |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.DateTime ->
                    match el.ValueKind with
                    | JsonValueKind.String -> json.GetDateTime() |> Value.DateTime |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Guid ->
                    match el.ValueKind with
                    | JsonValueKind.String -> json.GetGuid() |> Value.Guid |> CoercionResult.Success
                    | jvk -> handleTypeError jvk bt
                | BaseType.Option sbt ->
                    match el.ValueKind with
                    | JsonValueKind.Null
                    | JsonValueKind.Undefined -> Value.Option None |> CoercionResult.Success
                    | _ -> handler (el, sbt) |> CoercionResult.map (fun v -> Some v |> Value.Option)

            match baseType, json.ValueKind with
            | BaseType.Option _, JsonValueKind.Null
            | BaseType.Option _, JsonValueKind.Undefined -> Value.Option None |> CoercionResult.Success
            | bt, JsonValueKind.Null
            | bt, JsonValueKind.Undefined ->
                CoercionResult.NullValue $"The type `{bt}` does not accept null or undefined values."
            | _, JsonValueKind.Object
            | _, JsonValueKind.Array -> CoercionResult.NonScalarValue "The json value must be scalar."
            | _, _ ->
                try
                    handler (json, baseType)
                with exn ->
                    CoercionResult.Failure $"Unhandled exception: {exn.Message}"

        static member FromString(str: string, baseType: BaseType, ?format: string) =
            let rec handler (bt: BaseType) =
                match baseType with
                | BaseType.Boolean ->
                    match [ "yes"; "true"; "ok"; "1" ] |> List.contains (str.ToLower()) with
                    | true -> Value.Boolean true |> Some
                    | false -> None
                | BaseType.Byte ->
                    match Byte.TryParse str with
                    | true, v -> Value.Byte v |> Some
                    | false, _ -> None
                | BaseType.Char ->
                    match String.IsNullOrEmpty(str) with
                    | true -> None
                    | false -> Value.Char str.[0] |> Some
                | BaseType.Decimal ->
                    match Decimal.TryParse str with
                    | true, v -> Value.Decimal v |> Some
                    | false, _ -> None
                | BaseType.Double ->
                    match Double.TryParse str with
                    | true, v -> Value.Double v |> Some
                    | false, _ -> None
                | BaseType.Float ->
                    match Double.TryParse str with
                    | true, v -> Value.Float v |> Some
                    | false, _ -> None
                | BaseType.Int ->
                    match Int32.TryParse str with
                    | true, v -> Value.Int v |> Some
                    | false, _ -> None
                | BaseType.Short ->
                    match Int16.TryParse str with
                    | true, v -> Value.Short v |> Some
                    | false, _ -> None
                | BaseType.Long ->
                    match Int64.TryParse str with
                    | true, v -> Value.Long v |> Some
                    | false, _ -> None
                | BaseType.String ->
                    match str = null with
                    | true -> None
                    | false -> Value.String str |> Some
                | BaseType.DateTime ->
                    match format with
                    | Some f ->
                        match
                            DateTime.TryParseExact(
                                str,
                                f,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AdjustToUniversal
                            )
                        with
                        | true, v -> Value.DateTime v |> Some
                        | false, _ -> None
                    | None ->
                        match DateTime.TryParse str with
                        | true, v -> Value.DateTime v |> Some
                        | false, _ -> None
                | BaseType.Guid ->
                    match format with
                    | Some f ->
                        match Guid.TryParseExact(str, f) with
                        | true, v -> Value.Guid v |> Some
                        | false, _ -> None
                    | None ->
                        match Guid.TryParse str with
                        | true, v -> Value.Guid v |> Some
                        | false, _ -> None
                | BaseType.Option ibt -> handler ibt

            handler baseType

        member fv.Box() =
            let rec handler (value: Value) =
                match value with
                | Boolean v -> v |> box
                | Byte v -> v |> box
                | Char v -> v |> box
                | Decimal v -> v |> box
                | Double v -> v |> box
                | Float v -> v |> box
                | Int v -> v |> box
                | Short v -> v |> box
                | Long v -> v |> box
                | String v -> v |> box
                | DateTime v -> v |> box
                | Guid v -> v |> box
                | Option v ->
                    match v with
                    | Some v -> handler v
                    | None -> None |> box

            handler fv

        member fv.GetString() =
            let rec handler (value: Value) =
                match value with
                | Boolean v -> v.ToString()
                | Byte v -> v.ToString()
                | Char v -> v.ToString()
                | Decimal v -> v.ToString()
                | Double v -> v.ToString()
                | Float v -> v.ToString()
                | Int v -> v.ToString()
                | Short v -> v.ToString()
                | Long v -> v.ToString()
                | String v -> v
                | DateTime v -> v.ToString()
                | Guid v -> v.ToString()
                | Option v ->
                    match v with
                    | Some v -> handler v
                    | None -> ""

            handler fv

        member fv.GetDecimal() =
            let rec handler (value: Value) =
                match value with
                | Boolean _ -> 0m
                | Byte v -> decimal v
                | Char _ -> 0m
                | Decimal v -> v
                | Double v -> decimal v
                | Float v -> decimal v
                | Int v -> decimal v
                | Short v -> decimal v
                | Long v -> decimal v
                | String v -> decimal v
                | DateTime _ -> 0m
                | Guid _ -> 0m
                | Option v ->
                    match v with
                    | Some v -> handler v
                    | None -> 0m

            handler fv

        member v.IsMatch(value: Value) =
            let rec handler (v: Value) (v2: Value) =
                match v, v2 with
                | Value.Boolean b1, Value.Boolean b2 -> b1 = b2
                | Value.Byte b1, Value.Byte b2 -> b1 = b2
                | Value.Char c1, Value.Char c2 -> c1 = c2
                | Value.Decimal d1, Value.Decimal d2 -> d1 = d2
                | Value.Double d1, Value.Double d2 -> d1 = d2
                | Value.Float f1, Value.Float f2 -> f1 = f2
                | Value.Guid g1, Value.Guid g2 -> g1 = g2
                | Value.Int i1, Value.Int i2 -> i1 = i2
                | Value.Long l1, Value.Long l2 -> l1 = l2
                | Value.Short s1, Value.Short s2 -> s1 = s2
                | Value.String s1, Value.String s2 -> String.Equals(s1, s2, StringComparison.Ordinal)
                | Value.DateTime dt1, Value.DateTime dt2 -> dt1 = dt2
                | Value.Option o1, Value.Option o2 ->
                    match o1, o2 with
                    | Some sv1, Some sv2 -> handler sv1 sv2
                    | None, None -> true
                    | _ -> false
                | _ -> false

            handler v value

        member v.IsStringMatch(value: Value, comparison) =
            String.Equals(v.GetString(), value.GetString(), comparison)
