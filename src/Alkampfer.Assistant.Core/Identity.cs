using System;

namespace Alkampfer.Assistant.Core;

public abstract class Identity : IEquatable<Identity>
{
    protected Identity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identity value cannot be null or empty", nameof(value));

        Value = value;
        
        // Parse the value to extract prefix and numeric id
        var parts = value.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid identity format. Expected 'prefix/numericId', got '{value}'", nameof(value));

        var prefix = parts[0];
        if (!long.TryParse(parts[1], out var numericId))
            throw new ArgumentException($"Invalid numeric id in identity '{value}'", nameof(value));

        NumericId = numericId;
    }

    protected Identity(long numericId)
    {
        if (numericId < 0)
            throw new ArgumentException("Numeric id must be non-negative", nameof(numericId));

        NumericId = numericId;
        Value = $"{Prefix}/{numericId}";
    }

    public string Value { get; }
    public long NumericId { get; }

    protected abstract string Prefix { get; }


    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is Identity other && Equals(other);
    }

    public bool Equals(Identity? other)
    {
        return other != null && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(Identity? left, Identity? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(Identity? left, Identity? right)
    {
        return !(left == right);
    }
}
