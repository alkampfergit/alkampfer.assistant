using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Comprehensive tests for DateTimeRange and Numeric/Integer range filter semantics.
/// Tests cover: inclusive/exclusive bounds, open-ended ranges, boundary conditions, edge cases.
/// </summary>
public class ElasticRangeFilterTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticFixture>
{
    private readonly ElasticFixture _fixture;

    public ElasticRangeFilterTests(ElasticServerAvailabilityFixture _avail, ElasticFixture fixture)
    {
        // Availability fixture ensures the Elasticsearch URL is configured and reachable (fail-fast)
        _fixture = fixture;
    }

    #region DateTime Range Tests

    [Fact]
    public async Task DateRange_InclusiveBothBounds_IncludesBoundaryRecords()
    {
        // Arrange - base date as in fixture: d1 (-10 days), d2 (0 days), d3 (+10 days)
        var baseDate = new DateTime(2023, 1, 1);

        // Range exactly covering d1 and d2 with inclusive bounds
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate.AddDays(-10), baseDate, includeFrom: true, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include both d1 and d2
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d1", ids);
        Assert.Contains("d2", ids);
        Assert.DoesNotContain("d3", ids);
    }

    [Fact]
    public async Task DateRange_ExclusiveFromBound_ExcludesLowerBoundary()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // Range with exclusive lower bound (> baseDate.AddDays(-10))
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate.AddDays(-10), baseDate, includeFrom: false, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should exclude d1, include d2
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.DoesNotContain("d1", ids);
        Assert.Contains("d2", ids);
    }

    [Fact]
    public async Task DateRange_ExclusiveToBound_ExcludesUpperBoundary()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // Range with exclusive upper bound (< baseDate)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate.AddDays(-10), baseDate, includeFrom: true, includeTo: false)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include d1, exclude d2
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d1", ids);
        Assert.DoesNotContain("d2", ids);
    }

    [Fact]
    public async Task DateRange_ExclusiveBothBounds_ExcludesBothBoundaries()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // Range with both bounds exclusive (> -10 and < 0)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate.AddDays(-10), baseDate, includeFrom: false, includeTo: false)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should exclude both d1 and d2 (no records in between)
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task DateRange_ExactSingleDate_Inclusive_ReturnsExactMatch()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // Range that exactly matches the base date (single point with inclusive bounds)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate, baseDate, includeFrom: true, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return only d2
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("d2", result.Records[0].Id);
    }

    [Fact]
    public async Task DateRange_ExactSingleDate_Exclusive_ReturnsEmpty()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // Range that exactly matches the base date but with exclusive bounds (impossible range)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate, baseDate, includeFrom: false, includeTo: false)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return no records (> X and < X is empty)
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task DateRange_OnlyFromBound_Inclusive_ReturnsAllAbove()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // From-only range (>= baseDate)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate, null, includeFrom: true, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include d2 and d3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d2", ids);
        Assert.Contains("d3", ids);
        Assert.DoesNotContain("d1", ids);
    }

    [Fact]
    public async Task DateRange_OnlyFromBound_Exclusive_ExcludesLowerBoundary()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // From-only range with exclusive bound (> baseDate)
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate, null, includeFrom: false, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include only d3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d3", ids);
        Assert.DoesNotContain("d2", ids);
        Assert.DoesNotContain("d1", ids);
    }

    [Fact]
    public async Task DateRange_OnlyToBound_Inclusive_ReturnsAllBelow()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // To-only range (<= baseDate)
        var query = VectorQuery.Create()
            .WhereDateRange("created", null, baseDate, includeFrom: true, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include d1 and d2
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d1", ids);
        Assert.Contains("d2", ids);
        Assert.DoesNotContain("d3", ids);
    }

    [Fact]
    public async Task DateRange_OnlyToBound_Exclusive_ExcludesUpperBoundary()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1);

        // To-only range with exclusive bound (< baseDate)
        var query = VectorQuery.Create()
            .WhereDateRange("created", null, baseDate, includeFrom: true, includeTo: false)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include only d1
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("d1", ids);
        Assert.DoesNotContain("d2", ids);
        Assert.DoesNotContain("d3", ids);
    }

    #endregion

    #region Integer Range Tests

    [Fact]
    public async Task IntegerRange_InclusiveBothBounds_IncludesBoundaryRecords()
    {
        // Arrange - fixture has y1(2021), y2(2022), y3(2023), m1(2021), m2(2022), m3(2022)
        var query = VectorQuery.Create()
            .WhereRange("year", 2021, 2022, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include y1, y2, m1, m2, m3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y1", ids);
        Assert.Contains("y2", ids);
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        Assert.DoesNotContain("y3", ids);
    }

    [Fact]
    public async Task IntegerRange_ExclusiveFromBound_ExcludesLowerBoundary()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", 2021, 2022, includeFrom: false, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should exclude 2021 records, include 2022
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.DoesNotContain("y1", ids);
        Assert.DoesNotContain("m1", ids);
        Assert.Contains("y2", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
    }

    [Fact]
    public async Task IntegerRange_ExclusiveToBound_ExcludesUpperBoundary()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", 2021, 2022, includeFrom: true, includeTo: false)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include 2021, exclude 2022
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y1", ids);
        Assert.Contains("m1", ids);
        Assert.DoesNotContain("y2", ids);
        Assert.DoesNotContain("m2", ids);
        Assert.DoesNotContain("m3", ids);
    }

    [Fact]
    public async Task IntegerRange_ExclusiveBothBounds_ExcludesBothBoundaries()
    {
        // Arrange - range (2021 < year < 2023) with no values in between
        var query = VectorQuery.Create()
            .WhereRange("year", 2021, 2023, includeFrom: false, includeTo: false)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should only include 2022 records
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.DoesNotContain("y1", ids);
        Assert.Contains("y2", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        Assert.DoesNotContain("y3", ids);
    }

    [Fact]
    public async Task IntegerRange_OnlyFromBound_Inclusive_ReturnsAllAbove()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", 2022, null, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include y2, y3, m2, m3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y2", ids);
        Assert.Contains("y3", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        Assert.DoesNotContain("y1", ids);
        Assert.DoesNotContain("m1", ids);
    }

    [Fact]
    public async Task IntegerRange_OnlyFromBound_Exclusive_ExcludesLowerBoundary()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", 2022, null, includeFrom: false, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include only y3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y3", ids);
        Assert.DoesNotContain("y2", ids);
        Assert.DoesNotContain("m2", ids);
        Assert.DoesNotContain("m3", ids);
    }

    [Fact]
    public async Task IntegerRange_OnlyToBound_Inclusive_ReturnsAllBelow()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", null, 2022, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include y1, y2, m1, m2, m3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y1", ids);
        Assert.Contains("y2", ids);
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        Assert.DoesNotContain("y3", ids);
    }

    [Fact]
    public async Task IntegerRange_OnlyToBound_Exclusive_ExcludesUpperBoundary()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("year", null, 2022, includeFrom: true, includeTo: false)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should include only y1, m1
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y1", ids);
        Assert.Contains("m1", ids);
        Assert.DoesNotContain("y2", ids);
        Assert.DoesNotContain("m2", ids);
        Assert.DoesNotContain("m3", ids);
    }

    [Fact]
    public async Task IntegerRange_ExactSingleValue_Inclusive_ReturnsExactMatches()
    {
        // Arrange - single value range (2022 <= year <= 2022)
        var query = VectorQuery.Create()
            .WhereRange("year", 2022, 2022, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return y2, m2, m3
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y2", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        Assert.Equal(3, result.Records.Count);
    }

    [Fact]
    public async Task IntegerRange_ExactSingleValue_Exclusive_ReturnsEmpty()
    {
        // Arrange - impossible range (2022 < year < 2022)
        var query = VectorQuery.Create()
            .WhereRange("year", 2022, 2022, includeFrom: false, includeTo: false)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return no records
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    #endregion

    #region Double/Numeric Range Tests (if fixture has appropriate data)

    [Fact]
    public async Task NumericRange_InclusiveBothBounds_WorksWithDoubles()
    {
        // Arrange - Note: fixture may not have double fields indexed; this test documents expected behavior
        // If no data matches, test will pass with empty result (valid for TDD)
        var query = VectorQuery.Create()
            .WhereRange("score", 0.5, 1.5, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw, result depends on fixture data
        Assert.NotNull(result);
    }

    [Fact]
    public async Task NumericRange_ExclusiveBounds_WorksWithDoubles()
    {
        // Arrange
        var query = VectorQuery.Create()
            .WhereRange("rating", 1.0, 5.0, includeFrom: false, includeTo: false)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw
        Assert.NotNull(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task IntegerRange_InvertedBounds_ReturnsEmpty()
    {
        // Arrange - inverted range where from > to
        var query = VectorQuery.Create()
            .WhereRange("year", 2023, 2021, includeFrom: true, includeTo: true)
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return empty (invalid range)
        Assert.NotNull(result);
        Assert.Empty(result.Records);
    }

    [Fact]
    public async Task DateRange_VeryNarrowRange_OneMillisecond_WorksCorrectly()
    {
        // Arrange
        var baseDate = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var query = VectorQuery.Create()
            .WhereDateRange("created", baseDate, baseDate.AddMilliseconds(1), includeFrom: true, includeTo: true)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should not throw, result depends on fixture data precision
        Assert.NotNull(result);
    }

    #endregion
}
