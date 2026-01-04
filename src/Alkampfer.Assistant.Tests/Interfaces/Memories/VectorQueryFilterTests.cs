using System;
using System.Linq;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories;

public class VectorQueryFilterTests
{
    [Fact]
    public void WithOrFilter_WithMultipleFilters_ShouldAddOrFilterToQuery()
    {
        // Arrange
        var query = VectorQuery.Create();
        var filter1 = new IntegerEqualsFilter("age", 25);
        var filter2 = new IntegerEqualsFilter("age", 30);

        // Act
        var result = query.WithOrFilter(filter1, filter2);

        // Assert
        Assert.Same(query, result); // Fluent interface
        Assert.Single(query.Filters);
        var orFilter = Assert.IsType<OrFilter>(query.Filters[0]);
        Assert.Equal(2, orFilter.Filters.Count);
        Assert.Contains(filter1, orFilter.Filters);
        Assert.Contains(filter2, orFilter.Filters);
    }

    [Fact]
    public void WithOrFilter_WithNullFilters_ShouldNotAddFilter()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act
        var result = query.WithOrFilter(null);

        // Assert
        Assert.Same(query, result);
        Assert.Empty(query.Filters);
    }

    [Fact]
    public void WithOrFilter_WithEmptyArray_ShouldNotAddFilter()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act
        var result = query.WithOrFilter();

        // Assert
        Assert.Same(query, result);
        Assert.Empty(query.Filters);
    }

    [Fact]
    public void WithAndFilter_WithMultipleFilters_ShouldAddAndFilterToQuery()
    {
        // Arrange
        var query = VectorQuery.Create();
        var filter1 = new StringEqualsFilter("name", "John");
        var filter2 = new IntegerEqualsFilter("age", 25);

        // Act
        var result = query.WithAndFilter(filter1, filter2);

        // Assert
        Assert.Same(query, result);
        Assert.Single(query.Filters);
        var andFilter = Assert.IsType<AndFilter>(query.Filters[0]);
        Assert.Equal(2, andFilter.Filters.Count);
        Assert.Contains(filter1, andFilter.Filters);
        Assert.Contains(filter2, andFilter.Filters);
    }

    [Fact]
    public void WithNotFilter_WithFilter_ShouldAddNotFilterToQuery()
    {
        // Arrange
        var query = VectorQuery.Create();
        var filter = new BooleanEqualsFilter("active", false);

        // Act
        var result = query.WithNotFilter(filter);

        // Assert
        Assert.Same(query, result);
        Assert.Single(query.Filters);
        var notFilter = Assert.IsType<NotFilter>(query.Filters[0]);
        Assert.Equal(filter, notFilter.Filter);
    }

    [Fact]
    public void WithNotFilter_WithNull_ShouldNotAddFilter()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act
        var result = query.WithNotFilter(null);

        // Assert
        Assert.Same(query, result);
        Assert.Empty(query.Filters);
    }

    [Fact]
    public void Where_WithFilterBuilder_ShouldAddFilter()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act
        var result = query.Where(f => f.Equals("name", "John"));

        // Assert
        Assert.Same(query, result);
        Assert.Single(query.Filters);
        var filter = Assert.IsType<StringEqualsFilter>(query.Filters[0]);
        Assert.Equal("name", filter.FieldName);
        Assert.Equal("John", filter.Value);
    }

    [Fact]
    public void Where_WithComplexFilterBuilder_ShouldBuildCorrectStructure()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act: (A = 12 OR B = 'pippo') AND pluto = 'xxx'
        var result = query.Where(f => f.And(
            f.Or(
                f.Equals("A", 12),
                f.Equals("B", "pippo")
            ),
            f.Equals("pluto", "xxx")
        ));

        // Assert
        Assert.Same(query, result);
        Assert.Single(query.Filters);

        var andFilter = Assert.IsType<AndFilter>(query.Filters[0]);
        Assert.Equal(2, andFilter.Filters.Count);

        var orFilter = Assert.IsType<OrFilter>(andFilter.Filters[0]);
        Assert.Equal(2, orFilter.Filters.Count);

        var intFilter = Assert.IsType<IntegerEqualsFilter>(orFilter.Filters[0]);
        Assert.Equal("A", intFilter.FieldName);
        Assert.Equal(12, intFilter.Value);

        var strFilter1 = Assert.IsType<StringEqualsFilter>(orFilter.Filters[1]);
        Assert.Equal("B", strFilter1.FieldName);
        Assert.Equal("pippo", strFilter1.Value);

        var strFilter2 = Assert.IsType<StringEqualsFilter>(andFilter.Filters[1]);
        Assert.Equal("pluto", strFilter2.FieldName);
        Assert.Equal("xxx", strFilter2.Value);
    }

    [Fact]
    public void Where_WithNullBuilder_ShouldNotAddFilter()
    {
        // Arrange
        var query = VectorQuery.Create();

        // Act
        var result = query.Where(null);

        // Assert
        Assert.Same(query, result);
        Assert.Empty(query.Filters);
    }

    [Fact]
    public void CombiningMultipleFilters_ShouldCreateImplicitAndAtRootLevel()
    {
        // Arrange & Act
        var query = VectorQuery.Create()
            .WhereEquals("name", "John")
            .WithOrFilter(
                new IntegerEqualsFilter("age", 25),
                new IntegerEqualsFilter("age", 30)
            )
            .WhereEquals("active", true);

        // Assert
        Assert.Equal(3, query.Filters.Count);

        var stringFilter = Assert.IsType<StringEqualsFilter>(query.Filters[0]);
        Assert.Equal("name", stringFilter.FieldName);

        var orFilter = Assert.IsType<OrFilter>(query.Filters[1]);
        Assert.Equal(2, orFilter.Filters.Count);

        var boolFilter = Assert.IsType<BooleanEqualsFilter>(query.Filters[2]);
        Assert.Equal("active", boolFilter.FieldName);
    }

    [Fact]
    public void MixingTraditionalAndBuilderSyntax_ShouldWork()
    {
        // Arrange & Act
        var query = VectorQuery.Create()
            .WhereEquals("category", "tech")
            .Where(f => f.Or(
                f.Equals("priority", 1),
                f.Equals("priority", 2)
            ))
            .WhereKeywordEquals("status", "active");

        // Assert
        Assert.Equal(3, query.Filters.Count);
        Assert.IsType<StringEqualsFilter>(query.Filters[0]);
        Assert.IsType<OrFilter>(query.Filters[1]);
        Assert.IsType<KeywordEqualsFilter>(query.Filters[2]);
    }

    [Fact]
    public void ComplexRealWorldQuery_ShouldBuildCorrectly()
    {
        // Arrange & Act:
        // Find active users who are either premium OR have high engagement
        // AND created in the last year
        var query = VectorQuery.Create()
            .WhereEquals("active", true)
            .Where(f => f.Or(
                f.Equals("subscription", "premium"),
                f.NumericRange("engagement_score", 80.0, null)
            ))
            .WhereDateRange("created_at",
                new DateTime(2023, 1, 1),
                new DateTime(2023, 12, 31));

        // Assert
        Assert.Equal(3, query.Filters.Count);

        var activeFilter = Assert.IsType<BooleanEqualsFilter>(query.Filters[0]);
        Assert.True(activeFilter.Value);

        var orFilter = Assert.IsType<OrFilter>(query.Filters[1]);
        Assert.Equal(2, orFilter.Filters.Count);

        var dateFilter = Assert.IsType<DateTimeRangeFilter>(query.Filters[2]);
        Assert.Equal("created_at", dateFilter.FieldName);
    }

    [Fact]
    public void NestedNotFilters_ShouldBuildCorrectly()
    {
        // Arrange & Act:
        // Find items that are NOT (inactive OR deleted)
        var query = VectorQuery.Create()
            .Where(f => f.Not(
                f.Or(
                    f.Equals("status", "inactive"),
                    f.Equals("status", "deleted")
                )
            ));

        // Assert
        Assert.Single(query.Filters);

        var notFilter = Assert.IsType<NotFilter>(query.Filters[0]);
        var orFilter = Assert.IsType<OrFilter>(notFilter.Filter);
        Assert.Equal(2, orFilter.Filters.Count);

        var filter1 = Assert.IsType<StringEqualsFilter>(orFilter.Filters[0]);
        Assert.Equal("inactive", filter1.Value);

        var filter2 = Assert.IsType<StringEqualsFilter>(orFilter.Filters[1]);
        Assert.Equal("deleted", filter2.Value);
    }

    [Fact]
    public void DeeplyNestedFilters_ShouldBuildCorrectly()
    {
        // Arrange & Act:
        // ((A AND B) OR (C AND D)) AND E
        var query = VectorQuery.Create()
            .Where(f => f.And(
                f.Or(
                    f.And(
                        f.Equals("A", 1),
                        f.Equals("B", 2)
                    ),
                    f.And(
                        f.Equals("C", 3),
                        f.Equals("D", 4)
                    )
                ),
                f.Equals("E", 5)
            ));

        // Assert
        Assert.Single(query.Filters);

        var rootAnd = Assert.IsType<AndFilter>(query.Filters[0]);
        Assert.Equal(2, rootAnd.Filters.Count);

        var orFilter = Assert.IsType<OrFilter>(rootAnd.Filters[0]);
        Assert.Equal(2, orFilter.Filters.Count);

        var leftAnd = Assert.IsType<AndFilter>(orFilter.Filters[0]);
        Assert.Equal(2, leftAnd.Filters.Count);

        var rightAnd = Assert.IsType<AndFilter>(orFilter.Filters[1]);
        Assert.Equal(2, rightAnd.Filters.Count);

        var eFilter = Assert.IsType<IntegerEqualsFilter>(rootAnd.Filters[1]);
        Assert.Equal("E", eFilter.FieldName);
        Assert.Equal(5, eFilter.Value);
    }

    [Fact]
    public void QueryWithAllFilterTypes_ShouldWork()
    {
        // Arrange & Act
        var query = VectorQuery.Create()
            .Where(f => f.And(
                f.Equals("name", "John"),
                f.Equals("age", 25),
                f.Equals("score", 9.5),
                f.Equals("active", true),
                f.Equals("created", new DateTime(2023, 1, 1)),
                f.KeywordEquals("category", "tech"),
                f.DateRange("updated", new DateTime(2023, 1, 1), new DateTime(2023, 12, 31)),
                f.NumericRange("rating", 4.0, 5.0),
                f.IntegerRange("count", 10, 100)
            ));

        // Assert
        Assert.Single(query.Filters);
        var andFilter = Assert.IsType<AndFilter>(query.Filters[0]);
        Assert.Equal(9, andFilter.Filters.Count);
        Assert.IsType<StringEqualsFilter>(andFilter.Filters[0]);
        Assert.IsType<IntegerEqualsFilter>(andFilter.Filters[1]);
        Assert.IsType<DoubleEqualsFilter>(andFilter.Filters[2]);
        Assert.IsType<BooleanEqualsFilter>(andFilter.Filters[3]);
        Assert.IsType<DateTimeEqualsFilter>(andFilter.Filters[4]);
        Assert.IsType<KeywordEqualsFilter>(andFilter.Filters[5]);
        Assert.IsType<DateTimeRangeFilter>(andFilter.Filters[6]);
        Assert.IsType<NumericRangeFilter>(andFilter.Filters[7]);
        Assert.IsType<IntegerRangeFilter>(andFilter.Filters[8]);
    }
}
