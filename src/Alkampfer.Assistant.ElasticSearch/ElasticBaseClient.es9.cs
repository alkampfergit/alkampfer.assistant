using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Polly;
using Polly.Retry;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Base class for Elasticsearch operations providing common client configuration and resilience.
/// </summary>
public abstract class ElasticBaseClient
{
    protected readonly ElasticSearchConfiguration _config;
    protected readonly ElasticsearchClient _client;
    protected readonly ResiliencePipeline _resiliencePipeline;

    /// <summary>
    /// Initializes a new instance of the ElasticBaseClient class.
    /// </summary>
    /// <param name="config">The Elasticsearch configuration.</param>
    protected ElasticBaseClient(ElasticSearchConfiguration config)
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
                ShouldHandle = new PredicateBuilder().Handle<System.Net.Http.HttpRequestException>()
                    .Handle<TaskCanceledException>()
            })
            .Build();
    }

    /// <summary>
    /// Ensures that an index exists with the correct mapping for VectorRecord.
    /// </summary>
    public async Task EnsureIndexMappingAsync(
        string indexName,
        IEnumerable<VectorFieldConfiguration>? vectorFields = null,
        CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        var existsResponse = await _client.Indices.ExistsAsync(indexName, cancellationToken);

        if (existsResponse.Exists)
        {
            return;
        }

        var createRequest = new Elastic.Clients.Elasticsearch.IndexManagement.CreateIndexRequest(indexName)
        {
            Settings = ElasticVectorRecordMapping.GetIndexSettings(_config.ShardNumber, _config.ReplicaNumber),
            Mappings = ElasticVectorRecordMapping.GetTypeMapping(vectorFields)
        };

        var createResponse = await _client.Indices.CreateAsync(createRequest, cancellationToken);

        if (!createResponse.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Failed to create index '{indexName}': {createResponse.DebugInformation}");
        }
    }
}
