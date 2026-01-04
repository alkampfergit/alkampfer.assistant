using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Interface for vector storage operations including indexing, querying, and vector similarity search.
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Gets or sets the name of the index to use for operations.
    /// </summary>
    string IndexName { get; }

    /// <summary>
    /// Adds or updates a vector record in the store.
    /// </summary>
    /// <param name="record">The vector record to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates multiple vector records in the store.
    /// </summary>
    /// <param name="records">The vector records to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the bulk indexing result.</returns>
    Task<BulkIndexResult> UpsertManyAsync(IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a vector record from the store by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the record to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns true if the record was found and removed, false otherwise.</returns>
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all vector records with the specified document identifier.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the number of records deleted.</returns>
    Task<long> RemoveByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a vector record by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the record if found, null otherwise.</returns>
    Task<VectorRecord?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query against the vector store with filters and search parameters.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the query result with matching records and metadata.</returns>
    Task<IVectorQueryResult> QueryAsync(IVectorQuery query, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Ensures the index exists with the correct mapping.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the entire index from the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteIndexAsync(CancellationToken cancellationToken = default);
}
