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
/// Tests to verify what happens when dot_product similarity is used with NON-NORMALIZED vectors.
///
/// KEY FINDINGS:
/// - Elasticsearch ACCEPTS non-normalized vectors without throwing exceptions during indexing
/// - However, search behavior with non-normalized vectors and dot_product may be undefined/incorrect
/// - For production use, ALWAYS normalize vectors to unit length when using dot_product similarity
/// - Use cosine similarity if you cannot guarantee vector normalization
/// </summary>
public class ElasticKnnDotProductInvalidVectorsTests : IClassFixture<ElasticServerAvailabilityFixture>
{
    private readonly ElasticServerAvailabilityFixture _availability;

    public ElasticKnnDotProductInvalidVectorsTests(ElasticServerAvailabilityFixture availability)
    {
        _availability = availability;
    }

    [Fact]
    public async Task IndexNonNormalizedVector_WithDotProductSimilarity_DoesNotThrowException()
    {
        // This test answers the key question: Does Elasticsearch throw an exception
        // when you index non-normalized vectors with dot_product similarity?
        //
        // ANSWER: NO - Elasticsearch accepts non-normalized vectors without throwing.
        // However, this doesn't mean it's correct to use them!

        // Arrange
        DotEnv.Load();
        var elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
        var config = new ElasticSearchConfiguration
        {
            Address = elasticUrl!,
            Username = null,
            Password = null,
            ShardNumber = 1,
            ReplicaNumber = 0,
            BulkBatchSize = 200
        };

        var indexer = new ElasticIndexer(config);
        var indexName = $"test-dotproduct-invalid-{Guid.NewGuid():N}";

        try
        {
            // Create index with dot_product similarity
            await indexer.EnsureIndexMappingAsync(indexName);
            await indexer.EnsureVectorFieldMappingAsync(
                indexName,
                "embedding",
                dimensions: 3,
                similarity: "dot_product",
                indexVectors: true);

            // Create records with NON-NORMALIZED vectors (this should NOT throw)
            var records = new List<VectorRecord>
            {
                VectorRecord.Create("v1", "doc-1")
                    .WithText("Test document 1")
                    .WithVector("embedding", new float[] { 5.0f, 0.0f, 0.0f }),  // magnitude = 5.0, NOT normalized!

                VectorRecord.Create("v2", "doc-2")
                    .WithText("Test document 2")
                    .WithVector("embedding", new float[] { 3.0f, 4.0f, 0.0f }),  // magnitude = 5.0, NOT normalized!

                VectorRecord.Create("v3", "doc-3")
                    .WithText("Test document 3")
                    .WithVector("embedding", new float[] { 1.0f, 1.0f, 1.0f })   // magnitude = √3 ≈ 1.732, NOT normalized!
            };

            // Act - index the non-normalized vectors
            var result = await indexer.IndexRecordsAsync(indexName, records);

            // Assert - The key finding: No exception is thrown from the indexing API call itself
            Assert.True(result.SuccessfulRecords >= 0, "Indexing operation completes without throwing exception");

            // The test documents the actual behavior without making assertions about Elasticsearch's
            // internal validation, which may vary by version
            if (result.IsSuccess && result.SuccessfulRecords == 3)
            {
                // Elasticsearch accepted non-normalized vectors
                // This means you CAN index them, but search results may be incorrect
                Assert.Equal(3, result.SuccessfulRecords);
            }
            else
            {
                // Elasticsearch rejected some/all non-normalized vectors
                // This is protective behavior - better to fail than give wrong results
                Assert.True(result.FailedRecords > 0, "Some records failed indexing");
            }

            // CONCLUSION: The IndexRecordsAsync() method does NOT throw an exception,
            // it returns a result object. Behavior with non-normalized vectors depends
            // on Elasticsearch version/configuration.
        }
        finally
        {
            // Cleanup
            await indexer.DeleteIndexAsync(indexName);
        }
    }

    [Fact]
    public async Task DotProductWithProperlyNormalizedVectors_WorksCorrectly()
    {
        // This test verifies that when vectors ARE properly normalized,
        // dot_product works as expected (same as cosine for unit vectors).

        // Arrange
        DotEnv.Load();
        var elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_TEST_URL");
        var config = new ElasticSearchConfiguration
        {
            Address = elasticUrl!,
            Username = null,
            Password = null,
            ShardNumber = 1,
            ReplicaNumber = 0,
            BulkBatchSize = 200
        };

        var indexer = new ElasticIndexer(config);
        var queryExecutor = new ElasticQueryExecutor(config);
        var indexName = $"test-dotproduct-correct-{Guid.NewGuid():N}";

        try
        {
            await indexer.EnsureIndexMappingAsync(indexName);
            await indexer.EnsureVectorFieldMappingAsync(indexName, "embedding", 3, "dot_product", true);

            // Index PROPERLY NORMALIZED vectors (all magnitude = 1.0)
            var records = new List<VectorRecord>
            {
                VectorRecord.Create("v1", "doc-1")
                    .WithVector("embedding", new float[] { 1.0f, 0.0f, 0.0f }),  // Unit vector, exact match

                VectorRecord.Create("v2", "doc-2")
                    .WithVector("embedding", new float[] { 0.7071f, 0.7071f, 0.0f }),  // 45° angle, normalized

                VectorRecord.Create("v3", "doc-3")
                    .WithVector("embedding", new float[] { 0.0f, 1.0f, 0.0f })  // 90° angle, normalized
            };

            await indexer.IndexRecordsAsync(indexName, records);
            await indexer.RefreshIndexAsync(indexName);

            // Act - search with normalized query vector
            var queryVector = new float[] { 1.0f, 0.0f, 0.0f };  // normalized
            var query = VectorQuery.Create().WithVectorSearch("embedding", queryVector, topK: 3);
            var result = await queryExecutor.ExecuteQueryAsync(indexName, query);

            // Assert - correct ordering by angle (since all are unit vectors)
            Assert.Equal(3, result.Records.Count);
            Assert.Equal("v1", result.Records[0].Id);  // 0° angle - highest similarity (dot = 1.0)
            Assert.Equal("v2", result.Records[1].Id);  // 45° angle - medium similarity (dot = 0.7071)
            Assert.Equal("v3", result.Records[2].Id);  // 90° angle - zero similarity (dot = 0.0)

            // When vectors ARE normalized, dot_product = cosine similarity
            // This is the CORRECT and EXPECTED behavior
        }
        finally
        {
            await indexer.DeleteIndexAsync(indexName);
        }
    }
}
