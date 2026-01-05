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
/// Integration tests for ElasticQueryExecutor.
/// Requires ELASTIC_TEST_URL environment variable to be set to an Elasticsearch instance.
/// </summary>
public class ElasticQueryExecutorTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticFixture>
{
    private readonly ElasticFixture _fixture;

    public ElasticQueryExecutorTests(ElasticServerAvailabilityFixture _avail, ElasticFixture fixture)
    {
        // Availability fixture ensures the Elasticsearch URL is configured and reachable (fail-fast)
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithSearchText_ReturnsMatchingRecords()
    {
        // Arrange - records indexed once in constructor

        var query = VectorQuery.Create()
            .WithSearchText("Elasticsearch")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.Contains(result.Records, r => r.Text?.Contains("Elasticsearch") == true);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithStringFilter_ReturnsMatchingRecords()
    {
        // Arrange - records indexed once in constructor

        var query = VectorQuery.Create()
            .WhereEquals("title", "First")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        Assert.Contains(result.Records, r => r.GetMetadataAsString("title")?.Contains("First") == true);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithIntegerFilter_ReturnsMatchingRecords()
    {
        // Arrange - records indexed once in constructor

        var query = VectorQuery.Create()
            .WhereEquals("year", 2022)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Records);
        Assert.All(result.Records, r => Assert.Equal(2022, r.GetMetadataAsInt("year")));
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithKeywordFilter_ReturnsMatchingRecords()
    {
        // Arrange - records indexed once in constructor

        var query = VectorQuery.Create()
            .WhereKeywordEquals("status", "active") // Case insensitive
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithDateRangeFilter_ReturnsMatchingRecords()
    {
        // Arrange - use same base date as constructor mapping
        var baseDate = new DateTime(2023, 1, 1);
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate.AddDays(-5), baseDate.AddDays(5))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("d2", result.Records[0].Id);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithMultipleFilters_ReturnsMatchingRecords()
    {
        // Arrange - records indexed once in constructor

        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .WhereEquals("year", 2022)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("m2", result.Records[0].Id);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithNullFilters_IgnoresNullFilters()
    {
        // Arrange - records indexed once in constructor

        // Act - null filters should be skipped
        var query = VectorQuery.Create()
            .WhereEquals("field", (string?)null)
            .WhereEquals("number", (int?)null)
            .WithLimit(10);

        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should match at least one record since filters are null
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithLimitAndSkip_PaginatesResults()
    {
        // Arrange - pagination records indexed in constructor
        var query = VectorQuery.Create()
            .WithLimit(5)
            .WithSkip(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - page size respected, total count should be at least the paging set
        Assert.NotNull(result);
        Assert.Equal(5, result.Records.Count);
        Assert.True(result.TotalCount >= 20);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task ExecuteQueryAsync_EmptyIndex_ReturnsEmptyResult()
    {
        // Arrange - ensure empty index exists and has mapping
        await _fixture.Indexer.EnsureIndexMappingAsync(_fixture.EmptyIndexName);

        var query = VectorQuery.Create()
            .WithSearchText("nonexistent")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.EmptyIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
        Assert.Equal(0, result.TotalCount);
        Assert.False(result.HasMore);
    }
}
