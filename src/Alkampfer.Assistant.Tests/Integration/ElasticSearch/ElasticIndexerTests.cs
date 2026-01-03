using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.ElasticSearch;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Integration tests for ElasticIndexer.
/// Requires ELASTIC_TEST_URL environment variable to be set to an Elasticsearch instance.
/// </summary>
public class ElasticIndexerTests : IAsyncDisposable
{
    private readonly ElasticIndexer _indexer;
    private readonly string _testIndexName;
    private readonly List<string> _indexesToCleanup = new();

    public ElasticIndexerTests()
    {
        // use dotenv to load environment variables from .env file if present
        DotEnv.Load();
        // Get Elasticsearch URL from environment variable
        var elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
        if (string.IsNullOrEmpty(elasticUrl))
        {
            throw new InvalidOperationException(
                "ELASTIC_TEST_URL environment variable is not set. " +
                "Please set it to your Elasticsearch instance URL (e.g., http://localhost:9200).");
        }

        // Create configuration (no authentication for test instance)
        var config = new ElasticSearchConfiguration
        {
            Address = elasticUrl,
            Username = null,
            Password = null,
            ShardNumber = 1,
            ReplicaNumber = 0, // No replicas for testing
            BulkBatchSize = 200
        };

        _indexer = new ElasticIndexer(config);

        // Generate unique test index name
        _testIndexName = $"test-vector-{Guid.NewGuid():N}";

        // Fast-fail: ping Elasticsearch to ensure it's available
        try
        {
            var pingTask = Task.Run(async () =>
            {
                await _indexer.EnsureIndexMappingAsync(_testIndexName);
                await _indexer.DeleteIndexAsync(_testIndexName);
            });

            if (!pingTask.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new InvalidOperationException(
                    $"Failed to connect to Elasticsearch at {elasticUrl} within 5 seconds. " +
                    "Please ensure Elasticsearch is running and accessible.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Elasticsearch at {elasticUrl}. " +
                "Please ensure Elasticsearch is running and accessible.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up all test indexes
        foreach (var indexName in _indexesToCleanup)
        {
            try
            {
                await _indexer.DeleteIndexAsync(indexName);
            }
            catch
            {
                // Best-effort cleanup - swallow exceptions
            }
        }
    }

    private string CreateTestIndex()
    {
        var indexName = $"test-vector-{Guid.NewGuid():N}";
        _indexesToCleanup.Add(indexName);
        return indexName;
    }

    [Fact]
    public void ValidateIndexName_WithInvalidUppercase_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ElasticSearchConfiguration.ValidateIndexName("TestIndex"));

        Assert.Contains("lowercase", ex.Message);
    }

    [Fact]
    public void ValidateIndexName_WithInvalidCharacters_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ElasticSearchConfiguration.ValidateIndexName("test*index"));

        Assert.Contains("invalid characters", ex.Message);
    }

    [Fact]
    public void ValidateIndexName_WithInvalidStartingCharacter_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ElasticSearchConfiguration.ValidateIndexName("-testindex"));

        Assert.Contains("cannot start with", ex.Message);
    }

    [Fact]
    public async Task EnsureIndexMappingAsync_CreatesIndex()
    {
        var indexName = CreateTestIndex();

        await _indexer.EnsureIndexMappingAsync(indexName);

        // Verify index was created by attempting to ensure it again (should not throw)
        await _indexer.EnsureIndexMappingAsync(indexName);
    }

    [Fact]
    public async Task EnsureIndexMappingAsync_IsIdempotent()
    {
        var indexName = CreateTestIndex();

        // Call multiple times
        await _indexer.EnsureIndexMappingAsync(indexName);
        await _indexer.EnsureIndexMappingAsync(indexName);
        await _indexer.EnsureIndexMappingAsync(indexName);

        // Should not throw
    }

    [Fact]
    public async Task IndexRecordsAsync_WithSingleRecord_ReturnsSuccess()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-1", "doc-1")
            .WithMetadata("name", "Test Record")
            .WithMetadata("count", 42)
            .WithMetadata("price", 19.99)
            .WithMetadata("active", true)
            .WithMetadata("created", DateTime.UtcNow);

        var result = await _indexer.IndexRecordsAsync(indexName, new[] { record });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalRecords);
        Assert.Equal(1, result.SuccessfulRecords);
        Assert.Equal(0, result.FailedRecords);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task IndexRecordsAsync_WithMultipleBatches_CombinesResults()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        // Create more than 200 records to test batching
        var records = Enumerable.Range(1, 250)
            .Select(i => VectorRecord.Create($"test-{i}", "doc-batch")
                .WithMetadata("index", i))
            .ToList();

        var result = await _indexer.IndexRecordsAsync(indexName, records);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.TotalRecords);
        Assert.Equal(250, result.SuccessfulRecords);
        Assert.Equal(0, result.FailedRecords);
    }

    [Fact]
    public async Task GetRecordAsync_WithExistingRecord_ReturnsRecord()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var originalDate = new DateTime(2025, 12, 31, 10, 30, 0, DateTimeKind.Utc);
        var record = VectorRecord.Create("test-get-1", "doc-get")
            .WithMetadata("title", "Test Title")
            .WithMetadata("count", 100)
            .WithMetadata("rating", 4.5)
            .WithMetadata("verified", true)
            .WithMetadata("published", originalDate);

        await _indexer.IndexRecordsAsync(indexName, new[] { record });

        // Wait a bit for Elasticsearch to index the document
        await Task.Delay(1000);

        var retrieved = await _indexer.GetRecordAsync(indexName, "test-get-1");

        Assert.NotNull(retrieved);
        Assert.Equal("test-get-1", retrieved!.Id);
        Assert.Equal("doc-get", retrieved.DocumentId);
        Assert.Equal("Test Title", retrieved.GetMetadataAsString("title"));
        Assert.Equal(100, retrieved.GetMetadataAsInt("count"));
        Assert.Equal(4.5, retrieved.GetMetadataAsDouble("rating"));
        Assert.Equal(true, retrieved.GetMetadataAsBool("verified"));
        
        var retrievedDate = retrieved.GetMetadataAsDateTime("published");
        Assert.NotNull(retrievedDate);
        // Allow for minor timestamp precision differences
        Assert.True(Math.Abs((retrievedDate.Value - originalDate).TotalSeconds) < 1);
    }

    [Fact]
    public async Task GetRecordAsync_WithMissingRecord_ReturnsNull()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var retrieved = await _indexer.GetRecordAsync(indexName, "non-existent-id");

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task RoundTrip_AllMetadataTypes_PreservesValues()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var testDate = DateTime.UtcNow;
        var record = VectorRecord.Create("roundtrip-1", "doc-roundtrip")
            .WithMetadata("string_field", "Hello, World!")
            .WithMetadata("int_field", 12345)
            .WithMetadata("double_field", 3.14159)
            .WithMetadata("bool_field", true)
            .WithMetadata("date_field", testDate);

        await _indexer.IndexRecordsAsync(indexName, new[] { record });
        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "roundtrip-1");

        Assert.NotNull(retrieved);
        Assert.Equal("Hello, World!", retrieved!.GetMetadataAsString("string_field"));
        Assert.Equal(12345, retrieved.GetMetadataAsInt("int_field"));
        
        var doubleValue = retrieved.GetMetadataAsDouble("double_field");
        Assert.NotNull(doubleValue);
        Assert.Equal(3.14159, doubleValue.Value, precision: 5);
        
        Assert.Equal(true, retrieved.GetMetadataAsBool("bool_field"));
        
        var retrievedDate = retrieved.GetMetadataAsDateTime("date_field");
        Assert.NotNull(retrievedDate);
        Assert.True(Math.Abs((retrievedDate.Value - testDate).TotalSeconds) < 1);
    }

    [Fact]
    public async Task DeleteIndexAsync_RemovesIndex()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        await _indexer.DeleteIndexAsync(indexName);

        // Verify index was deleted by checking if we need to create it again
        await _indexer.EnsureIndexMappingAsync(indexName);
    }

    [Fact]
    public async Task IndexRecordsAsync_WithEmptyCollection_ReturnsEmptyResult()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var result = await _indexer.IndexRecordsAsync(indexName, Array.Empty<VectorRecord>());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.TotalRecords);
        Assert.Equal(0, result.SuccessfulRecords);
        Assert.Equal(0, result.FailedRecords);
    }

    [Fact]
    public async Task IndexRecordsAsync_WithTextProperty_IndexesSuccessfully()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-text-1", "doc-text-1")
            .WithText("This is a sample text content for testing")
            .WithMetadata("category", "test");

        var result = await _indexer.IndexRecordsAsync(indexName, new[] { record });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalRecords);
        Assert.Equal(1, result.SuccessfulRecords);
        Assert.Equal(0, result.FailedRecords);
    }

    [Fact]
    public async Task GetRecordAsync_WithTextProperty_RetrievesTextCorrectly()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var textContent = "The quick brown fox jumps over the lazy dog. This is a test of the text field functionality.";
        var record = VectorRecord.Create("test-text-2", "doc-text-2")
            .WithText(textContent)
            .WithMetadata("title", "Test Document")
            .WithMetadata("priority", 5);

        await _indexer.IndexRecordsAsync(indexName, new[] { record });
        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "test-text-2");

        Assert.NotNull(retrieved);
        Assert.Equal("test-text-2", retrieved!.Id);
        Assert.Equal("doc-text-2", retrieved.DocumentId);
        Assert.Equal(textContent, retrieved.Text);
        Assert.Equal("Test Document", retrieved.GetMetadataAsString("title"));
        Assert.Equal(5, retrieved.GetMetadataAsInt("priority"));
    }

    [Fact]
    public async Task RoundTrip_WithTextProperty_PreservesAllData()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var testDate = DateTime.UtcNow;
        var textContent = "Multi-line text content\nwith special characters: @#$%^&*()\nand unicode: \u00e9\u00e0\u00fc\u00f1";

        var record = VectorRecord.Create("roundtrip-text-1", "doc-roundtrip-text")
            .WithText(textContent)
            .WithMetadata("string_field", "Hello, World!")
            .WithMetadata("int_field", 12345)
            .WithMetadata("double_field", 3.14159)
            .WithMetadata("bool_field", true)
            .WithMetadata("date_field", testDate);

        await _indexer.IndexRecordsAsync(indexName, new[] { record });
        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "roundtrip-text-1");

        Assert.NotNull(retrieved);
        Assert.Equal(textContent, retrieved!.Text);
        Assert.Equal("Hello, World!", retrieved.GetMetadataAsString("string_field"));
        Assert.Equal(12345, retrieved.GetMetadataAsInt("int_field"));

        var doubleValue = retrieved.GetMetadataAsDouble("double_field");
        Assert.NotNull(doubleValue);
        Assert.Equal(3.14159, doubleValue.Value, precision: 5);

        Assert.Equal(true, retrieved.GetMetadataAsBool("bool_field"));

        var retrievedDate = retrieved.GetMetadataAsDateTime("date_field");
        Assert.NotNull(retrievedDate);
        Assert.True(Math.Abs((retrievedDate.Value - testDate).TotalSeconds) < 1);
    }

    [Fact]
    public async Task IndexRecordsAsync_WithNullText_IndexesWithoutTextField()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-text-null-1", "doc-text-null-1")
            .WithText(null)
            .WithMetadata("category", "test");

        var result = await _indexer.IndexRecordsAsync(indexName, new[] { record });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessfulRecords);

        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "test-text-null-1");

        Assert.NotNull(retrieved);
        Assert.Null(retrieved!.Text);
        Assert.Equal("test", retrieved.GetMetadataAsString("category"));
    }

    [Fact]
    public async Task IndexRecordsAsync_WithEmptyText_IndexesEmptyString()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-text-empty-1", "doc-text-empty-1")
            .WithText("")
            .WithMetadata("status", "empty");

        var result = await _indexer.IndexRecordsAsync(indexName, new[] { record });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessfulRecords);

        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "test-text-empty-1");

        Assert.NotNull(retrieved);
        Assert.Equal("", retrieved!.Text);
        Assert.Equal("empty", retrieved.GetMetadataAsString("status"));
    }

    [Fact]
    public async Task IndexRecordsAsync_WithLongText_IndexesSuccessfully()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        // Create a long text (several paragraphs)
        var longText = string.Join("\n\n", Enumerable.Range(1, 50)
            .Select(i => $"Paragraph {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                        "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."));

        var record = VectorRecord.Create("test-text-long-1", "doc-text-long-1")
            .WithText(longText)
            .WithMetadata("word_count", 450);

        var result = await _indexer.IndexRecordsAsync(indexName, new[] { record });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessfulRecords);

        await Task.Delay(1000); // Wait for indexing

        var retrieved = await _indexer.GetRecordAsync(indexName, "test-text-long-1");

        Assert.NotNull(retrieved);
        Assert.Equal(longText, retrieved!.Text);
        Assert.Equal(450, retrieved.GetMetadataAsInt("word_count"));
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithSingleRecord_DeletesSuccessfully()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-delete-1", "doc-to-delete")
            .WithMetadata("category", "test");

        await _indexer.IndexRecordsAsync(indexName, new[] { record });
        await Task.Delay(1000); // Wait for indexing

        // Verify record exists
        var retrieved = await _indexer.GetRecordAsync(indexName, "test-delete-1");
        Assert.NotNull(retrieved);

        // Delete by DocumentId
        var deletedCount = await _indexer.DeleteByDocumentIdAsync(indexName, "doc-to-delete");

        Assert.Equal(1, deletedCount);

        // Verify record is deleted
        var afterDelete = await _indexer.GetRecordAsync(indexName, "test-delete-1");
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithMultipleRecords_DeletesAll()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        // Create multiple records with the same DocumentId
        var records = new[]
        {
            VectorRecord.Create("record-1", "shared-doc-id").WithMetadata("index", 1),
            VectorRecord.Create("record-2", "shared-doc-id").WithMetadata("index", 2),
            VectorRecord.Create("record-3", "shared-doc-id").WithMetadata("index", 3),
            VectorRecord.Create("record-4", "different-doc-id").WithMetadata("index", 4)
        };

        await _indexer.IndexRecordsAsync(indexName, records);
        await Task.Delay(1000); // Wait for indexing

        // Verify all records exist
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "record-1"));
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "record-2"));
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "record-3"));
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "record-4"));

        // Delete by DocumentId
        var deletedCount = await _indexer.DeleteByDocumentIdAsync(indexName, "shared-doc-id");

        Assert.Equal(3, deletedCount);

        // Verify records with shared-doc-id are deleted
        Assert.Null(await _indexer.GetRecordAsync(indexName, "record-1"));
        Assert.Null(await _indexer.GetRecordAsync(indexName, "record-2"));
        Assert.Null(await _indexer.GetRecordAsync(indexName, "record-3"));

        // Verify record with different DocumentId still exists
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "record-4"));
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithNonExistentDocumentId_ReturnsZero()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var record = VectorRecord.Create("test-1", "existing-doc")
            .WithMetadata("data", "value");

        await _indexer.IndexRecordsAsync(indexName, new[] { record });
        await Task.Delay(1000); // Wait for indexing

        // Try to delete non-existent DocumentId
        var deletedCount = await _indexer.DeleteByDocumentIdAsync(indexName, "non-existent-doc-id");

        Assert.Equal(0, deletedCount);

        // Verify original record still exists
        var retrieved = await _indexer.GetRecordAsync(indexName, "test-1");
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithEmptyDocumentId_ThrowsArgumentException()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _indexer.DeleteByDocumentIdAsync(indexName, ""));

        Assert.Contains("DocumentId", ex.Message);
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithNullDocumentId_ThrowsArgumentException()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _indexer.DeleteByDocumentIdAsync(indexName, null!));

        Assert.Contains("DocumentId", ex.Message);
    }

    [Fact]
    public async Task DeleteByDocumentIdAsync_WithManyRecords_DeletesAllMatching()
    {
        var indexName = CreateTestIndex();
        await _indexer.EnsureIndexMappingAsync(indexName);

        // Create many records with the same DocumentId
        var recordsToDelete = Enumerable.Range(1, 100)
            .Select(i => VectorRecord.Create($"bulk-delete-{i}", "bulk-doc-id")
                .WithMetadata("index", i))
            .ToList();

        // Create some records with different DocumentId
        var recordsToKeep = Enumerable.Range(1, 50)
            .Select(i => VectorRecord.Create($"keep-{i}", "keep-doc-id")
                .WithMetadata("index", i))
            .ToList();

        var allRecords = recordsToDelete.Concat(recordsToKeep).ToList();

        await _indexer.IndexRecordsAsync(indexName, allRecords);
        await Task.Delay(1500); // Wait for indexing (longer for more records)

        // Delete by DocumentId
        var deletedCount = await _indexer.DeleteByDocumentIdAsync(indexName, "bulk-doc-id");

        Assert.Equal(100, deletedCount);

        // Verify a few deleted records
        Assert.Null(await _indexer.GetRecordAsync(indexName, "bulk-delete-1"));
        Assert.Null(await _indexer.GetRecordAsync(indexName, "bulk-delete-50"));
        Assert.Null(await _indexer.GetRecordAsync(indexName, "bulk-delete-100"));

        // Verify a few kept records
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "keep-1"));
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "keep-25"));
        Assert.NotNull(await _indexer.GetRecordAsync(indexName, "keep-50"));
    }
}
