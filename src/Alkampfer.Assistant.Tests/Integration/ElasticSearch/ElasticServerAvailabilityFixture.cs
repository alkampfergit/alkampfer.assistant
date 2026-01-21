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
    // Guard to ensure cleanup runs only once per process
    private static int _cleanupPerformed;

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

                // Optional global cleanup: only run once per test process and only when explicitly enabled
                if (Environment.GetEnvironmentVariable("ELASTIC_TEST_CLEANUP") == "true")
                {
                    // Ensure cleanup runs only once across threads
                    if (Interlocked.Exchange(ref _cleanupPerformed, 1) == 0)
                    {
                        try
                        {
                            var esClient = TestIndexUtils.CreateClientFromEnv();
                            // Only delete indices that are safe to delete - skip system indices starting with '.'
                            var deleted = TestIndexUtils.CleanupAllIndicesAsync(esClient, force: false, skipPredicate: name => name.StartsWith('.')).GetAwaiter().GetResult();
                            if (deleted != null)
                            {
                                foreach (var d in deleted)
                                    Console.WriteLine($"Deleted test index during startup cleanup: {d}");
                            }
                        }
                        catch (Exception ex)
                        {
                            // Do not fail the availability check if cleanup fails; just log
                            Console.WriteLine($"Elastic test cleanup failed: {ex.Message}");
                        }
                    }
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
