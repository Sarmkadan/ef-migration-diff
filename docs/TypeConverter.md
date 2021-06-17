# TypeConverter

A utility class providing type conversion methods for common scenarios, including primitive type conversions, enum parsing, and serialization between objects and dictionaries. Designed to simplify data transformation tasks, particularly in Entity Framework migration contexts where type flexibility is required.

## API

### `public static T? ConvertTo<T>(object? value)`
Converts the given value to the specified type `T`. Supports primitive types, enums, and nullable types.

**Parameters:**
- `value` (`object?`): The value to convert. May be `null`.

**Returns:**
- `T?`: The converted value, or `null` if the conversion fails or the input is `null`.

**Throws:**
- `InvalidCastException`: If the conversion is not supported for the given types.
- `ArgumentException`: If the input is an enum value not defined in `T`.

---

### `public static bool TryConvertTo<T>(object? value, out T? result)`
Attempts to convert the given value to the specified type `T`. Returns `false` if the conversion fails.

**Parameters:**
- `value` (`object?`): The value to convert. May be `null`.
- `result` (`out T?`): The converted value, or `default(T)` if the conversion fails.

**Returns:**
- `bool`: `true` if the conversion succeeds; otherwise, `false`.

**Throws:**
- None.

---

### `public static T? StringToEnum<T>(string? value)`
Parses a string into an enum value of type `T`. Case-insensitive by default.

**Parameters:**
- `value` (`string?`): The string representation of the enum value. May be `null`.

**Returns:**
- `T?`: The parsed enum value, or `null` if the input is `null` or invalid.

**Throws:**
- `ArgumentException`: If `T` is not an enum type or the input string does not match any enum value.

---

### `public static string EnumToString<T>(T value)`
Converts an enum value of type `T` to its string representation.

**Parameters:**
- `value` (`T`): The enum value to convert. Must not be `null`.

**Returns:**
- `string`: The string representation of the enum value.

**Throws:**
- `ArgumentNullException`: If `value` is `null`.
- `ArgumentException`: If `T` is not an enum type.

---

### `public static Dictionary<string, object?> ObjectToDictionary(object obj)`
Serializes an object into a dictionary of property names and values. Supports nested objects and collections.

**Parameters:**
- `obj` (`object`): The object to serialize. Must not be `null`.

**Returns:**
- `Dictionary<string, object?>`: A dictionary where keys are property names and values are the corresponding property values.

**Throws:**
- `ArgumentNullException`: If `obj` is `null`.

---

### `public static T? DictionaryToObject<T>(Dictionary<string, object?> dict) where T : class, new()`
Deserializes a dictionary into an object of type `T`. Property names in the dictionary must match the target object's property names.

**Parameters:**
- `dict` (`Dictionary<string, object?>`): The dictionary to deserialize. May be `null`.

**Returns:**
- `T?`: The deserialized object, or `null` if `dict` is `null`.

**Throws:**
- `ArgumentException`: If a property in `dict` does not exist on `T` or the conversion fails.
- `InvalidOperationException`: If `T` cannot be instantiated (e.g., lacks a parameterless constructor).

---

### `public static object? ConvertTo(Type targetType, object? value)`
Converts the given value to the specified `targetType`. Supports primitive types, enums, and nullable types.

**Parameters:**
- `targetType` (`Type`): The target type to convert to. Must not be `null`.
- `value` (`object?`): The value to convert. May be `null`.

**Returns:**
- `object?`: The converted value, or `null` if the conversion fails or the input is `null`.

**Throws:**
- `ArgumentNullException`: If `targetType` is `null`.
- `InvalidCastException`: If the conversion is not supported for the given types.
- `ArgumentException`: If the input is an enum value not defined in `targetType`.

---

### `public static bool CanConvertTo<T>(object? value)`
Determines whether the given value can be converted to type `T`.

**Parameters:**
- `value` (`object?`): The value to check. May be `null`.

**Returns:**
- `bool`: `true` if the conversion is possible; otherwise, `false`.

**Throws:**
- None.

## Usage

### Example 1: Converting Between Primitives and Enums
