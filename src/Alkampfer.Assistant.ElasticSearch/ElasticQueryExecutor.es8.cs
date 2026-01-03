using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Provides Elasticsearch query operations for VectorRecord using DisMax pattern.
/// </summary>
public class ElasticQueryExecutor : ElasticBaseClient
{
    /// <summary>
    /// Initializes a new instance of the ElasticQueryExecutor class.
    /// </summary>
    /// <param name="config">The Elasticsearch configuration.</param>
    public ElasticQueryExecutor(ElasticSearchConfiguration config) : base(config)
    {
    }

    /// <summary>
    /// Executes a VectorQuery against the specified index.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A VectorQueryResult containing matching records and metadata.</returns>
    public async Task<VectorQueryResult> ExecuteQueryAsync(
        string indexName,
        VectorQuery query,
        CancellationToken cancellationToken = default)
    {
        ElasticSearchConfiguration.ValidateIndexName(indexName);

        if (query == null)
            throw new ArgumentNullException(nameof(query));

        // Build the Elasticsearch query
        var elasticQuery = BuildElasticQuery(query);

        // Create search request
        var searchRequest = new SearchRequest(indexName)
        {
            Query = elasticQuery,
            Size = query.Limit ?? 10,
            From = query.Skip ?? 0,
            TrackTotalHits = new Elastic.Clients.Elasticsearch.Core.Search.TrackHits(true)
        };

        // Execute search with resilience
        var response = await _resiliencePipeline.ExecuteAsync(
            async ct => await _client.SearchAsync<object>(searchRequest, ct),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Query execution failed: {response.DebugInformation}");
        }

        // Convert results to VectorRecords
        var records = new List<VectorRecord>();
        foreach (var hit in response.Hits)
        {
            if (hit.Source != null)
            {
                var jsonDoc = JsonDocument.Parse(
                    JsonSerializer.Serialize(hit.Source));
                var record = VectorRecordExtensions.FromJsonElement(jsonDoc.RootElement);
                records.Add(record);
            }
        }

        var totalCount = response.Total;
        var executionTime = response.Took;

        return new VectorQueryResult(records, totalCount, executionTime);
    }

    /// <summary>
    /// Builds an Elasticsearch Query from a VectorQuery using DisMax pattern.
    /// </summary>
    private Query BuildElasticQuery(VectorQuery query)
    {
        var queries = new List<Query>();

        // Add full-text search query using DisMax
        if (!string.IsNullOrEmpty(query.SearchText))
        {
            var disMaxQuery = new DisMaxQuery
            {
                Queries = new Query[]
                {
                    new MatchQuery { Field = "text", Query = query.SearchText, Boost = 2.0f },
                    new MatchQuery { Field = "documentId", Query = query.SearchText, Boost = 1.0f }
                }
            };
            queries.Add(disMaxQuery);
        }

        // Add filters
        foreach (var filter in query.Filters)
        {
            queries.Add(filter.ToElasticQuery());
        }

        // Combine all queries with AND logic
        if (queries.Count == 0)
        {
            return new MatchAllQuery();
        }

        if (queries.Count == 1)
        {
            return queries[0];
        }

        return new BoolQuery
        {
            Must = queries.ToArray()
        };
    }
}
