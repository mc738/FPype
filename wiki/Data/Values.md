<meta name="wikd:title" content="Values">
<meta name="wikd:name" content="data-values">
<meta name="wikd:order" content="2">
<meta name="wikd:icon" content="fas fa-plug">

# Values

The following value types are supported in `FPype`:

* `Boolean`
* `Byte`
* `Char`
* `Decimal`
* `Double`
* `Float`
* `Int`
* `Short`
* `Long`
* `String`
* `DateTime`
* `Guid`
* `Optional`

Each one has a corresponding `BaseType`.

## Boolean

A value represent true or false.

* Base type: `Boolean`
* Type id: `bool`
* Type code: `1`
* F# type: `bool`
* CLI equivalent: `System.Boolean`
* Value length : 1 byte

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.boolean?view=net-6.0).

## Byte

A value representing a single byte.

* Base type: `Byte`
* Type id: `byte`
* Type code: `2`
* F# type: `byte`
* CLI equivalent: `System.Byte`
* Value length: 1 byte
* Minimum value: 0
* Maximum value: 255

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.byte?view=net-6.0).

## Char

A value representing a single character.

* Base type: `char`
* Type id: `char`
* Type code: `3`
* F# type: `char`
* CLI equivalent: `System.Char`
* Value length: 2 bytes

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.char?view=net-6.0).

## Decimal

A value representing a decimal value.

* Base type: `decimal`
* Type id: `decimal`
* Type code: `4`
* F# type: `decimal`
* CLI equivalent: `System.Decimal`
* Value length: 16 bytes
* Minimum value: -79228162514264337593543950335
* Maximum value: 79228162514264337593543950335

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.decimal?view=net-6.0).

### Notes

The serialized format of `decimals` are made up of 4 `int32`.
See [here](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.-ctor?view=net-6.0#system-decimal-ctor(system-int32())) for more details.

## Double

A value representing a double (float) value.

* Base type: `Double`
* Type id: `double`
* Type code: `5`
* F# type: `double`
* CLI equivalent: `System.Double`
* Value length: 8 bytes
* Minimum value: -1.797693135e+308
* Maximum value: 1.797693135e+308

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.double?view=net-6.0).

## Float

A value representing a single (float) value.

* Base type: `float`
* Type id: `float`
* Type code: `6`
* F# type: `float32`
* CLI equivalent: `System.Single`
* Value length: 4 bytes
* Minimum value: -3.402823466e+38f
* Maximum value: 3.402823466e+38f

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.single?view=net-6.0).

## Int

A value representing a 32 bit integer value.

* Base type: `Int`
* Type id: `int`
* Type code: `7`
* F# type: `int`
* CLI equivalent: `System.Int32`
* Value length: 4 bytes
* Minimum value: -2147483648
* Maximum value: 2147483647

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.int32?view=net-6.0).

## Short

A value representing a 16 bit integer value.

* Base type: `Short`
* Type id: `short`
* Type code: `8`
* F# type: `int16`
* CLI equivalent: `System.Int16`
* Value length: 2 bytes
* Minimum value: -32768
* Maximum value: 32767

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.int16?view=net-6.0).

## Long

A value representing a 64 bit integer value.

* Base type: `Long`
* Type id: `long`
* Type code: `9`
* F# type: `int64`
* CLI equivalent: `System.Int64`
* Value length: 8 bytes
* Minimum value: -9223372036854775808
* Maximum value: 9223372036854775807

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.int64?view=net-6.0).

## String

A value representing a string of characters.

* Base type: `String`
* Type id: `string`
* Type code: `10`
* F# type: `string`
* CLI equivalent: `System.String`
* Value length: variable (4 bytes + utf 8 encoded length)

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.string?view=net-6.0).

### Notes

Strings are variable length and serialized in the format `[string length] + [utf8 encoded string]`.
This means there is a maximum string size of 2147483647 bytes when serializing.

## DateTime

A value representing a date time.

* Base type: `DateTime`
* Type id: `datetime`
* Type code: `11`
* F# type: `DateTime`
* CLI equivalent: `System.DateTime`
* Value length: 8 bytes

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-6.0).

## Guid

A value representing a guid.

* Base type: `Guid`
* Type id: `guid`
* Type code: `12`
* F# type: `Guid`
* CLI equivalent: `System.Guid`
* Value length: 16 bytes

For more information on the underlying type, see [here](https://learn.microsoft.com/en-us/dotnet/api/system.guid?view=net-6.0).

## Option (some)

* Base type `Option`
* Type id: none
* Type code : `13`
* Value length: variable

### Notes

If an optional value is `some` then the value is stored after. The length therefore depends on the actual value.

## Option (none)

* Base type `Option`
* Type id: none
* Type code: `14`
* Value length: None