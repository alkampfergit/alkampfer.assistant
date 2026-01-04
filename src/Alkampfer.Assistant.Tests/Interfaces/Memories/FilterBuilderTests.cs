using System;
using System.Linq;
using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Interfaces.Memories;

public class FilterBuilderTests
{
    [Fact]
    public void And_WithMultipleFilters_ShouldCreateAndFilter()
    {
        // Arrange
        var builder = new FilterBuilder();
        var filter1 = new IntegerEqualsFilter("age", 25);
        var filter2 = new StringEqualsFilter("name", "John");

        // Act
        var result = builder.And(filter1, filter2);

        // Assert
        Assert.NotNull(result);
        var andFilter = Assert.IsType<AndFilter>(result);
        Assert.Equal(2, andFilter.Filters.Count);
        Assert.Contains(filter1, andFilter.Filters);
        Assert.Contains(filter2, andFilter.Filters);
    }

    [Fact]
    public void And_WithNoFilters_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.And());
    }

    [Fact]
    public void And_WithNullFilters_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.And(null));
    }

    [Fact]
    public void Or_WithMultipleFilters_ShouldCreateOrFilter()
    {
        // Arrange
        var builder = new FilterBuilder();
        var filter1 = new IntegerEqualsFilter("age", 25);
        var filter2 = new IntegerEqualsFilter("age", 30);

        // Act
        var result = builder.Or(filter1, filter2);

        // Assert
        Assert.NotNull(result);
        var orFilter = Assert.IsType<OrFilter>(result);
        Assert.Equal(2, orFilter.Filters.Count);
        Assert.Contains(filter1, orFilter.Filters);
        Assert.Contains(filter2, orFilter.Filters);
    }

    [Fact]
    public void Or_WithNoFilters_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.Or());
    }

    [Fact]
    public void Not_WithFilter_ShouldCreateNotFilter()
    {
        // Arrange
        var builder = new FilterBuilder();
        var filter = new BooleanEqualsFilter("active", true);

        // Act
        var result = builder.Not(filter);

        // Assert
        Assert.NotNull(result);
        var notFilter = Assert.IsType<NotFilter>(result);
        Assert.Equal(filter, notFilter.Filter);
    }

    [Fact]
    public void Not_WithNullFilter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Not(null));
    }

    [Fact]
    public void Equals_WithStringValue_ShouldCreateStringEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.Equals("name", "John");

        // Assert
        var filter = Assert.IsType<StringEqualsFilter>(result);
        Assert.Equal("name", filter.FieldName);
        Assert.Equal("John", filter.Value);
    }

    [Fact]
    public void Equals_WithIntValue_ShouldCreateIntegerEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.Equals("age", 25);

        // Assert
        var filter = Assert.IsType<IntegerEqualsFilter>(result);
        Assert.Equal("age", filter.FieldName);
        Assert.Equal(25, filter.Value);
    }

    [Fact]
    public void Equals_WithDoubleValue_ShouldCreateDoubleEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.Equals("score", 9.5);

        // Assert
        var filter = Assert.IsType<DoubleEqualsFilter>(result);
        Assert.Equal("score", filter.FieldName);
        Assert.Equal(9.5, filter.Value);
    }

    [Fact]
    public void Equals_WithBoolValue_ShouldCreateBooleanEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.Equals("active", true);

        // Assert
        var filter = Assert.IsType<BooleanEqualsFilter>(result);
        Assert.Equal("active", filter.FieldName);
        Assert.True(filter.Value);
    }

    [Fact]
    public void Equals_WithDateTimeValue_ShouldCreateDateTimeEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();
        var date = new DateTime(2023, 1, 15);

        // Act
        var result = builder.Equals("created", date);

        // Assert
        var filter = Assert.IsType<DateTimeEqualsFilter>(result);
        Assert.Equal("created", filter.FieldName);
        Assert.Equal(date, filter.Value);
    }

    [Fact]
    public void KeywordEquals_ShouldCreateKeywordEqualsFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.KeywordEquals("category", "Technology");

        // Assert
        var filter = Assert.IsType<KeywordEqualsFilter>(result);
        Assert.Equal("category", filter.FieldName);
        Assert.Equal("Technology", filter.Value);
    }

    [Fact]
    public void DateRange_ShouldCreateDateTimeRangeFilter()
    {
        // Arrange
        var builder = new FilterBuilder();
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 12, 31);

        // Act
        var result = builder.DateRange("created", from, to);

        // Assert
        var filter = Assert.IsType<DateTimeRangeFilter>(result);
        Assert.Equal("created", filter.FieldName);
        Assert.Equal(from, filter.From);
        Assert.Equal(to, filter.To);
    }

    [Fact]
    public void NumericRange_ShouldCreateNumericRangeFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.NumericRange("score", 5.0, 10.0);

        // Assert
        var filter = Assert.IsType<NumericRangeFilter>(result);
        Assert.Equal("score", filter.FieldName);
        Assert.Equal(5.0, filter.From);
        Assert.Equal(10.0, filter.To);
    }

    [Fact]
    public void IntegerRange_ShouldCreateIntegerRangeFilter()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act
        var result = builder.IntegerRange("age", 18, 65);

        // Assert
        var filter = Assert.IsType<IntegerRangeFilter>(result);
        Assert.Equal("age", filter.FieldName);
        Assert.Equal(18, filter.From);
        Assert.Equal(65, filter.To);
    }

    [Fact]
    public void ComplexNestedFilter_ShouldBuildCorrectStructure()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act: (A = 12 OR B = 'pippo') AND pluto = 'xxx'
        var result = builder.And(
            builder.Or(
                builder.Equals("A", 12),
                builder.Equals("B", "pippo")
            ),
            builder.Equals("pluto", "xxx")
        );

        // Assert
        var andFilter = Assert.IsType<AndFilter>(result);
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
    public void ComplexFilterWithNot_ShouldBuildCorrectStructure()
    {
        // Arrange
        var builder = new FilterBuilder();

        // Act: A = 1 AND NOT (B = 2 OR C = 3)
        var result = builder.And(
            builder.Equals("A", 1),
            builder.Not(
                builder.Or(
                    builder.Equals("B", 2),
                    builder.Equals("C", 3)
                )
            )
        );

        // Assert
        var andFilter = Assert.IsType<AndFilter>(result);
        Assert.Equal(2, andFilter.Filters.Count);

        var intFilter = Assert.IsType<IntegerEqualsFilter>(andFilter.Filters[0]);
        Assert.Equal("A", intFilter.FieldName);
        Assert.Equal(1, intFilter.Value);

        var notFilter = Assert.IsType<NotFilter>(andFilter.Filters[1]);
        var orFilter = Assert.IsType<OrFilter>(notFilter.Filter);
        Assert.Equal(2, orFilter.Filters.Count);
    }
}
