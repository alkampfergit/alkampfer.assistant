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
    /// Gets the vector search parameters for KNN (K-Nearest Neighbors) search.
    /// When set, vector search replaces text search as the primary ranking mechanism.
    /// </summary>
    VectorSearchParams? VectorSearch { get; }

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
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    IVectorQuery WhereDateRange(string fieldName, DateTime? from, DateTime? to, bool includeFrom = true, bool includeTo = true);

    /// <summary>
    /// Adds a numeric range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    IVectorQuery WhereRange(string fieldName, double? from, double? to, bool includeFrom = true, bool includeTo = true);

    /// <summary>
    /// Adds an integer range filter.
    /// </summary>
    /// <param name="fieldName">The field name to filter on.</param>
    /// <param name="from">The start of the range (null for no lower bound).</param>
    /// <param name="to">The end of the range (null for no upper bound).</param>
    /// <param name="includeFrom">True to include the From boundary (>=), false to exclude (>). Default is true.</param>
    /// <param name="includeTo">True to include the To boundary (<=), false to exclude (<). Default is true.</param>
    IVectorQuery WhereRange(string fieldName, int? from, int? to, bool includeFrom = true, bool includeTo = true);

    /// <summary>
    /// Adds an OR composite filter combining multiple filters.
    /// </summary>
    IVectorQuery WithOrFilter(params IQueryFilter[] filters);

    /// <summary>
    /// Adds an AND composite filter combining multiple filters.
    /// </summary>
    IVectorQuery WithAndFilter(params IQueryFilter[] filters);

    /// <summary>
    /// Adds a NOT filter that negates another filter.
    /// </summary>
    IVectorQuery WithNotFilter(IQueryFilter filter);

    /// <summary>
    /// Adds a filter using the FilterBuilder for complex query construction.
    /// </summary>
    IVectorQuery Where(Func<FilterBuilder, IQueryFilter> buildFilter);

    /// <summary>
    /// Sets the vector search parameters for KNN (K-Nearest Neighbors) search.
    /// When specified, vector search replaces text search as the primary ranking mechanism.
    /// </summary>
    IVectorQuery WithVectorSearch(string vectorKey, float[] queryVector, int topK = 10);

    /// <summary>
    /// Sets the vector search parameters for KNN (K-Nearest Neighbors) search with explicit NumCandidates.
    /// </summary>
    IVectorQuery WithVectorSearch(string vectorKey, float[] queryVector, int topK, int numCandidates);
}
