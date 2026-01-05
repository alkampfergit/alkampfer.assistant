using System;
using System.Collections.Generic;
using System.Linq;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Fluent builder for constructing complex query filters with AND, OR, and NOT logic.
/// </summary>
public class FilterBuilder
{
    /// <summary>
    /// Creates an AND composite filter combining multiple filters.
    /// All filters must match.
    /// </summary>
    public IQueryFilter And(params IQueryFilter[] filters)
    {
        if (filters == null || filters.Length == 0)
            throw new ArgumentException("At least one filter must be provided for AND operation.", nameof(filters));

        return new AndFilter(filters.ToList());
    }

    /// <summary>
    /// Creates an OR composite filter combining multiple filters.
    /// At least one filter must match.
    /// </summary>
    public IQueryFilter Or(params IQueryFilter[] filters)
    {
        if (filters == null || filters.Length == 0)
            throw new ArgumentException("At least one filter must be provided for OR operation.", nameof(filters));

        return new OrFilter(filters.ToList());
    }

    /// <summary>
    /// Creates a NOT filter that negates another filter.
    /// </summary>
    public IQueryFilter Not(IQueryFilter filter)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        return new NotFilter(filter);
    }

    /// <summary>
    /// Creates a string equality filter (analyzed, full-text match).
    /// </summary>
    public IQueryFilter Equals(string fieldName, string value)
    {
        return new StringEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates a keyword equality filter (case-insensitive, exact match).
    /// </summary>
    public IQueryFilter KeywordEquals(string fieldName, string value)
    {
        return new KeywordEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates an integer equality filter.
    /// </summary>
    public IQueryFilter Equals(string fieldName, int value)
    {
        return new IntegerEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates a double equality filter.
    /// </summary>
    public IQueryFilter Equals(string fieldName, double value)
    {
        return new DoubleEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates a boolean equality filter.
    /// </summary>
    public IQueryFilter Equals(string fieldName, bool value)
    {
        return new BooleanEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates a DateTime equality filter.
    /// </summary>
    public IQueryFilter Equals(string fieldName, DateTime value)
    {
        return new DateTimeEqualsFilter(fieldName, value);
    }

    /// <summary>
    /// Creates a DateTime range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IQueryFilter DateRange(string fieldName, DateTime? from, DateTime? to, bool includeFrom = true, bool includeTo = true)
    {
        return new DateTimeRangeFilter(fieldName, from, to, includeFrom, includeTo);
    }

    /// <summary>
    /// Creates a numeric (double) range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IQueryFilter NumericRange(string fieldName, double? from, double? to, bool includeFrom = true, bool includeTo = true)
    {
        return new NumericRangeFilter(fieldName, from, to, includeFrom, includeTo);
    }

    /// <summary>
    /// Creates an integer range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    public IQueryFilter IntegerRange(string fieldName, int? from, int? to, bool includeFrom = true, bool includeTo = true)
    {
        return new IntegerRangeFilter(fieldName, from, to, includeFrom, includeTo);
    }
}
