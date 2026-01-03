using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.VectorStore;

/// <summary>
/// Interface for executing queries against a vector store.
/// </summary>
/// <remarks>
/// This interface abstracts the underlying vector store implementation (e.g., Elasticsearch, Pinecone, Weaviate, etc.)
/// and provides a common API for semantic search using vector embeddings.
/// 
/// Typical usage pattern:
/// <code>
/// // 1. Generate embedding for query text
/// var embedding = await embeddingModel.GenerateEmbeddingsAsync(new[] { queryText });
/// 
/// // 2. Create query
/// var query = new VectorStoreQuery(embedding.FirstEmbedding)
/// {
///     MaxResults = 5,
///     MinimumScore = 0.7f
/// };
/// 
/// // 3. Execute query
/// var result = await queryExecutor.ExecuteQueryAsync(query);
/// 
/// // 4. Process results
/// foreach (var searchResult in result.Results)
/// {
///     Console.WriteLine($"Score: {searchResult.Score}, Content: {searchResult.Content}");
/// }
/// </code>
/// </remarks>
public interface IVectorStoreQueryExecutor
{
    /// <summary>
    /// Executes a vector similarity search query against the vector store.
    /// </summary>
    /// <param name="query">The query parameters including the embedding vector and filters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="VectorStoreQueryResult"/> containing the matching results ordered by relevance (highest score first).
    /// </returns>
    /// <remarks>
    /// The query execution performs a similarity search using the provided embedding vector.
    /// Results are ranked by similarity score (cosine similarity, dot product, or euclidean distance depending on implementation).
    /// The returned results respect the <see cref="VectorStoreQuery.MaxResults"/> and <see cref="VectorStoreQuery.MinimumScore"/> constraints.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="query"/> is null.</exception>
    Task<VectorStoreQueryResult> ExecuteQueryAsync(
        VectorStoreQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the vector store is available and healthy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the vector store is available and can accept queries; otherwise, false.</returns>
    /// <remarks>
    /// This method can be used for health checks and diagnostics.
    /// It should be a lightweight operation that doesn't perform extensive validation.
    /// </remarks>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
