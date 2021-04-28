#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Gets all public methods from a type.
    /// </summary>
    public static IEnumerable<MethodInfo> GetPublicMethods(this Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
    /// </summary>
    public static bool ImplementsInterface<TInterface>(this Type type) where TInterface : class
    {
        return typeof(TInterface).IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks if a type is a simple/scalar type (not a complex object).
    /// </summary>
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid) ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                IsSimpleType(type.GetGenericArguments()[0]));
    }

    /// <summary>
    /// Gets the value of a property from an object, returns null if property doesn't exist.
    /// </summary>
    public static object? GetPropertyValue(this object? obj, string propertyName)
    {
        if (obj is null || string.IsNullOrEmpty(propertyName))
            return null;

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Sets the value of a property on an object.
    /// </summary>
    public static bool SetPropertyValue(this object? obj, string propertyName, object? value)
    {
        if (obj is null || string.IsNullOrEmpty(propertyName))
            return false;

        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
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
    public static bool HasParameterlessConstructor(this Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) is not null;
    }

    /// <summary>
    /// Creates an instance of a type using its parameterless constructor.
    /// </summary>
    public static object? CreateInstance(this Type type)
    {
        if (!type.HasParameterlessConstructor())
            return null;

        return Activator.CreateInstance(type);
    }

    /// <summary>
    /// Gets all types that implement or inherit from a base type.
    /// </summary>
    public static IEnumerable<Type> GetImplementations(this Type baseType, Assembly? assembly = null)
    {
        var targetAssembly = assembly ?? Assembly.GetAssembly(baseType);
        if (targetAssembly is null)
            return Enumerable.Empty<Type>();

        return targetAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && baseType.IsAssignableFrom(t));
    }

    /// <summary>
    /// Gets the friendly name of a type (handles generics).
    /// </summary>
    public static string GetFriendlyName(this Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name.Substring(0, type.Name.LastIndexOf('`'));
        var args = type.GetGenericArguments().Select(t => t.GetFriendlyName());
        return $"{name}<{string.Join(", ", args)}>";
    }
}
