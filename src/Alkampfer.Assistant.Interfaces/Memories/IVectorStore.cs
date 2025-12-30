using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Interface for vector storage operations.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Adds or updates a vector record in the store.
    /// </summary>
    /// <param name="record">The vector record to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a vector record from the store by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the record to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns true if the record was found and removed, false otherwise.</returns>
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for the top K most similar vectors given a query vector.
    /// </summary>
    /// <param name="vectorKey">The key/name of the vector to search (e.g., "text", "title").</param>
    /// <param name="queryVector">The query vector to search for.</param>
    /// <param name="topK">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns a collection of search results ordered by similarity (highest first).</returns>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string vectorKey,
        float[] queryVector,
        int topK,
        CancellationToken cancellationToken = default);
}
