using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Transport;
using Alkampfer.Assistant.Interfaces.Memories;
using Polly;
using Polly.Retry;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Provides Elasticsearch indexing operations for VectorRecord with resilience and batch support.
/// </summary>
public class ElasticIndexer : ElasticBaseClient, IVectorIndexer
{
    /// <summary>
    /// Initializes a new instance of the ElasticIndexer class.
    /// </summary>
    /// <param name="config">The Elasticsearch configuration.</param>
    public ElasticIndexer(ElasticSearchConfiguration config) : base(config)
    {
    }

    /// <summary>
    /// Indexes a collection of VectorRecords in batches with automatic retry on transient failures.
    /// </summary>
    /// <param name="indexName">The name of the index to write to.</param>
    /// <param name="records">The records to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A BulkIndexResult containing success/failure information.</returns>
    public async Task<BulkIndexResult> IndexRecordsAsync(
        string indexName,
        IEnumerable<VectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        var recordsList = records.ToList();
        if (recordsList.Count == 0)
        {
            return new BulkIndexResult
            {
                TotalRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0,
                Errors = Array.Empty<BulkIndexError>()
            };
        }

        // Process in batches
        var batches = recordsList.Chunk(_config.BulkBatchSize);
        var batchResults = new List<BulkIndexResult>();

        foreach (var batch in batches)
        {
            var batchResult = await IndexBatchAsync(indexName, batch, cancellationToken);
            batchResults.Add(batchResult);
        }

        return BulkIndexResult.Combine(batchResults);
    }

    private async Task<BulkIndexResult> IndexBatchAsync(
        string indexName,
        IEnumerable<VectorRecord> batch,
        CancellationToken cancellationToken)
    {
        var batchList = batch.ToList();
        var totalRecords = batchList.Count;

        // Execute bulk indexing for each record
        var errors = new List<BulkIndexError>();
        var successCount = 0;
        
        foreach (var record in batchList)
        {
            try
            {
                var doc = record.ToExpandoObjectForIndexing();
                var response = await _resiliencePipeline.ExecuteAsync(
                    async ct => await _client.IndexAsync(doc, indexName, record.Id, ct),
                    cancellationToken);

                if (response.IsValidResponse)
                {
                    successCount++;
                }
                else
                {
                    errors.Add(new BulkIndexError
                    {
                        RecordId = record.Id,
                        ErrorMessage = response.DebugInformation ?? "Index operation failed",
                        StatusCode = response.ApiCallDetails.HttpStatusCode
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add(new BulkIndexError
                {
                    RecordId = record.Id,
                    ErrorMessage = ex.Message,
                    StatusCode = null
                });
            }
        }

        return new BulkIndexResult
        {
            TotalRecords = totalRecords,
            SuccessfulRecords = successCount,
            FailedRecords = totalRecords - successCount,
            Errors = errors
        };
    }

    /// <summary>
    /// Retrieves a single VectorRecord by ID from the specified index.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="id">The ID of the record to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The VectorRecord if found, otherwise null.</returns>
    public async Task<VectorRecord?> GetRecordAsync(
        string indexName,
        string id,
        CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        var response = await _resiliencePipeline.ExecuteAsync(
            async ct => await _client.GetAsync<object>(indexName, id, ct),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            // 404 means document not found - return null
            if (response.ApiCallDetails.HttpStatusCode == 404)
            {
                return null;
            }

            // Other errors should be thrown
            throw new InvalidOperationException(
                $"Failed to retrieve record '{id}' from index '{indexName}': {response.DebugInformation}");
        }

        if (!response.Found || response.Source == null)
        {
            return null;
        }

        // Deserialize from JsonElement
        var jsonDoc = System.Text.Json.JsonDocument.Parse(
            System.Text.Json.JsonSerializer.Serialize(response.Source));

        return VectorRecordExtensions.FromJsonElement(jsonDoc.RootElement);
    }

    /// <summary>
    /// Deletes all records with the specified DocumentId from the index.
    /// </summary>
    /// <param name="indexName">The name of the index to delete from.</param>
    /// <param name="documentId">The DocumentId of records to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    public async Task<long> DeleteByDocumentIdAsync(
        string indexName,
        string documentId,
        CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        if (string.IsNullOrEmpty(documentId))
        {
            throw new ArgumentException("DocumentId cannot be null or empty.", nameof(documentId));
        }

        // Use DeleteByQuery to delete all documents matching the DocumentId
        var response = await _resiliencePipeline.ExecuteAsync(
            async ct => await _client.DeleteByQueryAsync(indexName, d => d
                .Query(q => q
                    .Term(t => t
                        .Field("documentId")
                        .Value(documentId)
                    )
                )
                .Refresh(true), // Refresh immediately so changes are visible
                ct),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Failed to delete records with DocumentId '{documentId}' from index '{indexName}': {response.DebugInformation}");
        }

        return response.Deleted ?? 0;
    }

    /// <summary>
    /// Deletes an index from Elasticsearch.
    /// </summary>
    /// <param name="indexName">The name of the index to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        var response = await _client.Indices.DeleteAsync(indexName, cancellationToken);

        if (!response.IsValidResponse && response.ApiCallDetails.HttpStatusCode != 404)
        {
            throw new InvalidOperationException(
                $"Failed to delete index '{indexName}': {response.DebugInformation}");
        }
    }
}
