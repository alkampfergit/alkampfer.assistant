using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.VectorStore;

/// <summary>
/// Represents the complete result set from a vector store query execution.
/// </summary>
public class VectorStoreQueryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreQueryResult"/> class.
    /// </summary>
    /// <param name="results">The list of search results, ordered by relevance (highest score first).</param>
    /// <param name="totalCount">The total number of results found (may be greater than results returned due to pagination).</param>
    public VectorStoreQueryResult(
        IReadOnlyList<VectorSearchResult> results,
        int totalCount)
    {
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");
        }

        Results = results ?? throw new ArgumentNullException(nameof(results));
        TotalCount = totalCount;
    }

    /// <summary>
    /// Gets the list of search results, ordered by relevance (highest score first).
    /// </summary>
    public IReadOnlyList<VectorSearchResult> Results { get; }

    /// <summary>
    /// Gets the total number of results found in the vector store matching the query.
    /// </summary>
    /// <remarks>
    /// This may be greater than the number of results returned if a limit was applied to the query.
    /// </remarks>
    public int TotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether the query returned any results.
    /// </summary>
    public bool HasResults => Results.Count > 0;

    /// <summary>
    /// Gets the first result, or null if no results were found.
    /// Convenience property for queries expected to return a single top result.
    /// </summary>
    public VectorSearchResult? FirstResult => Results.Count > 0 ? Results[0] : null;

    /// <summary>
    /// Gets or sets optional query execution metadata.
    /// </summary>
    /// <remarks>
    /// This can include information such as:
    /// <list type="bullet">
    /// <item><description>Execution time in milliseconds</description></item>
    /// <item><description>Index name that was queried</description></item>
    /// <item><description>Query optimization details</description></item>
    /// <item><description>Debug information</description></item>
    /// </list>
    /// The specific metadata fields depend on the vector store implementation.
    /// </remarks>
    public Dictionary<string, object>? QueryMetadata { get; set; }
}
