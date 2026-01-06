using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Integration tests for KNN vector search with DOT PRODUCT similarity.
/// All vectors are normalized (unit length) as required by dot product.
/// </summary>
public class ElasticKnnDotProductTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticKnnDotProductFixture>
{
    private readonly ElasticKnnDotProductFixture _fixture;

    public ElasticKnnDotProductTests(ElasticServerAvailabilityFixture _avail, ElasticKnnDotProductFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDotProduct_ReturnsResultsOrderedByDotProductSimilarity()
    {
        // Arrange - query vector [1, 0, 0] (unit length)
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 9);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(9, result.Records.Count);

        // Verify ordering by dot product similarity (highest to lowest)
        // Expected order based on dot([1,0,0], vector):
        // v1: [1, 0, 0]           → dot = 1.0     (exact match)
        // v7: [0.9701, 0.2425, 0] → dot = 0.9701
        // v2: [0.9487, 0.3162, 0] → dot = 0.9487
        // v9: [0.8165, 0.4082, 0.4082] → dot = 0.8165
        // v5: [0.7071, 0.7071, 0] → dot = 0.7071
        // v6: [0.5, 0.5, 0.7071]  → dot = 0.5
        // v3: [0, 1, 0]           → dot = 0.0 (perpendicular)
        // v4: [0, 0, 1]           → dot = 0.0 (perpendicular)
        // v8: [-0.5, 0.5, 0.7071] → dot = -0.5 (opposite)

        Assert.Equal("v1", result.Records[0].Id);  // Highest similarity
        Assert.Equal("v7", result.Records[1].Id);  // Second highest
        Assert.Equal("v2", result.Records[2].Id);  // Third
        Assert.Equal("v9", result.Records[3].Id);  // Fourth
        Assert.Equal("v5", result.Records[4].Id);  // Fifth
        Assert.Equal("v6", result.Records[5].Id);  // Sixth

        // v3 and v4 both have dot product = 0, order between them may vary
        var zeroSimilarityIds = new[] { result.Records[6].Id, result.Records[7].Id };
        Assert.Contains("v3", zeroSimilarityIds);
        Assert.Contains("v4", zeroSimilarityIds);

        Assert.Equal("v8", result.Records[8].Id);  // Lowest (negative dot product)
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDotProductTopK3_ReturnsTop3MostSimilar()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

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
    public async Task ExecuteKnnSearch_WithDotProductAndFilter_ReturnsFilteredResultsOrderedBySimilarity()
    {
        // Arrange - filter for technology category
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

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
        // Expected: v1 (1.0), v7 (0.9701), v2 (0.9487), v5 (0.7071)
        Assert.Equal("v1", result.Records[0].Id);

        if (result.Records.Count > 1)
            Assert.Equal("v7", result.Records[1].Id);

        if (result.Records.Count > 2)
            Assert.Equal("v2", result.Records[2].Id);

        if (result.Records.Count > 3)
            Assert.Equal("v5", result.Records[3].Id);
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDotProductAndMultipleFilters_ReturnsCorrectlyOrderedResults()
    {
        // Arrange - complex filter
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };
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

        // Results should still be ordered by dot product similarity
        // Eligible records: v1 (4.5, 2023-01-01), v7 (5.0, 2023-01-31), v2 (4.8, 2023-01-11), v5 (4.0, 2023-01-21)
        if (result.Records.Count > 0)
            Assert.Equal("v1", result.Records[0].Id);  // Highest similarity

        if (result.Records.Count > 1)
            Assert.Equal("v7", result.Records[1].Id);  // Second
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDifferentQueryVector_ReturnsCorrectOrdering()
    {
        // Arrange - query vector pointing in y direction [0, 1, 0]
        var queryVector = new float[] { 0.0f, 1.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Records.Count);

        // For query [0, 1, 0], vectors with Y component should rank higher
        // v3: [0, 1, 0]           → dot = 1.0 (highest)
        // v5: [0.7071, 0.7071, 0] → dot = 0.7071
        // v8: [-0.5, 0.5, 0.7071] → dot = 0.5
        // v6: [0.5, 0.5, 0.7071]  → dot = 0.5
        // v9: [0.8165, 0.4082, 0.4082] → dot = 0.4082

        Assert.Equal("v3", result.Records[0].Id);  // Exact match with Y axis
        Assert.Equal("v5", result.Records[1].Id);  // 45° angle
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithOppositeQueryVector_ReturnsLowestSimilarityLast()
    {
        // Arrange - query vector opposite to most documents [-1, 0, 0]
        var queryVector = new float[] { -1.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 9);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);

        // v8 has the most positive X component in negative direction, should be first
        // v1, v7, v2, v9 pointing in positive X should be last
        Assert.Equal("v8", result.Records[0].Id);  // Most similar to [-1, 0, 0]

        // v1 should be last (opposite direction, dot = -1.0)
        Assert.Equal("v1", result.Records[^1].Id);
    }
}
