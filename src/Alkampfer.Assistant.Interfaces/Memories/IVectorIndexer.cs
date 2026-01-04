using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Provides vector indexing operations for VectorRecord with resilience and batch support.
/// </summary>
public interface IVectorIndexer
{
    /// <summary>
    /// Ensures that an index exists with the correct mapping for VectorRecord.
    /// If the index does not exist, it will be created with the appropriate configuration.
    /// </summary>
    /// <param name="indexName">The name of the index to ensure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureIndexMappingAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a collection of VectorRecords in batches with automatic retry on transient failures.
    /// </summary>
    /// <param name="indexName">The name of the index to write to.</param>
    /// <param name="records">The records to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A BulkIndexResult containing success/failure information.</returns>
    Task<BulkIndexResult> IndexRecordsAsync(
        string indexName,
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single VectorRecord by ID from the specified index.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="id">The ID of the record to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The VectorRecord if found, otherwise null.</returns>
    Task<VectorRecord?> GetRecordAsync(
        string indexName,
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all records with the specified DocumentId from the index.
    /// </summary>
    /// <param name="indexName">The name of the index to delete from.</param>
    /// <param name="documentId">The DocumentId of records to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    Task<long> DeleteByDocumentIdAsync(
        string indexName,
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an index.
    /// </summary>
    /// <param name="indexName">The name of the index to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures that a vector field mapping exists for the specified index with the given configuration.
    /// If the index does not exist, it will be created with the vector field.
    /// If the index exists, the vector field mapping will be added via PutMapping.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <param name="vectorFieldName">The name of the vector field.</param>
    /// <param name="dimensions">The number of dimensions for the vector.</param>
    /// <param name="similarity">The similarity function to use (default: Cosine).</param>
    /// <param name="indexVectors">Whether to index vectors for kNN search (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureVectorFieldMappingAsync(
        string indexName,
        string vectorFieldName,
        int dimensions,
        string similarity = "cosine",
        bool indexVectors = true,
        CancellationToken cancellationToken = default);
}
