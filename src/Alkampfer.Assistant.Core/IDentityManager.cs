using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Fasterflect;

namespace Alkampfer.Assistant.Core;

public class IdentityManager
{
    private readonly ConcurrentDictionary<string, Type> _prefixToType = new();
    private readonly ConcurrentDictionary<Type, string> _typeToPrefix = new();

    public void RegisterIdentityType<TIdentity>() where TIdentity : Identity
    {
        var type = typeof(TIdentity);
        
        // Ensure the type has a constructor with a single long parameter
        var ctor = type.Constructor(typeof(long));
        if (ctor == null)
            throw new InvalidOperationException($"Identity type '{type.Name}' must have a constructor with a single long parameter");

        // Create an instance using the long constructor to get the prefix
        var instance = (Identity)type.CreateInstance(0L);
        var prefix = (string)instance.GetPropertyValue("Prefix")!;

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

        // Use Fasterflect to create instance with string constructor
        try
        {
            return (Identity)type.CreateInstance(value);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }
}
