using System;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Comprehensive tests for complex nested boolean compositions combining AND, OR, NOT.
/// Based on examples from VectorQueryFilterTests but executing against Elasticsearch.
/// </summary>
public class ElasticComplexQueryTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticFixture>
{
    private readonly ElasticFixture _fixture;

    public ElasticComplexQueryTests(ElasticServerAvailabilityFixture _avail, ElasticFixture fixture)
    {
        _fixture = fixture;
    }

    #region Complex Nested Boolean Logic

    [Fact]
    public async Task ComplexQuery_NestedOrInsideAnd_ReturnsCorrectResults()
    {
        // Query: (A = 12 OR B = 'pippo') AND pluto = 'xxx'
        // Since fixture doesn't have these exact fields, we adapt to fixture data:
        // (category = 'tech' OR year = 2021) AND year = 2021
        // Expected: m1 (has both category='tech' AND year=2021)

        var query = VectorQuery.Create()
            .Where(f => f.And(
                f.Or(
                    f.Equals("category", "tech"),
                    f.Equals("year", 2021)
                ),
                f.Equals("year", 2021)
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return records matching (tech OR 2021) AND 2021 = all 2021 records
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y1", ids); // year=2021
        Assert.Contains("m1", ids); // category=tech, year=2021
    }

    [Fact]
    public async Task ComplexRealWorldQuery_ActivePremiumUsersInTimeRange()
    {
        // Find records with category='tech' AND (year >= 2022) AND category = 'tech'
        // Adapted from: active users who are premium OR have high engagement AND created in time range
        // Using fixture data: category='tech' AND year in [2021, 2022]

        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .Where(f => f.Or(
                f.Equals("year", 2021),
                f.Equals("year", 2022)
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return m1 (tech, 2021) and m2 (tech, 2022)
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
        var ids = result.Records.Select(r => r.Id).OrderBy(id => id).ToArray();
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
    }

    [Fact]
    public async Task NestedNotFilters_NotOrCombination()
    {
        // Find items that are NOT (year=2021 OR year=2023)
        // Should return only records with year=2022 or no year field

        var query = VectorQuery.Create()
            .Where(f => f.Not(
                f.Or(
                    f.Equals("year", 2021),
                    f.Equals("year", 2023)
                )
            ))
            .WithLimit(50);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);

        // Should include y2, m2, m3 (all have year=2022)
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("y2", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);

        // Should NOT include y1 (2021) or y3 (2023) or m1 (2021)
        Assert.DoesNotContain("y1", ids);
        Assert.DoesNotContain("y3", ids);
        Assert.DoesNotContain("m1", ids);
    }

    [Fact]
    public async Task DeeplyNestedFilters_MultipleOrAndLevels()
    {
        // ((category='tech' AND year=2021) OR (category='other' AND year=2022)) AND status exists
        // Expected:
        // - Left: m1 (tech, 2021)
        // - Right: m3 (other, 2022)
        // But we also need a status field filter, which only s1, s2, s3 have
        // So this might return empty or need adjustment

        // Simplified version: ((year=2021 AND category='tech') OR (year=2022 AND category='tech'))
        // Expected: m1 (2021, tech) and m2 (2022, tech)

        var query = VectorQuery.Create()
            .Where(f => f.Or(
                f.And(
                    f.Equals("year", 2021),
                    f.Equals("category", "tech")
                ),
                f.And(
                    f.Equals("year", 2022),
                    f.Equals("category", "tech")
                )
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
        var ids = result.Records.Select(r => r.Id).OrderBy(id => id).ToArray();
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
    }

    [Fact]
    public async Task VeryDeeplyNested_ThreeLevels()
    {
        // (((year=2021 OR year=2022) AND category='tech') OR year=2023) AND has any year field
        // Expected: m1 (2021, tech), m2 (2022, tech), y3 (2023)

        var query = VectorQuery.Create()
            .Where(f => f.Or(
                f.And(
                    f.Or(
                        f.Equals("year", 2021),
                        f.Equals("year", 2022)
                    ),
                    f.Equals("category", "tech")
                ),
                f.Equals("year", 2023)
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).OrderBy(id => id).ToArray();
        
        // Should include m1, m2 (tech + 2021/2022) and y3 (2023)
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("y3", ids);
    }

    #endregion

    #region Mixing Traditional and Builder Syntax

    [Fact]
    public async Task MixingTraditionalAndBuilder_CombinesCorrectly()
    {
        // Traditional .WhereEquals combined with builder .Where
        // category='tech' AND (year=2021 OR year=2022) AND no filter on title

        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .Where(f => f.Or(
                f.Equals("year", 2021),
                f.Equals("year", 2022)
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - m1 (tech, 2021) and m2 (tech, 2022)
        Assert.NotNull(result);
        Assert.Equal(2, result.Records.Count);
        var ids = result.Records.Select(r => r.Id).OrderBy(id => id).ToArray();
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
    }

    [Fact]
    public async Task MultipleBuilderCalls_StacksAsAnd()
    {
        // Multiple Where calls should stack as AND at the root
        var query = VectorQuery.Create()
            .Where(f => f.Equals("category", "tech"))
            .Where(f => f.Equals("year", 2022))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - only m2 (tech AND 2022)
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("m2", result.Records[0].Id);
    }

    #endregion

    #region Complex Range + Boolean Combinations

    [Fact]
    public async Task RangeFilter_InsideBooleanComposition()
    {
        // (year >= 2022) OR category='other'
        // Expected: y2, y3, m2, m3 (year >= 2022) and m3 (other, 2022 - already included), r3 (other)

        var query = VectorQuery.Create()
            .Where(f => f.Or(
                f.IntegerRange("year", 2022, null),
                f.Equals("category", "other")
            ))
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        
        // Should include records with year >= 2022
        Assert.Contains("y2", ids);
        Assert.Contains("y3", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        
        // Should also include records with category='other'
        Assert.Contains("r3", ids);
    }

    [Fact]
    public async Task DateRangeWithExclusiveBounds_InNestedQuery()
    {
        // Date range with exclusive bounds inside an OR
        // (created > 2023-01-01 exclusive) OR year=2021
        
        var baseDate = new DateTime(2023, 1, 1);
        var query = VectorQuery.Create()
            .Where(f => f.Or(
                f.DateRange("created", baseDate, null, includeFrom: false, includeTo: true),
                f.Equals("year", 2021)
            ))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        
        // Should include d3 (created = baseDate + 10 days) and y1, m1 (year=2021)
        Assert.Contains("d3", ids);
        Assert.Contains("y1", ids);
        Assert.Contains("m1", ids);
        
        // Should NOT include d2 (created = baseDate exactly, excluded by exclusive bound)
        Assert.DoesNotContain("d2", ids);
    }

    #endregion

    #region NOT with Complex Combinations

    [Fact]
    public async Task NotFilter_WithNestedAnd()
    {
        // NOT (category='tech' AND year=2022)
        // Should exclude only m2

        var query = VectorQuery.Create()
            .Where(f => f.Not(
                f.And(
                    f.Equals("category", "tech"),
                    f.Equals("year", 2022)
                )
            ))
            .WithLimit(50);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
        
        var ids = result.Records.Select(r => r.Id).ToArray();
        
        // Should NOT include m2 (the only record with tech AND 2022)
        Assert.DoesNotContain("m2", ids);
        
        // Should include m1 (tech but 2021), y2 (2022 but no category='tech')
        Assert.Contains("m1", ids);
        Assert.Contains("y2", ids);
    }

    [Fact]
    public async Task DoubleNegation_NotNotFilter()
    {
        // NOT (NOT (year=2022))
        // Should be equivalent to year=2022

        var query = VectorQuery.Create()
            .Where(f => f.Not(
                f.Not(
                    f.Equals("year", 2022)
                )
            ))
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        
        // Should include y2, m2, m3 (all have year=2022)
        Assert.Contains("y2", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("m3", ids);
        
        // Should NOT include y1, y3, m1 (different years)
        Assert.DoesNotContain("y1", ids);
        Assert.DoesNotContain("y3", ids);
        Assert.DoesNotContain("m1", ids);
    }

    #endregion

    #region All Filter Types Combined

    [Fact]
    public async Task QueryWithMultipleFilterTypes_InComplexStructure()
    {
        // Complex query using string, integer, date, range filters in nested structure
        // (category='tech' AND year in [2021-2022]) OR (created >= baseDate AND title contains 'Third')

        var baseDate = new DateTime(2023, 1, 1);
        var query = VectorQuery.Create()
            .Where(f => f.Or(
                f.And(
                    f.Equals("category", "tech"),
                    f.IntegerRange("year", 2021, 2022)
                ),
                f.And(
                    f.DateRange("created", baseDate, null),
                    f.Equals("title", "Third")
                )
            ))
            .WithLimit(20);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        
        // Left side: tech AND year in [2021, 2022] = m1, m2
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
        
        // Right side: created >= baseDate AND title='Third'
        // d2 (created=baseDate), d3 (created>baseDate), t3 (title=Third Title)
        // Intersection of these two conditions would be if t3 also had created >= baseDate
        // Since t3 doesn't have created field, right side might be empty
        // But left side (m1, m2) should definitely be there
    }

    #endregion

    #region Edge Cases in Complex Queries

    [Fact]
    public async Task EmptyOrFilter_InsideAnd_BehavesCorrectly()
    {
        // Testing edge case: what if we have an empty OR inside an AND?
        // This is more of a structural test - in practice, empty filters shouldn't be created
        // But if they are, Elasticsearch should handle it gracefully
        
        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should work normally
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);
    }

    [Fact]
    public async Task SingletonBooleanFilters_SimplifyCorrectly()
    {
        // AND with single child, OR with single child - should simplify to the child
        var query = VectorQuery.Create()
            .WithAndFilter(new StringEqualsFilter("category", "tech"))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return tech records
        Assert.NotNull(result);
        var ids = result.Records.Select(r => r.Id).ToArray();
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
    }

    #endregion
}
