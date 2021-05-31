# ReflectionExtensions

Utility class providing reflection-based operations for type inspection, property access, interface checking, and object instantiation.

## API

### `GetPublicProperties`

Returns all public instance properties of a given type, including those defined in base types.

**Parameters**
- `type`: `Type` – the type to inspect.

**Returns**
- `IEnumerable<PropertyInfo>` – sequence of public instance properties.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

---

### `GetPublicMethods`

Returns all public instance methods of a given type, including those defined in base types.

**Parameters**
- `type`: `Type` – the type to inspect.

**Returns**
- `IEnumerable<MethodInfo>` – sequence of public instance methods.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

---

### `ImplementsInterface<TInterface>`

Determines whether the given type implements the specified interface.

**Type Parameters**
- `TInterface`: `Type` – the interface type to check for.

**Parameters**
- `type`: `Type` – the type to inspect.

**Returns**
- `bool` – `true` if `type` implements `TInterface`; otherwise, `false`.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

---

### `IsSimpleType`

Determines whether the given type is a simple type (value types, primitives, enums, strings, or `DateTime`).

**Parameters**
- `type`: `Type` – the type to check.

**Returns**
- `bool` – `true` if `type` is a simple type; otherwise, `false`.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

---
### `GetPropertyValue`

Retrieves the value of a property from an object instance.

**Parameters**
- `obj`: `object` – the object instance.
- `propertyName`: `string` – the name of the property.

**Returns**
- `object?` – the property value, or `null` if the property does not exist or cannot be accessed.

**Exceptions**
- Throws `ArgumentNullException` if `obj` or `propertyName` is `null`.

---
### `SetPropertyValue`

Sets the value of a property on an object instance.

**Parameters**
- `obj`: `object` – the object instance.
- `propertyName`: `string` – the name of the property.
- `value`: `object?` – the value to set.

**Returns**
- `bool` – `true` if the property was found and set; otherwise, `false`.

**Exceptions**
- Throws `ArgumentNullException` if `obj` or `propertyName` is `null`.

---
### `GetPropertyDictionary`

Creates a dictionary of property names and their values for a given object.

**Parameters**
- `obj`: `object` – the object instance.

**Returns**
- `Dictionary<string, object?>` – a dictionary mapping property names to their values.

**Exceptions**
- Throws `ArgumentNullException` if `obj` is `null`.

---
### `HasParameterlessConstructor`

Determines whether the given type has a public parameterless constructor.

**Parameters**
- `type`: `Type` – the type to inspect.

**Returns**
- `bool` – `true` if a public parameterless constructor exists; otherwise, `false`.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

---
### `CreateInstance`

Creates an instance of the specified type using its parameterless constructor.

**Parameters**
- `type`: `Type` – the type to instantiate.

**Returns**
- `object?` – a new instance of the type, or `null` if instantiation fails.

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.
- Throws `MissingMethodException` if no public parameterless constructor exists.

---
### `GetImplementations`

Retrieves all non-abstract implementations of a given interface or abstract class within loaded assemblies.

**Parameters**
- `baseType`: `Type` – the interface or abstract class to find implementations for.

**Returns**
- `IEnumerable<Type>` – sequence of concrete types implementing `baseType`.

**Exceptions**
- Throws `ArgumentNullException` if `baseType` is `null`.

---
### `GetFriendlyName`

Returns a human-readable name for a type, suitable for display purposes.

**Parameters**
- `type`: `Type` – the type to format.

**Returns**
- `string` – a friendly name for the type (e.g., "List<T>" instead of "List`1").

**Exceptions**
- Throws `ArgumentNullException` if `type` is `null`.

## Usage

```csharp
// Example 1: Inspecting a type's properties and methods
var type = typeof(List<string>);
var properties = ReflectionExtensions.GetPublicProperties(type);
var methods = ReflectionExtensions.GetPublicMethods(type);

Console.WriteLine("Properties:");
foreach (var prop in properties)
{
    Console.WriteLine($"- {prop.Name}");
}

Console.WriteLine("\nMethods:");
foreach (var method in methods)
{
    Console.WriteLine($"- {method.Name}");
}

// Example 2: Creating an instance and setting properties dynamically
var personType = typeof(Person);
var person = ReflectionExtensions.CreateInstance(personType);

if (ReflectionExtensions.SetPropertyValue(person, "Name", "Alice"))
{
    var name = ReflectionExtensions.GetPropertyValue(person, "Name");
    Console.WriteLine($"Name set to: {name}");
}

var props = ReflectionExtensions.GetPropertyDictionary(person);
foreach (var kvp in props)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```

## Notes

- **Thread Safety**: All methods are stateless and thread-safe. No shared mutable state is used.
- **Performance**: Reflection operations are inherently slower than direct code. Cache results where possible.
- **Null Handling**: Methods validate arguments and throw `ArgumentNullException` for `null` inputs.
- **Property Access**: `GetPropertyValue` and `SetPropertyValue` do not handle indexers or non-public members.
- **Type Resolution**: `GetImplementations` searches only loaded assemblies; dynamically loaded assemblies may require re-scanning.
- **Friendly Names**: `GetFriendlyName` simplifies generic type names but does not handle all edge cases (e.g., nested generics).
