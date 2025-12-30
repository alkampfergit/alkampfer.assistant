using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a search result from a vector store query.
/// </summary>
public class VectorSearchResult
{
    /// <summary>
    /// Gets the unique identifier of the matched record.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the document identifier.
    /// </summary>
    public string DocumentId { get; }

    /// <summary>
    /// Gets the similarity score (higher values indicate greater similarity).
    /// </summary>
    public double Score { get; }

    /// <summary>
    /// Gets the metadata from the matched record.
    /// </summary>
    public IReadOnlyDictionary<string, MetadataValue> Metadata { get; }

    /// <summary>
    /// Creates a new vector search result.
    /// </summary>
    /// <param name="id">The record identifier.</param>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="score">The similarity score.</param>
    /// <param name="metadata">The metadata collection.</param>
    public VectorSearchResult(string id, string documentId, double score, IReadOnlyDictionary<string, MetadataValue> metadata)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DocumentId = documentId ?? throw new ArgumentNullException(nameof(documentId));
        Score = score;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Retrieves metadata as a MetadataValue.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value if found, otherwise null.</returns>
    public MetadataValue? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
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
    /// Retrieves metadata as a DateTime.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The DateTime value if found and of correct type, otherwise null.</returns>
    public DateTime? GetMetadataAsDateTime(string key)
    {
        return GetMetadata(key)?.AsDateTime();
    }
}
