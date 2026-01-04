using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Provides vector query operations for searching and retrieving VectorRecords.
/// </summary>
public interface IVectorQueryExecutor
{
    /// <summary>
    /// Executes a VectorQuery against the specified index.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A VectorQueryResult containing matching records and metadata.</returns>
    Task<VectorQueryResult> ExecuteQueryAsync(
        string indexName,
        IVectorQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a KNN (K-Nearest Neighbors) search for vector similarity.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="vectorKey">The key/name of the vector field to search.</param>
    /// <param name="queryVector">The query vector to find similar vectors for.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of VectorSearchResult ordered by similarity (highest first).</returns>
    Task<IReadOnlyList<VectorSearchResult>> ExecuteKnnSearchAsync(
        string indexName,
        string vectorKey,
        float[] queryVector,
        int topK,
        CancellationToken cancellationToken = default);
}
