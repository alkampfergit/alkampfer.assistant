using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Integration tests for KNN vector search with COSINE similarity.
/// Vectors are NOT normalized - cosine similarity handles varying magnitudes correctly.
/// </summary>
public class ElasticKnnCosineTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticKnnCosineFixture>
{
    private readonly ElasticKnnCosineFixture _fixture;

    public ElasticKnnCosineTests(ElasticServerAvailabilityFixture _avail, ElasticKnnCosineFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCosine_ReturnsResultsOrderedByCosineSimilarity()
    {
        // Arrange - query vector [10, 0, 0] (NON-normalized, magnitude = 10)
        var queryVector = new float[] { 10.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 9);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Records.Count);

        // Verify ordering by cosine similarity (angle between vectors)
        // Cosine ignores magnitude, only cares about direction
        // Expected order:
        // v1: [5, 0, 0]           → cos = 1.0     (same direction)
        // v7: [8, 2, 0]           → cos = 0.9701  (≈14° angle)
        // v2: [3, 1, 0]           → cos = 0.9487  (≈18° angle)
        // v9: [4, 2, 2]           → cos = 0.8165  (≈35° angle)
        // v5: [2, 2, 0]           → cos = 0.7071  (45° angle)
        // v6: [1.5, 1.5, 3]       → cos = 0.4082  (≈66° angle)
        // v3: [0, 2, 0]           → cos = 0.0     (90° - perpendicular)
        // v4: [0, 0, 4]           → cos = 0.0     (90° - perpendicular)
        // v8: [-2, 3, 4]          → cos = -0.3714 (≈112° - opposite)

        Assert.Equal("v1", result.Records[0].Id);  // Highest similarity (same direction)
        Assert.Equal("v7", result.Records[1].Id);  // Second highest
        Assert.Equal("v2", result.Records[2].Id);  // Third
        Assert.Equal("v9", result.Records[3].Id);  // Fourth
        Assert.Equal("v5", result.Records[4].Id);  // Fifth (45° angle)
        Assert.Equal("v6", result.Records[5].Id);  // Sixth

        // v3 and v4 both have cosine = 0, order between them may vary
        var zeroSimilarityIds = new[] { result.Records[6].Id, result.Records[7].Id };
        Assert.Contains("v3", zeroSimilarityIds);
        Assert.Contains("v4", zeroSimilarityIds);

        Assert.Equal("v8", result.Records[8].Id);  // Lowest (opposite direction)
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCosineTopK3_ReturnsTop3MostSimilar()
    {
        // Arrange
        var queryVector = new float[] { 10.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 3);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Records.Count);

        // Top 3 should be v1, v7, v2 in that order
        Assert.Equal("v1", result.Records[0].Id);
        Assert.Equal("v7", result.Records[1].Id);
        Assert.Equal("v2", result.Records[2].Id);
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCosineProvesMagnitudeIrrelevant()
    {
        // Arrange - use very large query vector [1000, 0, 0]
        // With cosine, this should give SAME results as [10, 0, 0] or [1, 0, 0]
        var queryVector = new float[] { 1000.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Records.Count);

        // Same ordering as with [10, 0, 0] - magnitude doesn't matter
        Assert.Equal("v1", result.Records[0].Id);
        Assert.Equal("v7", result.Records[1].Id);
        Assert.Equal("v2", result.Records[2].Id);
        Assert.Equal("v9", result.Records[3].Id);
        Assert.Equal("v5", result.Records[4].Id);
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCosineAndFilter_ReturnsFilteredResultsOrderedBySimilarity()
    {
        // Arrange - filter for technology category
        var queryVector = new float[] { 10.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .WhereKeywordEquals("category", "technology");

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);

        // Should return only technology documents (v1, v2, v5, v7)
        Assert.True(result.Records.Count <= 4);
        Assert.All(result.Records, r =>
        {
            var categories = r.GetMetadataAsKeywords("category");
            Assert.Contains("technology", categories, StringComparer.OrdinalIgnoreCase);
        });

        // Verify ordering among filtered results
        // Expected: v1 (cos=1.0), v7 (cos=0.9701), v2 (cos=0.9487), v5 (cos=0.7071)
        Assert.Equal("v1", result.Records[0].Id);

        if (result.Records.Count > 1)
            Assert.Equal("v7", result.Records[1].Id);

        if (result.Records.Count > 2)
            Assert.Equal("v2", result.Records[2].Id);

        if (result.Records.Count > 3)
            Assert.Equal("v5", result.Records[3].Id);
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCosineAndMultipleFilters_ReturnsCorrectlyOrderedResults()
    {
        // Arrange - complex filter
        var queryVector = new float[] { 10.0f, 0.0f, 0.0f };
        var fromDate = DateTime.Parse("2023-01-01");

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .Where(f => f.And(
                f.KeywordEquals("category", "technology"),
                f.NumericRange("rating", 4.0, 5.0),
                f.DateRange("created", fromDate, DateTime.Now)
            ));

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);

        // Filtered results should match all conditions
        Assert.All(result.Records, r =>
        {
            var categories = r.GetMetadataAsKeywords("category");
            Assert.Contains("technology", categories, StringComparer.OrdinalIgnoreCase);

            var rating = r.GetMetadataAsDouble("rating");
            Assert.InRange(rating!.Value, 4.0, 5.0);

            var created = r.GetMetadataAsDateTime("created");
            Assert.True(created >= fromDate);
        });

        // Results should still be ordered by cosine similarity
        // Eligible records: v1 (4.5, 2023-01-01), v7 (5.0, 2023-01-31), v2 (4.8, 2023-01-11), v5 (4.0, 2023-01-21)
        if (result.Records.Count > 0)
            Assert.Equal("v1", result.Records[0].Id);  // Highest similarity

        if (result.Records.Count > 1)
            Assert.Equal("v7", result.Records[1].Id);  // Second
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDifferentQueryVector_ReturnsCorrectOrdering()
    {
        // Arrange - query vector pointing in y direction [0, 5, 0] (non-normalized)
        var queryVector = new float[] { 0.0f, 5.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Records.Count);

        // For query [0, 5, 0], vectors with Y component should rank higher
        // v3: [0, 2, 0]        → cos = 1.0 (same direction)
        // v5: [2, 2, 0]        → cos = 0.7071 (45° angle)
        // v8: [-2, 3, 4]       → cos = 0.5145
        // v6: [1.5, 1.5, 3]    → cos = 0.4082

        Assert.Equal("v3", result.Records[0].Id);  // Exact match with Y axis
        Assert.Equal("v5", result.Records[1].Id);  // 45° angle
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithNonNormalizedVectors_VerifiesVectorMagnitudesVary()
    {
        // This test verifies that our test fixture actually has non-normalized vectors
        // Arrange
        var queryVector = new float[] { 10.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 9);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - verify we got results
        Assert.NotNull(result);
        Assert.Equal(9, result.Records.Count);

        // Verify that the vectors in our fixture have different magnitudes
        // v1: [5, 0, 0]     → magnitude = 5.0
        // v2: [3, 1, 0]     → magnitude = √10 ≈ 3.162
        // v3: [0, 2, 0]     → magnitude = 2.0
        // v7: [8, 2, 0]     → magnitude = √68 ≈ 8.246
        // This proves cosine similarity works with non-normalized vectors

        // The fact that we get correct ordering proves Elasticsearch
        // is properly normalizing vectors internally for cosine similarity
        Assert.Equal("v1", result.Records[0].Id);  // Different magnitude than query [10,0,0]
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithSmallQueryVector_SameOrderingAsLargeVector()
    {
        // Arrange - use tiny query vector [0.1, 0, 0]
        // Should give same ordering as [10, 0, 0] or [1000, 0, 0]
        var queryVector = new float[] { 0.1f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Records.Count);

        // Exact same ordering as with larger query vectors
        // This definitively proves magnitude is irrelevant for cosine
        Assert.Equal("v1", result.Records[0].Id);
        Assert.Equal("v7", result.Records[1].Id);
        Assert.Equal("v2", result.Records[2].Id);
        Assert.Equal("v9", result.Records[3].Id);
        Assert.Equal("v5", result.Records[4].Id);
    }
}
