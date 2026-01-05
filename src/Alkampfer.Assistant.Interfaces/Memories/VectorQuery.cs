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
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IVectorQuery WhereDateRange(string fieldName, DateTime? from, DateTime? to, bool includeFrom = true, bool includeTo = true)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new DateTimeRangeFilter(fieldName, from, to, includeFrom, includeTo));
        }
        return this;
    }

    /// <summary>
    /// Adds a numeric range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IVectorQuery WhereRange(string fieldName, double? from, double? to, bool includeFrom = true, bool includeTo = true)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new NumericRangeFilter(fieldName, from, to, includeFrom, includeTo));
        }
        return this;
    }

    /// <summary>
    /// Adds an integer range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IVectorQuery WhereRange(string fieldName, int? from, int? to, bool includeFrom = true, bool includeTo = true)
    {
        if (from.HasValue || to.HasValue)
        {
            _filters.Add(new IntegerRangeFilter(fieldName, from, to, includeFrom, includeTo));
        }
        return this;
    }

    /// <summary>
    /// Adds an OR composite filter combining multiple filters.
    /// </summary>
    public IVectorQuery WithOrFilter(params IQueryFilter[] filters)
    {
        if (filters != null && filters.Length > 0)
        {
            _filters.Add(new OrFilter(filters.ToList()));
        }
        return this;
    }

    /// <summary>
    /// Adds an AND composite filter combining multiple filters.
    /// </summary>
    public IVectorQuery WithAndFilter(params IQueryFilter[] filters)
    {
        if (filters != null && filters.Length > 0)
        {
            _filters.Add(new AndFilter(filters.ToList()));
        }
        return this;
    }

    /// <summary>
    /// Adds a NOT filter that negates another filter.
    /// </summary>
    public IVectorQuery WithNotFilter(IQueryFilter filter)
    {
        if (filter != null)
        {
            _filters.Add(new NotFilter(filter));
        }
        return this;
    }

    /// <summary>
    /// Adds a filter using the FilterBuilder for complex query construction.
    /// </summary>
    public IVectorQuery Where(Func<FilterBuilder, IQueryFilter> buildFilter)
    {
        if (buildFilter != null)
        {
            var builder = new FilterBuilder();
            var filter = buildFilter(builder);
            if (filter != null)
            {
                _filters.Add(filter);
            }
        }
        return this;
    }
}
