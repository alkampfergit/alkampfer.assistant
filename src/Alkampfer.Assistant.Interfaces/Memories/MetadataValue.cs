using System;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a metadata value that can be a string, int, double, bool, or DateTime.
/// </summary>
public class MetadataValue
{
    private readonly object _value;
    private readonly MetadataValueType _type;

    private MetadataValue(object value, MetadataValueType type)
    {
        _value = value;
        _type = type;
    }

    public static implicit operator MetadataValue(string value) => new(value, MetadataValueType.String);
    public static implicit operator MetadataValue(int value) => new(value, MetadataValueType.Int);
    public static implicit operator MetadataValue(double value) => new(value, MetadataValueType.Double);
    public static implicit operator MetadataValue(bool value) => new(value, MetadataValueType.Boolean);
    public static implicit operator MetadataValue(DateTime value) => new(value, MetadataValueType.DateTime);

    /// <summary>
    /// Attempts to retrieve the value as a string.
    /// </summary>
    /// <returns>The string value if the type matches, otherwise null.</returns>
    public string? AsString() => _type == MetadataValueType.String ? (string)_value : null;

    /// <summary>
    /// Attempts to retrieve the value as an int.
    /// </summary>
    /// <returns>The int value if the type matches, otherwise null.</returns>
    public int? AsInt() => _type == MetadataValueType.Int ? (int)_value : null;

    /// <summary>
    /// Attempts to retrieve the value as a double.
    /// </summary>
    /// <returns>The double value if the type matches, otherwise null.</returns>
    public double? AsDouble() => _type == MetadataValueType.Double ? (double)_value : null;

    /// <summary>
    /// Attempts to retrieve the value as a bool.
    /// </summary>
    /// <returns>The bool value if the type matches, otherwise null.</returns>
    public bool? AsBool() => _type == MetadataValueType.Boolean ? (bool)_value : null;

    /// <summary>
    /// Attempts to retrieve the value as a DateTime.
    /// </summary>
    /// <returns>The DateTime value if the type matches, otherwise null.</returns>
    public DateTime? AsDateTime() => _type == MetadataValueType.DateTime ? (DateTime)_value : null;

    /// <summary>
    /// Gets the raw value as an object.
    /// </summary>
    public object Value => _value;

    private enum MetadataValueType
    {
        String,
        Int,
        Double,
        Boolean,
        DateTime
    }
}
