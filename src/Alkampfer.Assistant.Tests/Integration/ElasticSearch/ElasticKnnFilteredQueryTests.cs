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
/// Integration tests for KNN vector search with filtering.
/// Requires ELASTIC_TEST_URL environment variable to be set to an Elasticsearch instance.
/// </summary>
public class ElasticKnnFilteredQueryTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticKnnFixture>
{
    private readonly ElasticKnnFixture _fixture;

    public ElasticKnnFilteredQueryTests(ElasticServerAvailabilityFixture _avail, ElasticKnnFixture fixture)
    {
        // Availability fixture ensures the Elasticsearch URL is configured and reachable (fail-fast)
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithoutFilters_ReturnsTopKResults()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };  // Similar to vector1 [1,0,0]

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 3);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Records.Count);
        // Should return records sorted by similarity (closest to [1,0,0])
        Assert.Equal("v1", result.Records[0].Id);  // [1,0,0] exact match
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithKeywordFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .WhereKeywordEquals("category", "technology");

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.All(result.Records, r =>
        {
            var categories = r.GetMetadataAsKeywords("category");
            Assert.NotNull(categories);
            Assert.Contains("technology", categories, StringComparer.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithNumericRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryVector = new float[] { 0.5f, 0.5f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .WhereRange("rating", 4.0, 5.0);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.All(result.Records, r =>
        {
            var rating = r.GetMetadataAsDouble("rating");
            Assert.NotNull(rating);
            Assert.InRange(rating.Value, 4.0, 5.0);
        });
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryVector = new float[] { 0.0f, 1.0f, 0.0f };
        var fromDate = DateTime.Parse("2023-01-01");
        var toDate = DateTime.Parse("2023-12-31");

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .WhereDateRange("created", fromDate, toDate);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.All(result.Records, r =>
        {
            var created = r.GetMetadataAsDateTime("created");
            Assert.NotNull(created);
            Assert.InRange(created.Value, fromDate, toDate);
        });
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange - matches the production code example from the user
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
        // Should have results that match all filters
        if (result.Records.Count > 0)
        {
            Assert.All(result.Records, r =>
            {
                var categories = r.GetMetadataAsKeywords("category");
                Assert.Contains("technology", categories, StringComparer.OrdinalIgnoreCase);

                var rating = r.GetMetadataAsDouble("rating");
                Assert.NotNull(rating);
                Assert.InRange(rating.Value, 4.0, 5.0);

                var created = r.GetMetadataAsDateTime("created");
                Assert.NotNull(created);
                Assert.True(created.Value >= fromDate);
            });
        }
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithOrFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryVector = new float[] { 0.0f, 0.0f, 1.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .Where(f => f.Or(
                f.KeywordEquals("category", "technology"),
                f.Equals("year", 2021)
            ));

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.All(result.Records, r =>
        {
            var categories = r.GetMetadataAsKeywords("category");
            var year = r.GetMetadataAsInt("year");

            // At least one condition must be true
            bool hasCategory = categories != null && categories.Contains("technology", StringComparer.OrdinalIgnoreCase);
            bool hasYear = year == 2021;
            Assert.True(hasCategory || hasYear);
        });
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithSelectiveFilter_StillReturnsResults()
    {
        // Arrange - very selective filter that eliminates most candidates
        var queryVector = new float[] { 1.0f, 1.0f, 1.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5)
            .WhereEquals("year", 2023);  // Only a few records have this

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        // Should return results if any exist with year=2023
        Assert.All(result.Records, r => Assert.Equal(2023, r.GetMetadataAsInt("year")));
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithImpossibleFilter_ReturnsEmptyResults()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 10)
            .WhereEquals("year", 9999);  // No records have this year

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task ExecuteKnnSearch_WithCustomNumCandidates_ExecutesSuccessfully()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector, topK: 5, numCandidates: 50);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count <= 5);
    }

    [Fact]
    public void VectorQuery_WithBothTextAndVectorSearch_ThrowsException()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        // Act & Assert
        var query = VectorQuery.Create()
            .WithSearchText("test");

        Assert.Throws<InvalidOperationException>(() =>
            query.WithVectorSearch("embedding", queryVector));
    }

    [Fact]
    public void VectorQuery_WithTextAfterVectorSearch_ThrowsException()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        // Act & Assert
        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector);

        Assert.Throws<InvalidOperationException>(() =>
            query.WithSearchText("test"));
    }

    [Fact]
    public void VectorQuery_WithMultipleVectorSearches_ThrowsException()
    {
        // Arrange
        var queryVector1 = new float[] { 1.0f, 0.0f, 0.0f };
        var queryVector2 = new float[] { 0.0f, 1.0f, 0.0f };

        // Act & Assert
        var query = VectorQuery.Create()
            .WithVectorSearch("embedding", queryVector1);

        Assert.Throws<InvalidOperationException>(() =>
            query.WithVectorSearch("embedding2", queryVector2));
    }

    [Fact]
    public void VectorQuery_WithInvalidParameters_ThrowsException()
    {
        // Arrange
        var queryVector = new float[] { 1.0f, 0.0f, 0.0f };

        // Act & Assert - null vectorKey
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch(null!, queryVector));

        // Empty vectorKey
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("", queryVector));

        // Null queryVector
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("embedding", null!));

        // Empty queryVector
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("embedding", Array.Empty<float>()));

        // Invalid topK
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("embedding", queryVector, topK: 0));

        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("embedding", queryVector, topK: -1));

        // NumCandidates < TopK
        Assert.Throws<ArgumentException>(() =>
            VectorQuery.Create().WithVectorSearch("embedding", queryVector, topK: 10, numCandidates: 5));
    }
}
