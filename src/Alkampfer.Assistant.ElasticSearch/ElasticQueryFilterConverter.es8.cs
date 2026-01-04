using Elastic.Clients.Elasticsearch.QueryDsl;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Converts generic IQueryFilter instances to Elasticsearch Query objects.
/// </summary>
public static class ElasticQueryFilterConverter
{
    /// <summary>
    /// Converts an IQueryFilter to an Elasticsearch Query.
    /// </summary>
    /// <param name="filter">The filter to convert.</param>
    /// <returns>The Elasticsearch Query representation of the filter.</returns>
    /// <exception cref="NotSupportedException">Thrown when the filter type is not supported.</exception>
    public static Query ToElasticQuery(IQueryFilter filter)
    {
        return filter switch
        {
            StringEqualsFilter f => new MatchQuery { Field = $"s_{f.FieldName}", Query = f.Value },
            KeywordEqualsFilter f => new TermQuery { Field = $"k_{f.FieldName}", Value = f.Value.ToLowerInvariant() },
            IntegerEqualsFilter f => new TermQuery { Field = $"i_{f.FieldName}", Value = f.Value },
            DoubleEqualsFilter f => new TermQuery { Field = $"n_{f.FieldName}", Value = f.Value },
            BooleanEqualsFilter f => new TermQuery { Field = $"b_{f.FieldName}", Value = f.Value },
            DateTimeEqualsFilter f => new TermQuery { Field = $"d_{f.FieldName}", Value = f.Value.ToString("O") },
            DateTimeRangeFilter f => CreateDateTimeRangeQuery(f),
            NumericRangeFilter f => CreateNumericRangeQuery(f),
            IntegerRangeFilter f => CreateIntegerRangeQuery(f),
            _ => throw new NotSupportedException($"Filter type {filter.GetType().Name} is not supported by Elasticsearch.")
        };
    }

    private static Query CreateDateTimeRangeQuery(DateTimeRangeFilter filter)
    {
        var rangeQuery = new DateRangeQuery($"d_{filter.FieldName}");
        if (filter.From.HasValue)
            rangeQuery.Gte = filter.From.Value;
        if (filter.To.HasValue)
            rangeQuery.Lte = filter.To.Value;
        return rangeQuery;
    }

    private static Query CreateNumericRangeQuery(NumericRangeFilter filter)
    {
        var rangeQuery = new NumberRangeQuery($"n_{filter.FieldName}");
        if (filter.From.HasValue)
            rangeQuery.Gte = filter.From.Value;
        if (filter.To.HasValue)
            rangeQuery.Lte = filter.To.Value;
        return rangeQuery;
    }

    private static Query CreateIntegerRangeQuery(IntegerRangeFilter filter)
    {
        var rangeQuery = new NumberRangeQuery($"i_{filter.FieldName}");
        if (filter.From.HasValue)
            rangeQuery.Gte = filter.From.Value;
        if (filter.To.HasValue)
            rangeQuery.Lte = filter.To.Value;
        return rangeQuery;
    }
}
