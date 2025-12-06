using System;

namespace Alkampfer.Assistant.Interfaces;

public abstract class Identity : IEquatable<Identity>
{
    /// <summary>
    /// Gets the prefix for the specified identity type.
    /// The prefix is derived from the type name by removing the "Id" suffix if present.
    /// </summary>
    /// <param name="identityType">The identity type</param>
    /// <returns>The prefix for the identity type</returns>
    public static string GetPrefix(Type identityType)
    {
        var typeName = identityType.Name;
        // Remove "Id" suffix if present
        if (typeName.EndsWith("Id", StringComparison.Ordinal) && typeName.Length > 2)
        {
            return typeName[..^2];
        }
        return typeName;
    }

    protected Identity(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identity value cannot be null or empty", nameof(value));

        // Parse the value to extract prefix and numeric id
        var parts = value.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid identity format. Expected 'prefix/numericId', got '{value}'", nameof(value));

        var parsedPrefix = parts[0];
        if (string.IsNullOrWhiteSpace(parsedPrefix))
            throw new ArgumentException($"Prefix cannot be null or empty in identity '{value}'", nameof(value));

        if (!long.TryParse(parts[1], out var numericId))
            throw new ArgumentException($"Invalid numeric id in identity '{value}'", nameof(value));

        if (numericId < 0)
            throw new ArgumentException($"Numeric id must be non-negative in identity '{value}'", nameof(value));

        // Validate that the parsed prefix matches the expected prefix (case-insensitive)
        var expectedPrefix = GetPrefix(GetType());
        if (!string.Equals(parsedPrefix, expectedPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Invalid prefix in identity '{value}'. Expected '{expectedPrefix}' but got '{parsedPrefix}'",
                nameof(value));

        NumericId = numericId;

        // Always store the value with the correct prefix casing
        Value = $"{expectedPrefix}/{numericId}";
    }

    protected Identity(long numericId)
    {
        if (numericId < 0)
            throw new ArgumentException("Numeric id must be non-negative", nameof(numericId));

        NumericId = numericId;
        Value = $"{GetPrefix(GetType())}/{numericId}";
    }

    public string Value { get; }
    public long NumericId { get; }

    public override string ToString() => Value;

    public override bool Equals(object? obj)
    {
        return obj is Identity other && Equals(other);
    }

    public bool Equals(Identity? other)
    {
        return other != null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
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
