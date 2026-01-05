using System;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration.ElasticSearch;

/// <summary>
/// Tests for boolean composition filters (AND/OR/NOT).
/// The converter supports composite filters using Elasticsearch BoolQuery.
/// </summary>
public class ElasticBooleanCompositionTests : IClassFixture<ElasticServerAvailabilityFixture>, IClassFixture<ElasticFixture>
{
    private readonly ElasticFixture _fixture;

    public ElasticBooleanCompositionTests(ElasticServerAvailabilityFixture _avail, ElasticFixture fixture)
    {
        // Availability fixture is used to fail fast in constructor if server is missing/unreachable.
        _fixture = fixture;
    }

    [Fact]
    public async Task OrFilter_ReturnsRecordsMatchingEitherCondition()
    {
        // Arrange - query for records with category="tech" OR year=2021
        // category="tech" matches: m1, m2 (both have category="tech")
        // year=2021 matches: y1 (year=2021), m1 (year=2021)
        // Expected: m1 (matches both), m2 (category), y1 (year) = 3 records
        var query = VectorQuery.Create()
            .WithOrFilter(new StringEqualsFilter("category", "tech"), new IntegerEqualsFilter("year", 2021))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Records.Count);
        var ids = result.Records.Select(r => r.Id).OrderBy(id => id).ToArray();
        Assert.Contains("m1", ids);
        Assert.Contains("m2", ids);
        Assert.Contains("y1", ids);
    }

    [Fact]
    public async Task AndFilter_ReturnsRecordsMatchingBothConditions()
    {
        // Arrange - query for records with category="tech" AND year=2022
        // Expected: only m2 (category=tech AND year=2022)
        var query = VectorQuery.Create()
            .WithAndFilter(new StringEqualsFilter("category", "tech"), new IntegerEqualsFilter("year", 2022))
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("m2", result.Records[0].Id);
    }

    [Fact]
    public async Task NotFilter_ReturnsRecordsNotMatchingCondition()
    {
        // Arrange - query for records NOT having status="active"
        // Test data has s1 and s3 with status=Active, s2 with status=Inactive
        // Expected: s2 (status=Inactive)
        var inner = new KeywordEqualsFilter("status", "Active");
        var query = VectorQuery.Create()
            .WithNotFilter(inner)
            .WithLimit(10);

        // Act
        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        // Assert - should return all records that don't have status="Active"
        // This includes s2 (status=Inactive) and all records without a status field
        Assert.NotNull(result);
        Assert.True(result.Records.Count > 0);

        // Verify s2 is in the results (has status=Inactive)
        Assert.Contains(result.Records, r => r.Id == "s2");

        // Verify s1 and s3 are NOT in the results (they have status=Active)
        Assert.DoesNotContain(result.Records, r => r.Id == "s1");
        Assert.DoesNotContain(result.Records, r => r.Id == "s3");
    }

    [Fact]
    public async Task NestedBoolean_PrecedenceAndShould_ReturnsExpectedIntersection()
    {
        // Test nested boolean logic: ((category == "tech" OR title == "Second Title") AND year == 2022)
        // m2 has category="tech" and year=2022 - should match
        // t2 has title="Second Title" but no year field - should NOT match
        // Expected: single record with id "m2"

        var orFilter = new OrFilter(new IQueryFilter[] {
            new StringEqualsFilter("category", "tech"),
            new StringEqualsFilter("title", "Second Title")
        });

        var query = VectorQuery.Create()
            .WithAndFilter(orFilter, new IntegerEqualsFilter("year", 2022))
            .WithLimit(10);

        var result = await _fixture.QueryExecutor.ExecuteQueryAsync(_fixture.TestIndexName, query);

        Assert.NotNull(result);
        Assert.Single(result.Records);
        Assert.Equal("m2", result.Records[0].Id);
    }
}
