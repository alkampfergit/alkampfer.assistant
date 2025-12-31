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
public class ElasticIndexer
{
    private readonly ElasticSearchConfiguration _config;
    private readonly ElasticsearchClient _client;
    private readonly ResiliencePipeline _resiliencePipeline;

    /// <summary>
    /// Initializes a new instance of the ElasticIndexer class.
    /// </summary>
    /// <param name="config">The Elasticsearch configuration.</param>
    public ElasticIndexer(ElasticSearchConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Configure Elasticsearch client
        var settings = new ElasticsearchClientSettings(new Uri(_config.Address));

        if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
        {
            settings.Authentication(new BasicAuthentication(_config.Username, _config.Password));
        }

        _client = new ElasticsearchClient(settings);

        // Configure resilience pipeline with exponential backoff
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _config.MaxRetries,
                Delay = TimeSpan.FromSeconds(_config.InitialRetryDelaySeconds),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
            })
            .Build();
    }

    /// <summary>
    /// Ensures that an index exists with the correct mapping for VectorRecord.
    /// If the index does not exist, it will be created with the appropriate configuration.
    /// </summary>
    /// <param name="indexName">The name of the index to ensure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when the index name is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when index creation fails.</exception>
    public async Task EnsureIndexMappingAsync(string indexName, CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        // Check if index exists
        var existsResponse = await _client.Indices.ExistsAsync(indexName, cancellationToken);

        if (existsResponse.Exists)
        {
            return; // Index already exists
        }

        // Create index with mapping
        var createRequest = new CreateIndexRequest(indexName)
        {
            Settings = ElasticVectorRecordMapping.GetIndexSettings(_config.ShardNumber, _config.ReplicaNumber),
            Mappings = ElasticVectorRecordMapping.GetTypeMapping()
        };

        var createResponse = await _client.Indices.CreateAsync(createRequest, cancellationToken);

        if (!createResponse.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Failed to create index '{indexName}': {createResponse.DebugInformation}");
        }
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
