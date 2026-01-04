using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Elasticsearch implementation of IVectorStore using composition with ElasticIndexer and ElasticQueryExecutor.
/// </summary>
public class ElasticVectorStore : IVectorStore
{
    private readonly ElasticIndexer _indexer;
    private readonly ElasticQueryExecutor _queryExecutor;
    private readonly string _indexName;

    /// <summary>
    /// Gets the name of the index being used.
    /// </summary>
    public string IndexName => _indexName;

    /// <summary>
    /// Initializes a new instance of the ElasticVectorStore class using existing indexer and query executor.
    /// </summary>
    /// <param name="indexer">The indexer to use for write operations.</param>
    /// <param name="queryExecutor">The query executor to use for read operations.</param>
    /// <param name="indexName">The name of the index to use.</param>
    public ElasticVectorStore(ElasticIndexer indexer, ElasticQueryExecutor queryExecutor, string indexName)
    {
        _indexer = indexer ?? throw new ArgumentNullException(nameof(indexer));
        _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        ElasticSearchConfiguration.ValidateIndexName(indexName);
        _indexName = indexName;
    }

    /// <summary>
    /// Adds or updates a vector record in the store.
    /// </summary>
    public async Task UpsertAsync(VectorRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        var result = await _indexer.IndexRecordsAsync(_indexName, new[] { record }, cancellationToken);

        if (!result.IsSuccess)
        {
            var errorMessages = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new InvalidOperationException($"Failed to upsert record '{record.Id}': {errorMessages}");
        }
    }

    /// <summary>
    /// Adds or updates multiple vector records in the store.
    /// </summary>
    public async Task<BulkIndexResult> UpsertManyAsync(IEnumerable<VectorRecord> records, CancellationToken cancellationToken = default)
    {
        if (records == null)
            throw new ArgumentNullException(nameof(records));

        return await _indexer.IndexRecordsAsync(_indexName, records, cancellationToken);
    }

    /// <summary>
    /// Removes a vector record from the store by its identifier.
    /// </summary>
    public async Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));

        // Get the record first to check if it exists
        var record = await GetAsync(id, cancellationToken);
        if (record == null)
            return false;

        // Delete using document ID (which in this case is the record's document ID)
        var deleted = await _indexer.DeleteByDocumentIdAsync(_indexName, record.DocumentId, cancellationToken);
        return deleted > 0;
    }

    /// <summary>
    /// Removes all vector records with the specified document identifier.
    /// </summary>
    public async Task<long> RemoveByDocumentIdAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(documentId))
            throw new ArgumentException("DocumentId cannot be null or empty.", nameof(documentId));

        return await _indexer.DeleteByDocumentIdAsync(_indexName, documentId, cancellationToken);
    }

    /// <summary>
    /// Gets a vector record by its identifier.
    /// </summary>
    public async Task<VectorRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Id cannot be null or empty.", nameof(id));

        return await _indexer.GetRecordAsync(_indexName, id, cancellationToken);
    }

    /// <summary>
    /// Executes a query against the vector store with filters and search parameters.
    /// </summary>
    public async Task<IVectorQueryResult> QueryAsync(IVectorQuery query, CancellationToken cancellationToken = default)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        return await _queryExecutor.ExecuteQueryAsync(_indexName, query, cancellationToken);
    }

    /// <summary>
    /// Searches for the top K most similar vectors given a query vector.
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string vectorKey,
        float[] queryVector,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(vectorKey))
            throw new ArgumentException("VectorKey cannot be null or empty.", nameof(vectorKey));
        if (queryVector == null || queryVector.Length == 0)
            throw new ArgumentException("QueryVector cannot be null or empty.", nameof(queryVector));
        if (topK <= 0)
            throw new ArgumentException("TopK must be greater than 0.", nameof(topK));

        return await _queryExecutor.ExecuteKnnSearchAsync(_indexName, vectorKey, queryVector, topK, cancellationToken);
    }

    /// <summary>
    /// Ensures the index exists with the correct mapping.
    /// </summary>
    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        await _indexer.EnsureIndexMappingAsync(_indexName, cancellationToken);
    }

    /// <summary>
    /// Deletes the entire index from the store.
    /// </summary>
    public async Task DeleteIndexAsync(CancellationToken cancellationToken = default)
    {
        await _indexer.DeleteIndexAsync(_indexName, cancellationToken);
    }
}
