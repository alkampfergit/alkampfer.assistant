using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

public static class TestIndexUtils
{
    /// <summary>
    /// Creates an <see cref="ElasticsearchClient"/> using the environment value <c>ELASTIC_TEST_URL</c>.
    /// </summary>
    public static ElasticsearchClient CreateClientFromEnv()
    {
        DotEnv.Load();
        var url = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException("ELASTIC_TEST_URL environment variable is not set. Please set it to your Elasticsearch instance URL (e.g., http://localhost:9200).");

        var settings = new ElasticsearchClientSettings(new Uri(url));
        return new ElasticsearchClient(settings);
    }

    /// <summary>
    /// Enumerates index names present in the cluster.
    /// </summary>
    public static async Task<IEnumerable<string>> EnumerateIndicesAsync(ElasticsearchClient client, CancellationToken cancellationToken = default)
    {
        // Get all indices using Get Index API with wildcard
        var resp = await client.Indices.GetAsync(Elastic.Clients.Elasticsearch.Indices.All, cancellationToken: cancellationToken);
        if (resp?.Indices == null)
            return Enumerable.Empty<string>();

        return resp.Indices.Keys.Select(k => k.ToString()).Where(n => !string.IsNullOrEmpty(n)).ToList();
    }

    /// <summary>
    /// Deletes the given index.
    /// </summary>
    public static async Task DeleteIndexAsync(ElasticsearchClient client, string indexName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(indexName))
            return;

        await client.Indices.DeleteAsync(indexName, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Cleans up indices in the cluster.
    /// </summary>
    /// <param name="force">If true, delete all non-skipped indices; if false, only delete indices that start with the test prefix.</param>
    public static async Task<IEnumerable<string>> CleanupAllIndicesAsync(ElasticsearchClient client, bool force = false, Func<string, bool>? skipPredicate = null, CancellationToken cancellationToken = default)
    {
        skipPredicate ??= (name => name.StartsWith('.'));
        var all = await EnumerateIndicesAsync(client, cancellationToken);
        var toDelete = new List<string>();
        foreach (var idx in all)
        {
            if (skipPredicate(idx))
                continue;

            if (!force && !idx.StartsWith("aatest-"))
                continue;

            toDelete.Add(idx);
        }

        var deleted = new List<string>();
        foreach (var idx in toDelete)
        {
            try
            {
                await DeleteIndexAsync(client, idx, cancellationToken);
                deleted.Add(idx);
            }
            catch
            {
                // swallow individual failures but continue
            }
        }

        return deleted;
    }

    /// <summary>
    /// Asserts the test index name follows the convention `aatest-` prefix.
    /// </summary>
    public static void AssertIsTestIndexName(string indexName)
    {
        if (string.IsNullOrEmpty(indexName) || !indexName.StartsWith("aatest-"))
            throw new ArgumentException("Test index names for integration tests must start with 'aatest-'.", nameof(indexName));
    }
}
