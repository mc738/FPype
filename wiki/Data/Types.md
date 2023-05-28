<meta name="wikd:title" content="Base types">
<meta name="wikd:name" content="data-types">
<meta name="wikd:order" content="1">
<meta name="wikd:icon" content="fas fa-plug">

# Base types

The following base types are supported in `FPype`:

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

## Boolean

A type represent true or false.

* Id: `bool`
* Type code: `1`

## Byte

A type representing a single byte.

* Id: `byte`
* Type code: `2`
* Minimum value: 0
* Maximum value: 255

## Char

A type representing a single character.

* Id: `char`
* Type code: `3`

## Decimal

A type representing a decimal value.

* Id: `decimal`
* Type code: `4`
* Minimum value: -79228162514264337593543950335
* Maximum value: 79228162514264337593543950335

## Double

A type representing a double (float) value.

* Id: `double`
* Type code: `5`
* Value length: 8 bytes
* Minimum value: -1.797693135e+308
* Maximum value: 1.797693135e+308

## Float

A type representing a single (float) value.

* Id: `float`
* Type code: `6`
* Value length: 4 bytes
* Minimum value: -3.402823466e+38f
* Maximum value: 3.402823466e+38f

## Int

A type representing a 32 bit integer value.

* Id: `int`
* Type code: `7`
* Value length: 4 bytes
* Minimum value: -2147483648
* Maximum value: 2147483647

## Short

A type representing a 16 bit integer value.

* Id: `short`
* Type code: `8`
* Value length: 2 bytes
* Minimum value: -32768
* Maximum value: 32767

## Long

A type representing a 64 bit integer value.

* Id: `long`
* Type code: `9`
* Value length: 8 bytes
* Minimum value: -9223372036854775808
* Maximum value: 9223372036854775807

## String

A type representing a string of characters.

* Id: `string`
* Type code: `10`
* Value length: variable (4 bytes + utf 8 encoded length)

## DateTime

A type representing a date time.

* Id: `datetime`
* Type code: `11`
* Value length: 8 bytes

## Guid

A type representing a guid.

* Id: `guid`
* Type code: `12`
* Value length: 16 bytes

## Option (some)

* Type code : `13`
* Value length: variable

### Notes

If an optional value is `some` then the value is stored after. The length therefore depends on the actual value.

## Option (none)

* Type code: `14`
* Value length: None
