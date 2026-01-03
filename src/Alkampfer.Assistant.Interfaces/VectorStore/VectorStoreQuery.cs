using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.VectorStore;

/// <summary>
/// Represents a query to be executed against a vector store.
/// </summary>
public class VectorStoreQuery
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreQuery"/> class.
    /// </summary>
    /// <param name="queryEmbedding">The embedding vector for the query.</param>
    public VectorStoreQuery(ReadOnlyMemory<float> queryEmbedding)
    {
        if (queryEmbedding.IsEmpty)
        {
            throw new ArgumentException("Query embedding cannot be empty.", nameof(queryEmbedding));
        }

        QueryEmbedding = queryEmbedding;
    }

    /// <summary>
    /// Gets the embedding vector for the query.
    /// </summary>
    public ReadOnlyMemory<float> QueryEmbedding { get; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    /// <remarks>
    /// Default is 10 if not specified.
    /// </remarks>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Gets or sets the minimum similarity score threshold (0.0 to 1.0).
    /// Results below this threshold will be excluded.
    /// </summary>
    /// <remarks>
    /// Default is 0.0 (no threshold).
    /// </remarks>
    public float MinimumScore { get; set; } = 0.0f;

    /// <summary>
    /// Gets or sets optional metadata filters to apply to the query.
    /// The key is the metadata field name, and the value is the expected value.
    /// </summary>
    /// <remarks>
    /// This allows filtering results by metadata attributes (e.g., {"type": "document", "language": "en"}).
    /// </remarks>
    public Dictionary<string, object>? MetadataFilters { get; set; }

    /// <summary>
    /// Gets or sets the index name or collection name to query.
    /// If null, the default index will be used.
    /// </summary>
    public string? IndexName { get; set; }
}
