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
    public IQueryFilter DateRange(string fieldName, DateTime? from, DateTime? to)
    {
        return new DateTimeRangeFilter(fieldName, from, to);
    }

    /// <summary>
    /// Creates a numeric (double) range filter.
    /// </summary>
    public IQueryFilter NumericRange(string fieldName, double? from, double? to)
    {
        return new NumericRangeFilter(fieldName, from, to);
    }

    /// <summary>
    /// Creates an integer range filter.
    /// </summary>
    public IQueryFilter IntegerRange(string fieldName, int? from, int? to)
    {
        return new IntegerRangeFilter(fieldName, from, to);
    }
}
