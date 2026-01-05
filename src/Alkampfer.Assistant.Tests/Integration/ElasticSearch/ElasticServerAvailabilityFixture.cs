using System;
using System.Net.Http;
using Alkampfer.Assistant.Core;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Ensures Elasticsearch test server is reachable once for the entire test run.
/// Throws an exception in the constructor when the server is not configured or not reachable,
/// causing tests that depend on it to fail fast.
/// </summary>
public class ElasticServerAvailabilityFixture
{
    private static readonly object _lock = new();
    private static bool _checked;
    private static Exception? _failure;

    public ElasticServerAvailabilityFixture()
    {
        EnsureAvailability();
    }

    private static void EnsureAvailability()
    {
        lock (_lock)
        {
            if (_checked)
            {
                if (_failure != null)
                    throw new InvalidOperationException("Elasticsearch availability check failed earlier.", _failure);
                return;
            }

            _checked = true;

            // Load .env helpers like other fixtures
            DotEnv.Load();

            var url = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
            if (string.IsNullOrEmpty(url))
            {
                _failure = new InvalidOperationException(
                    "ELASTIC_TEST_URL environment variable is not set. Please set it to your Elasticsearch instance URL (e.g., http://localhost:9200)."
                );
                throw _failure;
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                // Query cluster health endpoint which is lightweight and stable
                var healthUri = new Uri(new Uri(url), "/_cluster/health");
                var resp = client.GetAsync(healthUri).GetAwaiter().GetResult();
                if (!resp.IsSuccessStatusCode)
                {
                    _failure = new InvalidOperationException($"Elasticsearch at {url} returned status code {resp.StatusCode}.");
                    throw _failure;
                }
            }
            catch (Exception ex)
            {
                _failure = new InvalidOperationException($"Cannot reach Elasticsearch at {url}: {ex.Message}", ex);
                throw _failure;
            }
        }
    }
}
