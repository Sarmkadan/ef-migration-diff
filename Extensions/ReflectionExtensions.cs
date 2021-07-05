#nullable enable
using System;
using System.Reflection;

namespace EfMigrationDiff.Extensions;

/// <summary>
/// Extension methods for reflection operations.
/// Provides utilities for type inspection, property access, and dynamic invocation.
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// Gets all public properties from a type.
    /// </summary>
    /// <param name="type">The type to get properties from.</param>
    /// <returns>An enumerable of <see cref="PropertyInfo"/> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Gets all public methods from a type.
    /// </summary>
    /// <param name="type">The type to get methods from.</param>
    /// <returns>An enumerable of <see cref="MethodInfo"/> objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static IEnumerable<MethodInfo> GetPublicMethods(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to check for.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type implements the interface; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool ImplementsInterface<TInterface>(this Type type) where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(type);
        return typeof(TInterface).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks if a type is a simple/scalar type (not a complex object).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type is a simple type; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool IsSimpleType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && IsSimpleType(type.GetGenericArguments()[0]));
    }

    /// <summary>
    /// Gets the value of a property from an object, returns null if property doesn't exist.
    /// </summary>
    /// <param name="obj">The object to get the property value from.</param>
    /// <param name="propertyName">The name of the property to get.</param>
    /// <returns>The property value, or <see langword="null"/> if the property doesn't exist or inputs are invalid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    public static object? GetPropertyValue(this object? obj, string propertyName)
    {
        if (obj is null || string.IsNullOrEmpty(propertyName))
            return null;

        var property = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets the value of a property on an object.
    /// </summary>
    /// <param name="obj">The object to set the property value on.</param>
    /// <param name="propertyName">The name of the property to set.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><see langword="true"/> if the property was successfully set; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    public static bool SetPropertyValue(this object? obj, string propertyName, object? value)
    {
        if (obj is null || string.IsNullOrEmpty(propertyName))
            return false;

        var property = obj.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
        if (property?.CanWrite ?? false)
        {
            property.SetValue(obj, value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all property values from an object as a dictionary.
    /// </summary>
    /// <param name="obj">The object to get property values from.</param>
    /// <param name="includeNulls">Whether to include null values in the dictionary.</param>
    /// <returns>A dictionary mapping property names to their values.</returns>
    public static Dictionary<string, object?> GetPropertyDictionary(this object? obj, bool includeNulls = false)
    {
        var dict = new Dictionary<string, object?>();

        if (obj is null)
            return dict;

        var properties = obj.GetType().GetPublicProperties();
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            if (includeNulls || value is not null)
            {
                dict[prop.Name] = value;
            }
        }

        return dict;
    }

    /// <summary>
    /// Checks if a type has a parameterless constructor.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type has a parameterless constructor; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool HasParameterlessConstructor(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetConstructor(Type.EmptyTypes) is not null;
    }

    /// <summary>
    /// Creates an instance of a type using its parameterless constructor.
    /// </summary>
    /// <param name="type">The type to create an instance of.</param>
    /// <returns>A new instance of the type, or <see langword="null"/> if the type doesn't have a parameterless constructor.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static object? CreateInstance(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.HasParameterlessConstructor()
            ? Activator.CreateInstance(type)
            : null;
    }

    /// <summary>
    /// Gets all types that implement or inherit from a base type.
    /// </summary>
    /// <param name="baseType">The base type to find implementations of.</param>
    /// <param name="assembly">The assembly to search in. If <see langword="null"/>, uses the assembly containing <paramref name="baseType"/>.</param>
    /// <returns>An enumerable of types that implement or inherit from the base type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="baseType"/> is <see langword="null"/>.</exception>
    public static IEnumerable<Type> GetImplementations(this Type baseType, Assembly? assembly = null)
    {
        ArgumentNullException.ThrowIfNull(baseType);

        var targetAssembly = assembly ?? Assembly.GetAssembly(baseType);
        if (targetAssembly is null)
            return Enumerable.Empty<Type>();

        return targetAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t));
    }

    /// <summary>
    /// Gets the friendly name of a type (handles generics).
    /// </summary>
    /// <param name="type">The type to get the friendly name for.</param>
    /// <returns>The friendly name of the type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
    public static string GetFriendlyName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name.Substring(0, type.Name.LastIndexOf('`'));
        var args = type.GetGenericArguments().Select(t => t.GetFriendlyName());
        return $"{name}<{string.Join(", ", args)}>";
    }
}