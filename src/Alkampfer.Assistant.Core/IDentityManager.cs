using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Alkampfer.Assistant.Core;

public class IdentityManager
{
    private readonly ConcurrentDictionary<string, Type> _prefixToType = new();
    private readonly ConcurrentDictionary<Type, string> _typeToPrefix = new();

    public void RegisterIdentityType<TIdentity>() where TIdentity : Identity
    {
        var type = typeof(TIdentity);
        // Find the Prefix property (protected abstract in base, must be implemented in derived)
        var prefixProp = type.GetProperty("Prefix", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (prefixProp == null)
            throw new InvalidOperationException($"Type {type.Name} does not implement Prefix property");

        // Create a dummy instance to get the prefix
        // Try to find a constructor with a single long parameter
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(long) }, null);
        if (ctor == null)
            throw new InvalidOperationException($"Type {type.Name} must have a constructor with a single long parameter");

        var instance = (Identity)ctor.Invoke(new object[] { 0L });
        var prefix = (string)prefixProp.GetValue(instance)!;
        if (string.IsNullOrWhiteSpace(prefix))
            throw new InvalidOperationException($"Prefix for type {type.Name} cannot be null or empty");

        _prefixToType[prefix] = type;
        _typeToPrefix[type] = prefix;
    }

    public Identity Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identity value cannot be null or empty", nameof(value));

        var parts = value.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid identity format. Expected 'prefix/numericId', got '{value}'", nameof(value));

        var prefix = parts[0];
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException($"Prefix cannot be null or empty in identity '{value}'", nameof(value));
            
        if (!_prefixToType.TryGetValue(prefix, out var type))
            throw new InvalidOperationException($"Unknown identity prefix: '{prefix}'");

        // Find constructor with a single string parameter
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
        if (ctor == null)
            throw new InvalidOperationException($"Type {type.Name} must have a constructor with a single string parameter");

        try
        {
            return (Identity)ctor.Invoke(new object[] { value });
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }
}
