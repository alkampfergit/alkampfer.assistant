using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.ElasticSearch;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Integration tests for Elasticsearch vector field mapping and operations.
/// Requires ELASTIC_TEST_URL environment variable to be set to an Elasticsearch instance.
/// </summary>
public class ElasticVectorFieldTests : IAsyncDisposable, IClassFixture<ElasticServerAvailabilityFixture>
{
    private readonly ElasticIndexer _indexer;
    private readonly string _testIndexName;

    public ElasticVectorFieldTests(ElasticServerAvailabilityFixture _avail)
    {
        // Availability fixture ensures the Elasticsearch URL is configured and reachable (fail-fast)
        DotEnv.Load();
        var elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
        if (string.IsNullOrEmpty(elasticUrl))
        {
            throw new InvalidOperationException(
                "ELASTIC_TEST_URL environment variable is not set. " +
                "Please set it to your Elasticsearch instance URL (e.g., http://localhost:9200).");
        }

        var config = new ElasticSearchConfiguration
        {
            Address = elasticUrl,
            Username = null,
            Password = null,
            ShardNumber = 1,
            ReplicaNumber = 0,
            BulkBatchSize = 200
        };

        _indexer = new ElasticIndexer(config);
        _testIndexName = $"test-vector-field-{Guid.NewGuid():N}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _indexer.DeleteIndexAsync(_testIndexName);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public async Task EnsureVectorFieldMappingAsync_CreatesIndexWithVectorField()
    {
        // Arrange
        const string vectorFieldName = "embedding";
        const int dimensions = 384;

        // Act
        await _indexer.EnsureVectorFieldMappingAsync(
            _testIndexName,
            vectorFieldName,
            dimensions,
            similarity: "cosine",
            indexVectors: true);

        // Assert - verify we can index a record with this vector
        var vector = Enumerable.Range(0, dimensions).Select(i => (float)i / dimensions).ToArray();
        var record = VectorRecord.Create("test-1", "doc-1")
            .WithVector(vectorFieldName, vector)
            .WithMetadata("description", "Test record with vector");

        var result = await _indexer.IndexRecordsAsync(_testIndexName, new[] { record });
        var errors = result.GetErrorsAsString();
        Assert.True(result.IsSuccess, $"Indexing failed. Successful={result.SuccessfulRecords}, Failed={result.FailedRecords}, Errors: {errors}");
        Assert.Equal(1, result.SuccessfulRecords);
    }

    [Fact]
    public async Task EnsureVectorFieldMappingAsync_WithExistingIndex_UpdatesMapping()
    {
        // Arrange - create index first
        await _indexer.EnsureIndexMappingAsync(_testIndexName);

        const string vectorFieldName = "text_embedding";
        const int dimensions = 768;

        // Act - add vector field to existing index
        // Note: Using l2_norm similarity instead of dot_product because dot_product
        // requires unit-length (normalized) vectors, which adds complexity to the test
        await _indexer.EnsureVectorFieldMappingAsync(
            _testIndexName,
            vectorFieldName,
            dimensions,
            similarity: "l2_norm",
            indexVectors: true);

        // Assert - verify we can index a record with this vector
        var vector = Enumerable.Range(0, dimensions).Select(i => (float)i / dimensions).ToArray();
        var record = VectorRecord.Create("test-2", "doc-2")
            .WithVector(vectorFieldName, vector)
            .WithMetadata("title", "Document with embedding");

        var result = await _indexer.IndexRecordsAsync(_testIndexName, new[] { record });
        var errors = result.GetErrorsAsString();
        Assert.True(result.IsSuccess, $"Indexing failed. Successful={result.SuccessfulRecords}, Failed={result.FailedRecords}, Errors: {errors}");
        Assert.Equal(1, result.SuccessfulRecords);
    }

    [Fact]
    public async Task VectorRoundTrip_PreservesVectorData()
    {
        // Arrange
        const string vectorFieldName = "content_vector";
        const int dimensions = 512;

        await _indexer.EnsureVectorFieldMappingAsync(
            _testIndexName,
            vectorFieldName,
            dimensions);

        var originalVector = Enumerable.Range(0, dimensions)
            .Select(i => (float)Math.Sin(i * 0.1))
            .ToArray();

        var record = VectorRecord.Create("test-roundtrip-1", "doc-roundtrip")
            .WithText("Sample text content")
            .WithVector(vectorFieldName, originalVector)
            .WithMetadata("category", "test")
            .WithMetadata("score", 0.95);

        // Act - index and retrieve
        await _indexer.IndexRecordsAsync(_testIndexName, new[] { record });
        await _indexer.RefreshIndexAsync(_testIndexName);

        var retrieved = await _indexer.GetRecordAsync(_testIndexName, "test-roundtrip-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test-roundtrip-1", retrieved!.Id);
        Assert.Equal("doc-roundtrip", retrieved.DocumentId);
        Assert.Equal("Sample text content", retrieved.Text);
        Assert.Equal("test", retrieved.GetMetadataAsString("category"));
        Assert.Equal(0.95, retrieved.GetMetadataAsDouble("score"));

        // Verify vector
        var retrievedVector = retrieved.GetVector(vectorFieldName);
        Assert.NotNull(retrievedVector);
        Assert.Equal(dimensions, retrievedVector!.Length);
        
        // Check vector values (allow for minor floating point differences)
        for (int i = 0; i < dimensions; i++)
        {
            Assert.Equal(originalVector[i], retrievedVector[i], precision: 5);
        }
    }

    [Fact]
    public async Task MultipleVectorFields_CanBeIndexedAndRetrieved()
    {
        // Arrange
        const string textVectorField = "text_embedding";
        const string titleVectorField = "title_embedding";
        const int textDims = 384;
        const int titleDims = 256;

        await _indexer.EnsureVectorFieldMappingAsync(_testIndexName, textVectorField, textDims);
        await _indexer.EnsureVectorFieldMappingAsync(_testIndexName, titleVectorField, titleDims);

        var textVector = Enumerable.Range(0, textDims).Select(i => (float)i / textDims).ToArray();
        var titleVector = Enumerable.Range(0, titleDims).Select(i => (float)i / titleDims).ToArray();

        var record = VectorRecord.Create("test-multi-1", "doc-multi")
            .WithText("This is a document with multiple embeddings")
            .WithVector(textVectorField, textVector)
            .WithVector(titleVectorField, titleVector)
            .WithMetadata("type", "multi-vector");

        // Act
        await _indexer.IndexRecordsAsync(_testIndexName, new[] { record });
        await _indexer.RefreshIndexAsync(_testIndexName);

        var retrieved = await _indexer.GetRecordAsync(_testIndexName, "test-multi-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Vectors.Count);
        
        var retrievedTextVector = retrieved.GetVector(textVectorField);
        var retrievedTitleVector = retrieved.GetVector(titleVectorField);
        
        Assert.NotNull(retrievedTextVector);
        Assert.NotNull(retrievedTitleVector);
        Assert.Equal(textDims, retrievedTextVector!.Length);
        Assert.Equal(titleDims, retrievedTitleVector!.Length);
    }

    [Fact]
    public async Task EnsureVectorFieldMappingAsync_WithInvalidDimensions_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _indexer.EnsureVectorFieldMappingAsync(
                _testIndexName,
                "invalid_vector",
                0));

        await Assert.ThrowsAsync<ArgumentException>(
            () => _indexer.EnsureVectorFieldMappingAsync(
                _testIndexName,
                "invalid_vector",
                -1));
    }

    [Fact]
    public async Task EnsureVectorFieldMappingAsync_WithEmptyFieldName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _indexer.EnsureVectorFieldMappingAsync(
                _testIndexName,
                "",
                384));
    }

    [Fact]
    public async Task VectorFieldsWithMetadata_RoundTripSuccessfully()
    {
        // Arrange
        const string vectorField = "doc_embedding";
        const int dimensions = 128;

        await _indexer.EnsureVectorFieldMappingAsync(_testIndexName, vectorField, dimensions);

        var vector = Enumerable.Range(0, dimensions).Select(i => (float)i).ToArray();
        var testDate = DateTime.UtcNow;

        var record = VectorRecord.Create("test-mixed-1", "doc-mixed")
            .WithText("Content with vector and metadata")
            .WithVector(vectorField, vector)
            .WithMetadata("string_meta", "test value")
            .WithMetadata("int_meta", 42)
            .WithMetadata("double_meta", 3.14)
            .WithMetadata("bool_meta", true)
            .WithMetadata("date_meta", testDate)
            .WithMetadata("keywords_meta", "tag1", "tag2", "tag3");

        // Act
        await _indexer.IndexRecordsAsync(_testIndexName, new[] { record });
        await _indexer.RefreshIndexAsync(_testIndexName);

        var retrieved = await _indexer.GetRecordAsync(_testIndexName, "test-mixed-1");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Content with vector and metadata", retrieved!.Text);
        
        // Verify vector
        var retrievedVector = retrieved.GetVector(vectorField);
        Assert.NotNull(retrievedVector);
        Assert.Equal(dimensions, retrievedVector!.Length);
        
        // Verify all metadata
        Assert.Equal("test value", retrieved.GetMetadataAsString("string_meta"));
        Assert.Equal(42, retrieved.GetMetadataAsInt("int_meta"));
        Assert.Equal(3.14, retrieved.GetMetadataAsDouble("double_meta"));
        Assert.Equal(true, retrieved.GetMetadataAsBool("bool_meta"));
        
        var retrievedDate = retrieved.GetMetadataAsDateTime("date_meta");
        Assert.NotNull(retrievedDate);
        Assert.True(Math.Abs((retrievedDate.Value - testDate).TotalSeconds) < 1);
        
        var keywords = retrieved.GetMetadataAsKeywords("keywords_meta");
        Assert.NotNull(keywords);
        Assert.Equal(3, keywords!.Length);
        Assert.Contains("tag1", keywords);
        Assert.Contains("tag2", keywords);
        Assert.Contains("tag3", keywords);
    }

    [Fact]
    public async Task IndexRecord_WithoutVectorThenWithVector_BothSucceed()
    {
        // Arrange - create index and save record without vector
        await _indexer.EnsureIndexMappingAsync(_testIndexName);

        var recordWithoutVector = VectorRecord.Create("test-no-vec-1", "doc-evolving")
            .WithText("Initial document without vector")
            .WithMetadata("status", "initial");

        // Act - save record without vector
        var result1 = await _indexer.IndexRecordsAsync(_testIndexName, new[] { recordWithoutVector });
        var errors1 = result1.GetErrorsAsString();
        Assert.True(result1.IsSuccess, $"Indexing (initial) failed. Successful={result1.SuccessfulRecords}, Failed={result1.FailedRecords}, Errors: {errors1}");
        Assert.Equal(1, result1.SuccessfulRecords);

        // Now configure vector field
        const string vectorFieldName = "added_embedding";
        const int dimensions = 256;
        await _indexer.EnsureVectorFieldMappingAsync(_testIndexName, vectorFieldName, dimensions);

        // Save another record with the vector
        var vector = Enumerable.Range(0, dimensions).Select(i => (float)i / dimensions).ToArray();
        var recordWithVector = VectorRecord.Create("test-with-vec-1", "doc-evolved")
            .WithText("New document with vector")
            .WithVector(vectorFieldName, vector)
            .WithMetadata("status", "enhanced");

        // Act - save record with vector
        var result2 = await _indexer.IndexRecordsAsync(_testIndexName, new[] { recordWithVector });
        var errors2 = result2.GetErrorsAsString();
        Assert.True(result2.IsSuccess, $"Indexing (with vector) failed. Successful={result2.SuccessfulRecords}, Failed={result2.FailedRecords}, Errors: {errors2}");
        Assert.Equal(1, result2.SuccessfulRecords);

        // Refresh index to make documents immediately searchable
        await _indexer.RefreshIndexAsync(_testIndexName);

        // Assert - verify both records are retrievable
        var retrievedWithoutVector = await _indexer.GetRecordAsync(_testIndexName, "test-no-vec-1");
        Assert.NotNull(retrievedWithoutVector);
        Assert.Equal("Initial document without vector", retrievedWithoutVector!.Text);
        Assert.Empty(retrievedWithoutVector.Vectors);

        var retrievedWithVector = await _indexer.GetRecordAsync(_testIndexName, "test-with-vec-1");
        Assert.NotNull(retrievedWithVector);
        Assert.Equal("New document with vector", retrievedWithVector!.Text);
        Assert.Single(retrievedWithVector.Vectors);
        
        var retrievedVector = retrievedWithVector.GetVector(vectorFieldName);
        Assert.NotNull(retrievedVector);
        Assert.Equal(dimensions, retrievedVector!.Length);
    }

    [Fact]
    public async Task IndexRecord_WithUnmappedVector_ThrowsException()
    {
        // Arrange - create index without vector field mapping
        await _indexer.EnsureIndexMappingAsync(_testIndexName);

        // Create a record with a vector that hasn't been mapped
        const string unmappedVectorField = "unmapped_embedding";
        const int dimensions = 384;
        var vector = Enumerable.Range(0, dimensions).Select(i => (float)i / dimensions).ToArray();
        
        var recordWithUnmappedVector = VectorRecord.Create("test-unmapped-1", "doc-unmapped")
            .WithText("Document with unmapped vector field")
            .WithVector(unmappedVectorField, vector)
            .WithMetadata("attempt", "should_fail");

        // Act & Assert - indexing should fail with an exception
        var result = await _indexer.IndexRecordsAsync(_testIndexName, new[] { recordWithUnmappedVector });
        
        // Elasticsearch will reject the unmapped vector field, so we expect failures
        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.SuccessfulRecords);
        Assert.Equal(1, result.FailedRecords);
        Assert.NotEmpty(result.Errors);
        
        // Verify the error mentions the unmapped field
        var error = result.Errors.First();
        Assert.Contains("unmapped_embedding", error.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
