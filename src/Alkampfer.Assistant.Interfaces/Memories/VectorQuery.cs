using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Represents a query for VectorRecord with filters and search parameters.
/// </summary>
public class VectorQuery : IVectorQuery
{
    private readonly List<IQueryFilter> _filters = new();

    /// <summary>
    /// Gets or sets the full-text search query.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the number of results to skip.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Gets the collection of filters applied to this query.
    /// </summary>
    public IReadOnlyList<IQueryFilter> Filters => _filters;

    /// <summary>
    /// Creates a new VectorQuery instance.
    /// </summary>
    public static VectorQuery Create()
    {
        return new VectorQuery();
    }

    /// <summary>
    /// Sets the full-text search query.
    /// </summary>
    public IVectorQuery WithSearchText(string searchText)
    {
        SearchText = searchText;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of results to return.
    /// </summary>
    public IVectorQuery WithLimit(int limit)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0.", nameof(limit));
        Limit = limit;
        return this;
    }

    /// <summary>
    /// Sets the number of results to skip.
    /// </summary>
    public IVectorQuery WithSkip(int skip)
    {
        if (skip < 0)
            throw new ArgumentException("Skip must be non-negative.", nameof(skip));
        Skip = skip;
        return this;
    }

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    public IVectorQuery WithFilter(IQueryFilter? filter)
    {
        if (filter != null)
        {
            _filters.Add(filter);
        }
        return this;
    }

    /// <summary>
    /// Adds a string equality filter.
    /// </summary>
    public IVectorQuery WhereEquals(string fieldName, string? value)
    {
        if (value != null)
        {
            _filters.Add(new StringEqualsFilter(fieldName, value));
        }
        return this;
    }

    /// <summary>
    /// Adds a keyword equality filter (case-insensitive).
    /// </summary>
    public IVectorQuery WhereKeywordEquals(string fieldName, string? value)
    {
        if (value != null)
        {
            _filters.Add(new KeywordEqualsFilter(fieldName, value));
        }
        return this;
    }

    /// <summary>
    /// Adds an integer equality filter.
    /// </summary>
    public IVectorQuery WhereEquals(string fieldName, int? value)
    {
        if (value.HasValue)
        {
            _filters.Add(new IntegerEqualsFilter(fieldName, value.Value));
        }
        return this;
    }

    /// <summary>
    /// Adds a double equality filter.
    /// </summary>
    public IVectorQuery WhereEquals(string fieldName, double? value)
    {
        if (value.HasValue)
        {
            _filters.Add(new DoubleEqualsFilter(fieldName, value.Value));
        }
        return this;
    }

    /// <summary>
    /// Adds a boolean equality filter.
    /// </summary>
    public IVectorQuery WhereEquals(string fieldName, bool? value)
    {
        if (value.HasValue)
        {
            _filters.Add(new BooleanEqualsFilter(fieldName, value.Value));
        }
        return this;
    }

    /// <summary>
    /// Adds a DateTime equality filter.
    /// </summary>
    public IVectorQuery WhereEquals(string fieldName, DateTime? value)
    {
        if (value.HasValue)
        {
            _filters.Add(new DateTimeEqualsFilter(fieldName, value.Value));
        }
        return this;
    }

    /// <summary>
    /// Adds a DateTime range filter.
    /// </summary>
    public IVectorQuery WhereDateRange(string fieldName, DateTime? from, DateTime? to)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new DateTimeRangeFilter(fieldName, from, to));
        }
        return this;
    }

    /// <summary>
    /// Adds a numeric range filter.
    /// </summary>
    public IVectorQuery WhereRange(string fieldName, double? from, double? to)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new NumericRangeFilter(fieldName, from, to));
        }
        return this;
    }

    /// <summary>
    /// Adds an integer range filter.
    /// </summary>
    public IVectorQuery WhereRange(string fieldName, int? from, int? to)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new IntegerRangeFilter(fieldName, from, to));
        }
        return this;
    }
}
