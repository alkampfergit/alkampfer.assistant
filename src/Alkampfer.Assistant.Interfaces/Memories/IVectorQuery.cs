using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Interface for building vector queries with filters and search parameters.
/// </summary>
public interface IVectorQuery
{
    /// <summary>
    /// Gets the full-text search query.
    /// </summary>
    string? SearchText { get; }

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    int? Limit { get; }

    /// <summary>
    /// Gets the number of results to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the collection of filters applied to this query.
    /// </summary>
    IReadOnlyList<IQueryFilter> Filters { get; }

    /// <summary>
    /// Sets the full-text search query.
    /// </summary>
    IVectorQuery WithSearchText(string searchText);

    /// <summary>
    /// Sets the maximum number of results to return.
    /// </summary>
    IVectorQuery WithLimit(int limit);

    /// <summary>
    /// Sets the number of results to skip.
    /// </summary>
    IVectorQuery WithSkip(int skip);

    /// <summary>
    /// Adds a filter to the query.
    /// </summary>
    IVectorQuery WithFilter(IQueryFilter? filter);

    /// <summary>
    /// Adds a string equality filter.
    /// </summary>
    IVectorQuery WhereEquals(string fieldName, string? value);

    /// <summary>
    /// Adds a keyword equality filter (case-insensitive).
    /// </summary>
    IVectorQuery WhereKeywordEquals(string fieldName, string? value);

    /// <summary>
    /// Adds an integer equality filter.
    /// </summary>
    IVectorQuery WhereEquals(string fieldName, int? value);

    /// <summary>
    /// Adds a double equality filter.
    /// </summary>
    IVectorQuery WhereEquals(string fieldName, double? value);

    /// <summary>
    /// Adds a boolean equality filter.
    /// </summary>
    IVectorQuery WhereEquals(string fieldName, bool? value);

    /// <summary>
    /// Adds a DateTime equality filter.
    /// </summary>
    IVectorQuery WhereEquals(string fieldName, DateTime? value);

    /// <summary>
    /// Adds a DateTime range filter.
    /// </summary>
    IVectorQuery WhereDateRange(string fieldName, DateTime? from, DateTime? to);

    /// <summary>
    /// Adds a numeric range filter.
    /// </summary>
    IVectorQuery WhereRange(string fieldName, double? from, double? to);

    /// <summary>
    /// Adds an integer range filter.
    /// </summary>
    IVectorQuery WhereRange(string fieldName, int? from, int? to);
}
