using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a vector record with multiple named vectors and metadata.
/// </summary>
public class VectorRecord
{
    private readonly Dictionary<string, float[]> _vectors = new();
    private readonly Dictionary<string, MetadataValue> _metadata = new();

    /// <summary>
    /// Gets the unique identifier for this record.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the document identifier (multiple records can share the same DocumentId).
    /// </summary>
    public string DocumentId { get; }

    /// <summary>
    /// Gets the collection of named vectors.
    /// </summary>
    public IReadOnlyDictionary<string, float[]> Vectors => _vectors;

    /// <summary>
    /// Gets the metadata collection.
    /// </summary>
    public IReadOnlyDictionary<string, MetadataValue> Metadata => _metadata;

    private VectorRecord(string id, string documentId)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
    }

    /// <summary>
    /// Creates a new vector record with the specified id and document id.
    /// </summary>
    /// <param name="id">The unique identifier for the record.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <returns>A new VectorRecord instance ready for fluent configuration.</returns>
    public static VectorRecord Create(string id, string documentId)
    {
        return new VectorRecord(id, documentId);
    }

    /// <summary>
    /// Adds or updates a named vector.
    /// </summary>
    /// <param name="key">The vector key/name.</param>
    /// <param name="vector">The vector values.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithVector(string key, float[] vector)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (vector == null) throw new ArgumentNullException(nameof(vector));

        _vectors[key] = vector;
        return this;
    }

    /// <summary>
    /// Adds or updates string metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithMetadata(string key, string value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or updates integer metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithMetadata(string key, int value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or updates double metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The double value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithMetadata(string key, double value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or updates boolean metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithMetadata(string key, bool value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds or updates DateTime metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The DateTime value.</param>
    /// <returns>This instance for fluent chaining.</returns>
    public VectorRecord WithMetadata(string key, DateTime value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Retrieves a vector by key.
    /// </summary>
    /// <param name="key">The vector key.</param>
    /// <returns>The vector if found, otherwise null.</returns>
    public float[]? GetVector(string key)
    {
        return _vectors.TryGetValue(key, out var vector) ? vector : null;
    }

    /// <summary>
    /// Retrieves metadata as a MetadataValue.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value if found, otherwise null.</returns>
    public MetadataValue? GetMetadata(string key)
    {
        return _metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Retrieves metadata as a string.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The string value if found and of correct type, otherwise null.</returns>
    public string? GetMetadataAsString(string key)
    {
        return GetMetadata(key)?.AsString();
    }

    /// <summary>
    /// Retrieves metadata as an int.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The int value if found and of correct type, otherwise null.</returns>
    public int? GetMetadataAsInt(string key)
    {
        return GetMetadata(key)?.AsInt();
    }

    /// <summary>
    /// Retrieves metadata as a double.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The double value if found and of correct type, otherwise null.</returns>
    public double? GetMetadataAsDouble(string key)
    {
        return GetMetadata(key)?.AsDouble();
    }

    /// <summary>
    /// Retrieves metadata as a bool.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The bool value if found and of correct type, otherwise null.</returns>
    public bool? GetMetadataAsBool(string key)
    {
        return GetMetadata(key)?.AsBool();
    }

    /// <summary>
    /// Retrieves metadata as a DateTime.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The DateTime value if found and of correct type, otherwise null.</returns>
    public DateTime? GetMetadataAsDateTime(string key)
    {
        return GetMetadata(key)?.AsDateTime();
    }
}
