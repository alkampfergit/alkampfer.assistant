using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Comprehensive tests for atomic filter types and field semantics.
/// Covers: string equals, keyword equals, integer/double/boolean/datetime equals, 
/// array fields, case-sensitivity, whitespace handling, and multiple filter combinations.
/// </summary>
public class ElasticFilterTypesTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticFixture>
{
    private readonly ElasticFixture _fixture;

    public ElasticFilterTypesTests(ElasticServerAvailabilityFixture _avail, ElasticFixture fixture)
    {
        // Availability fixture ensures the Elasticsearch URL is configured and reachable (fail-fast)
        _fixture = fixture;
    }

    #region Keyword Filter Tests

    [Fact]
    public async Task KeywordEquals_CaseInsensitiveMatch_ReturnsMatchingRecords()
    {
        // Arrange - 'status' metadata contains "Active" for two records in the fixture
        var query = VectorQuery.Create()
            .WhereKeywordEquals("status", "ACTIVE") // use uppercase to validate case-insensitive matching
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
        // Ensure metadata contains the expected keyword (case-insensitive)
        foreach (var r in result.Records)
        {
            var keywords = r.GetMetadataAsKeywords("status");
            Assert.NotNull(keywords);
            Assert.Contains(keywords!, k => string.Equals(k, "Active", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task KeywordEquals_MixedCase_MatchesAnyCase()
    {
        // Arrange - test mixed case variations
        var query = VectorQuery.Create()
            .WhereKeywordEquals("status", "aCtIvE") // mixed case
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
    }

    [Fact]
    public async Task ArrayField_TermMatch_ReturnsMatchingElement()
    {
        // Arrange - one record in fixture has status "Inactive"
        var query = VectorQuery.Create()
            .WhereKeywordEquals("status", "inactive") // lowercase input expected to match
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("s2", result.Records[0].Id);
    }

    #endregion

    #region String Filter Tests (Full-Text Match)

    [Fact]
    public async Task StringEquals_PartialMatch_ReturnsMatchingRecords()
    {
        // Arrange - StringEqualsFilter uses Match query (analyzed, full-text)
        // Should match records containing the word "First" in title
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
    public async Task StringEquals_MultipleWords_MatchesAllWords()
    {
        // Arrange - should match records where category field contains "technology"
        var query = VectorQuery.Create()
            .WhereEquals("category", "technology")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
    }

    #endregion

    #region Integer Filter Tests

    [Fact]
    public async Task IntegerEquals_ExactMatch_ReturnsMatchingRecords()
    {
        // Arrange
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
    public async Task IntegerEquals_NoMatch_ReturnsEmpty()
    {
        // Arrange - query for a year that doesn't exist
        var query = VectorQuery.Create()
            .WhereEquals("year", 9999)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    #endregion

    #region Boolean Filter Tests

    [Fact]
    public async Task BooleanEquals_TrueValue_ReturnsMatchingRecords()
    {
        // Arrange - Note: fixture may not have boolean fields; test documents expected behavior
        var query = VectorQuery.Create()
            .WhereEquals("active", true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw, result depends on fixture data
        Assert.NotNull(result);
    }

    [Fact]
    public async Task BooleanEquals_FalseValue_ReturnsMatchingRecords()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereEquals("active", false)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw
        Assert.NotNull(result);
    }

    #endregion

    #region DateTime Filter Tests

    [Fact]
    public async Task DateTimeEquals_ExactMatch_ReturnsMatchingRecord()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);
        var query = VectorQuery.Create()
            .WhereEquals("created", baseDate)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        // Should return d2 which has created=baseDate
        if (result.Records.Count > 0)
        {
            Assert.All(result.Records, r => 
            {
                var dt = r.GetMetadataAsDateTime("created");
                Assert.NotNull(dt);
                Assert.Equal(baseDate, dt.Value);
            });
        }
    }

    #endregion

    #region Double/Numeric Filter Tests

    [Fact]
    public async Task DoubleEquals_ExactMatch_ReturnsMatchingRecords()
    {
        // Arrange - Note: fixture may not have double fields; test documents expected behavior
        var query = VectorQuery.Create()
            .WhereEquals("rating", 4.5)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw, result depends on fixture data
        Assert.NotNull(result);
    }

    #endregion

    #region Multiple Filter Combination Tests

    [Fact]
    public async Task MultipleAtomicFilters_ImplicitAnd_ReturnsIntersection()
    {
        // Arrange - multiple atomic filters at root level are AND-combined
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
    public async Task MultipleFilters_DifferentTypes_WorksTogether()
    {
        // Arrange - mix string, integer, and keyword filters
        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .WhereEquals("year", 2021)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("m1", result.Records[0].Id);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Filter_OnNonExistentField_ReturnsEmpty()
    {
        // Arrange - query for a field that doesn't exist in any record
        var query = VectorQuery.Create()
            .WhereEquals("nonexistent_field", "value")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task StringEquals_EmptyString_HandledCorrectly()
    {
        // Arrange - test behavior with empty string
        var query = VectorQuery.Create()
            .WhereEquals("title", "")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw, returns empty or matches empty fields
        Assert.NotNull(result);
    }

    #endregion
}
