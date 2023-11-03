namespace FPype.Core

open System
open System.Globalization
open System.Text
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
                .Match(name, "(?<=Microsoft.FSharp.Core.FSharpOption`1\[\[).+?(?=\,)")
                .Success

        let getOptionType name =
            // Maybe a bit wasteful doing this twice.
            Regex
                .Match(name, "(?<=Microsoft.FSharp.Core.FSharpOption`1\[\[).+?(?=\,)")
                .Value

        let convert (value: obj) (target: Type) =
            try
                Convert.ChangeType(value, target) |> Ok
            with ex ->
                Error ex.Message

    /// <summary>
    /// An internal DU for representing base types.
    /// </summary>
    [<RequireQualifiedAccess>]
    type BaseType =
        /// <summary>
        /// A boolean type. Equivalent to System.Boolean.
        /// </summary>
        | Boolean
        /// <summary>
        /// A byte type. Equivalent to System.Byte.
        /// </summary>
        | Byte
        /// <summary>
        /// A char type. Equivalent to System.Char.
        /// </summary>
        | Char
        /// <summary>
        /// A decimal type. Equivalent to System.Decimal.
        /// </summary>
        | Decimal
        /// <summary>
        /// A double type. Equivalent to System.Double.
        /// </summary>
        | Double
        /// <summary>
        /// A float type. Equivalent to System.Single.
        /// </summary>
        | Float
        /// <summary>
        /// A int type. Equivalent to System.Int32.
        /// </summary>
        | Int
        /// <summary>
        /// A short int type. Equivalent to System.Int16.
        /// </summary>
        | Short
        /// <summary>
        /// A long int type. Equivalent to System.Int64.
        /// </summary>
        | Long
        /// <summary>
        /// A string type. Equivalent to System.String.
        /// </summary>
        | String
        /// <summary>
        /// A datetime type. Equivalent to System.DateTime.
        /// </summary>
        | DateTime
        /// <summary>
        /// A guid type. Equivalent to System.Guid.
        /// </summary>
        | Guid
        /// <summary>
        /// An optional type.
        /// </summary>
        | Option of BaseType

        /// <summary>
        /// Try and create a BaseType from a .net type name. For example System.String.
        /// </summary>
        /// <param name="name">The type name.</param>
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

        /// <summary>
        /// Try from a .net type.
        /// </summary>
        /// <param name="typeInfo">The type.</param>
        static member TryFromType(typeInfo: Type) = BaseType.TryFromName(typeInfo.FullName)

        /// <summary>
        /// Create a base type from .net type name.
        /// Internally this calls TryFromName().
        /// If the call fails this defaults to BaseType.String
        /// </summary>
        /// <param name="name">The type name.</param>
        static member FromName(name: string) =
            match BaseType.TryFromName name with
            | Ok st -> st
            | Error _ -> BaseType.String

        /// <summary>
        /// Create a base type from .net type.
        /// Internally this calls TryFromName().
        /// If the call fails this defaults to BaseType.String
        /// </summary>
        /// <param name="typeInfo">The type.</param>
        static member FromType(typeInfo: Type) = BaseType.FromName(typeInfo.FullName)

        /// <summary>
        /// Type and get a base type from a byte code.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isOptional"></param>
        static member TryFromByte(value: byte, isOptional: bool) =
            match value with
            | 1uy -> Ok Boolean
            | 2uy -> Ok Byte
            | 3uy -> Ok Char
            | 4uy -> Ok Decimal
            | 5uy -> Ok Double
            | 6uy -> Ok Float
            | 7uy -> Ok Int
            | 8uy -> Ok Short
            | 9uy -> Ok Long
            | 10uy -> Ok String
            | 11uy -> Ok DateTime
            | 12uy -> Ok Guid
            | _ -> Error $"Unknown base type byte: {value}"
            |> Result.map (fun bt ->
                match isOptional with
                | true -> Option bt
                | false -> bt)

        /// <summary>
        /// Get a base type from a string id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="optional"></param>
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

        /// <summary>
        /// Check if a base type is optional.
        /// </summary>
        member bt.IsOptionType() =
            match bt with
            | BaseType.Option _ -> true
            | _ -> false

        /// <summary>
        /// Convert a base type to a .net type.
        /// </summary>
        member bt.ToType() =
            let rec handler baseType =
                match baseType with
                | Boolean -> typeof<bool>
                | Byte -> typeof<byte>
                | Char -> typeof<char>
                | Decimal -> typeof<decimal>
                | Double -> typeof<double>
                | Float -> typeof<float>
                | Int -> typeof<int>
                | Short -> typeof<int16>
                | Long -> typeof<int64>
                | String -> typeof<string>
                | DateTime -> typeof<DateTime>
                | Guid -> typeof<Guid>
                | Option ibt ->
                    // TODO implement this.
                    failwith "To implement"

            handler bt

        /// <summary>
        /// Serialize a base type 
        /// </summary>
        member bt.Serialize() =
            let rec handle (baseType: BaseType) =
                match baseType with
                | BaseType.Boolean -> "bool"
                | BaseType.Byte -> "byte"
                | BaseType.Char -> "char"
                | BaseType.DateTime -> "datetime"
                | BaseType.Decimal -> "decimal"
                | BaseType.Double -> "double"
                | BaseType.Float -> "float"
                | BaseType.Int -> "int"
                | BaseType.Short -> "short"
                | BaseType.Long -> "long"
                | BaseType.String -> "string"
                | BaseType.Guid -> "uuid"
                | BaseType.Option ibt -> handle ibt

            handle bt

        /// <summary>
        /// Convert a base type to byte code
        /// </summary>
        member bt.ToByte() =
            let rec handle (baseType: BaseType) =
                match baseType with
                | Boolean -> 1uy
                | Byte -> 2uy
                | Char -> 3uy
                | Decimal -> 4uy
                | Double -> 5uy
                | Float -> 6uy
                | Int -> 7uy
                | Short -> 8uy
                | Long -> 9uy
                | String -> 10uy
                | DateTime -> 11uy
                | Guid -> 12uy
                | Option ibt -> handle ibt

            handle bt
 
    /// <summary>
    /// The result of a value coercion.
    /// </summary>
    [<RequireQualifiedAccess>]
    type CoercionResult =
        | Success of Value
        | IncompatibleType of string
        | NonScalarValue of string
        | NullValue of string
        | Failure of string

        /// <summary>
        /// Bind a result to a function.
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="cr"></param>
        static member bind (fn: Value -> CoercionResult) (cr: CoercionResult) =
            match cr with
            | Success v -> fn v
            | _ -> cr

        /// <summary>
        /// Map a result to a function.
        /// </summary>
        /// <param name="fn"></param>
        /// <param name="cr"></param>
        static member map (fn: Value -> Value) (cr: CoercionResult) =
            match cr with
            | Success v -> fn v |> CoercionResult.Success
            | _ -> cr

    /// <summary>
    /// A FPype value.
    /// </summary>
    and [<RequireQualifiedAccess>] Value =
        | Boolean of bool
        | Byte of byte
        | Char of char
        | Decimal of decimal
        | Double of double
        | Float of float32
        | Int of int
        | Short of int16
        | Long of int64
        | String of string
        | DateTime of DateTime
        | Guid of Guid
        | Option of Value option

        /// <summary>
        /// Try and deserialize a value from a byte array.
        /// </summary>
        /// <param name="data"></param>
        static member TryDeserialize(data: byte array) =
            let rec handler (data: byte array) =
                match data |> Array.tryHead with
                | Some 1uy ->
                    // len 1
                    let t = data |> Array.tail

                    match t.Length >= 1 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 1

                        (BitConverter.ToBoolean(b) |> Value.Boolean, r) |> Ok
                    | false -> Error "Data is too short"
                | Some 2uy ->
                    // len 1
                    let t = data |> Array.tail

                    match t.Length >= 1 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 1

                        // NOTE - b should always be 1 long so this should be fine.
                        (b[0] |> Value.Byte, r) |> Ok
                    | false -> Error "Data is too short"
                | Some 3uy ->
                    // len 2
                    let t = data |> Array.tail

                    match t.Length >= 2 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 2

                        (BitConverter.ToChar(b) |> Value.Char, r) |> Ok
                    | false -> Error "Data is too short"
                | Some 4uy ->
                    // len 16
                    let t = data |> Array.tail

                    match t.Length >= 16 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 16


                        (System.Decimal(b |> Array.splitInto 4 |> Array.map (BitConverter.ToInt32))
                         |> Decimal,
                         r)
                        |> Ok
                    | false -> Error "Data is too short"
                | Some 5uy ->
                    // len 8
                    let t = data |> Array.tail

                    match t.Length >= 8 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 8

                        (BitConverter.ToDouble(b) |> Value.Double, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 6uy ->
                    // len 4
                    let t = data |> Array.tail

                    match t.Length >= 4 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 4

                        (BitConverter.ToSingle(b) |> Value.Float, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 7uy ->
                    // len 4
                    let t = data |> Array.tail

                    match t.Length >= 4 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 4

                        (BitConverter.ToInt32(b) |> Value.Int, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 8uy ->
                    // len 2
                    let t = data |> Array.tail

                    match t.Length >= 2 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 2

                        (BitConverter.ToInt16(b) |> Value.Short, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 9uy ->
                    // len 8
                    let t = data |> Array.tail

                    match t.Length >= 8 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 8

                        (BitConverter.ToInt64(b) |> Value.Long, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 10uy ->
                    // len 4 + that value
                    let t = data |> Array.tail

                    match t.Length >= 8 with
                    | true ->
                        let (b, r1) = t |> Array.splitAt 4
                        let len = BitConverter.ToInt32 b

                        match r1.Length >= len with
                        | true ->
                            let (s, r2) = r1 |> Array.splitAt len

                            Ok(Encoding.UTF8.GetString s |> String, r2)

                        | false -> Error "Data is too short"
                    | false -> Error "Data is too short"
                | Some 11uy ->
                    // len 8
                    let t = data |> Array.tail

                    match t.Length >= 8 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 8

                        (BitConverter.ToInt64(b) |> DateTime.FromBinary |> Value.DateTime, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 12uy ->
                    // len 16
                    let t = data |> Array.tail

                    match t.Length >= 16 with
                    | true ->
                        let (b, r) = t |> Array.splitAt 16

                        (System.Guid(b) |> Value.Guid, r) |> Ok
                    | false -> Error "Data is too short"

                | Some 13uy ->
                    handler (data |> Array.tail)
                    |> Result.map (fun (b, r) -> Value.Option(Some b), r)

                | Some 14uy ->
                    // len 0
                    (Value.Option None, data |> Array.tail) |> Ok
                | Some v -> Error $"Unknown type ({v})"
                | None -> Error "Missing value type byte"

            handler data

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
            | BaseType.Float -> handler1 (typeof<float32>) (fun o -> o :?> float32 |> Value.Float)
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
                | true, TypeHelpers.SomeObj(v1) ->
                    match t with
                    | BaseType.Boolean -> handler v1 typeof<bool> (fun o -> o :?> bool |> Value.Boolean |> toOption)
                    | BaseType.Byte -> handler v1 typeof<byte> (fun o -> o :?> byte |> Value.Byte |> toOption)
                    | BaseType.Char -> handler v1 typeof<char> (fun o -> o :?> char |> Value.Char |> toOption)
                    | BaseType.Decimal ->
                        handler v1 typeof<decimal> (fun o -> o :?> decimal |> Value.Decimal |> toOption)
                    | BaseType.Double -> handler v1 typeof<double> (fun o -> o :?> double |> Value.Double |> toOption)
                    | BaseType.Float -> handler v1 typeof<float> (fun o -> o :?> float32 |> Value.Float |> toOption)
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
                    | JsonValueKind.Number -> json.GetSingle() |> Value.Float |> CoercionResult.Success
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
                    match Single.TryParse str with
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

        /// <summary>
        /// Box a value to an obj. This is mainly used for internal use.
        /// </summary>
        /// <param name="stringToReadOnlyMemory">
        /// Special handling for strings that coverts them to ReadOnlyMemory and then boxes them.
        /// This is mainly used for created dynamic runtime objects and often won't be needed in other cases.
        /// </param>
        member fv.Box(?stringToReadOnlyMemory: bool) =
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
                | String v ->
                    match stringToReadOnlyMemory with
                    | Some true -> ReadOnlyMemory<Char>(v |> Seq.toArray) |> box
                    | Some false
                    | None -> v |> box
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

        member fv.GetFloat() =
            let rec handler (value: Value) =
                match value with
                | Boolean _ -> 0.
                | Byte v -> float v
                | Char _ -> 0.
                | Decimal v -> float v
                | Double v -> float v
                | Float v -> float v
                | Int v -> float v
                | Short v -> float v
                | Long v -> float v
                | String v -> float v
                | DateTime _ -> 0.
                | Guid _ -> 0.
                | Option v ->
                    match v with
                    | Some v -> handler v
                    | None -> 0.

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

        member v.GetBaseType() =
            let rec handler (value: Value) =
                match value with
                | Boolean _ -> BaseType.Boolean
                | Byte _ -> BaseType.Byte
                | Char _ -> BaseType.Char
                | Decimal _ -> BaseType.Decimal
                | Double _ -> BaseType.Double
                | Float _ -> BaseType.Float
                | Int _ -> BaseType.Int
                | Short _ -> BaseType.Short
                | Long _ -> BaseType.Long
                | String _ -> BaseType.String
                | DateTime _ -> BaseType.DateTime
                | Guid _ -> BaseType.Guid
                | Option iv -> failwith "TODO - implement"

            //handler iv |> BaseType.Option

            handler v

        member v.Serialize() =
            let rec handler (value: Value) =
                match value with
                | Boolean v -> [| 1uy; yield! BitConverter.GetBytes(v) |]
                | Byte v -> [| 2uy; v |]
                | Char v -> [| 3uy; yield! BitConverter.GetBytes(v) |]
                | Decimal v ->
                    // See - https://learn.microsoft.com/en-us/dotnet/api/system.decimal.getbits
                    // First break the bytes down into parts.
                    // Then convert parts to bytes.
                    let parts = Decimal.GetBits(v) |> Array.collect BitConverter.GetBytes

                    [| 4uy; yield! parts |]
                | Double v -> [| 5uy; yield! BitConverter.GetBytes(v) |]
                | Float v -> [| 6uy; yield! BitConverter.GetBytes v |]
                | Int v -> [| 7uy; yield! BitConverter.GetBytes v |]
                | Short v -> [| 8uy; yield! BitConverter.GetBytes v |]
                | Long v -> [| 9uy; yield! BitConverter.GetBytes v |]
                | String v ->
                    let bytes = Encoding.UTF8.GetBytes v
                    // Note - special handling. The length is stored after the type.
                    [| 10uy; yield! BitConverter.GetBytes bytes.Length; yield! bytes |]
                | DateTime v -> [| 11uy; yield! v.ToBinary() |> BitConverter.GetBytes |]
                | Guid v -> [| 12uy; yield! v.ToByteArray() |]
                | Option iv ->
                    match iv with
                    | Some v -> [| 13uy; yield! handler v |]
                    | None ->
                        [|
                           // Special value to represent option - none?
                           14uy |]

            handler v
