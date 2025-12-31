using System;
using System.Text.RegularExpressions;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Configuration for Elasticsearch connection and indexing behavior.
/// </summary>
public record ElasticSearchConfiguration
{
    /// <summary>
    /// Gets the Elasticsearch server address (e.g., "http://localhost:9200").
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the username for basic authentication. Null for unauthenticated instances.
    /// TODO: Consider integration with IConfiguration and .NET secrets management for sensitive data.
    /// TODO: Add support for API key authentication as an alternative to username/password.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the password for basic authentication. Null for unauthenticated instances.
    /// TODO: Consider integration with IConfiguration and .NET secrets management for sensitive data.
    /// TODO: Add support for API key authentication as an alternative to username/password.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets the number of primary shards for new indexes.
    /// </summary>
    public int ShardNumber { get; init; } = 1;

    /// <summary>
    /// Gets the number of replica shards for new indexes.
    /// </summary>
    public int ReplicaNumber { get; init; } = 1;

    /// <summary>
    /// Gets the batch size for bulk indexing operations.
    /// </summary>
    public int BulkBatchSize { get; init; } = 200;

    /// <summary>
    /// Gets the maximum number of retry attempts for transient errors.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the initial retry delay in seconds for exponential backoff.
    /// </summary>
    public int InitialRetryDelaySeconds { get; init; } = 1;

    /// <summary>
    /// Validates an Elasticsearch index name according to basic naming rules.
    /// </summary>
    /// <param name="indexName">The index name to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when indexName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when indexName violates Elasticsearch naming rules.</exception>
    public static void ValidateIndexName(string indexName)
    {
        if (indexName == null)
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentException("Index name cannot be empty or whitespace.", nameof(indexName));
        }

        // Check for uppercase letters
        if (indexName != indexName.ToLowerInvariant())
        {
            throw new ArgumentException("Index name must be lowercase.", nameof(indexName));
        }

        // Check for invalid characters: \, /, *, ?, ", <, >, |, space, comma, #, :
        var invalidChars = new[] { '\\', '/', '*', '?', '"', '<', '>', '|', ' ', ',', '#', ':' };
        if (indexName.IndexOfAny(invalidChars) >= 0)
        {
            throw new ArgumentException(
                $"Index name contains invalid characters. Cannot contain: \\ / * ? \" < > | space , # :",
                nameof(indexName));
        }

        // Check for invalid starting characters: -, _, +, .
        if (indexName.StartsWith('-') || indexName.StartsWith('_') || 
            indexName.StartsWith('+') || indexName.StartsWith('.'))
        {
            throw new ArgumentException(
                "Index name cannot start with -, _, +, or .",
                nameof(indexName));
        }
    }
}
