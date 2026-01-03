using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Represents a filter that can be applied to a VectorQuery.
/// </summary>
public interface IQueryFilter
{
    /// <summary>
    /// Converts this filter to an Elasticsearch Query.
    /// </summary>
    Query ToElasticQuery();
}

/// <summary>
/// Filter for string field equality (analyzed, full-text match).
/// </summary>
public class StringEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public string Value { get; }

    public StringEqualsFilter(string fieldName, string value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new MatchQuery { Field = $"s_{FieldName}", Query = Value };
    }
}

/// <summary>
/// Filter for keyword field equality (case-insensitive, exact match).
/// </summary>
public class KeywordEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public string Value { get; }

    public KeywordEqualsFilter(string fieldName, string value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new TermQuery { Field = $"k_{FieldName}", Value = Value.ToLowerInvariant() };
    }
}

/// <summary>
/// Filter for integer field equality.
/// </summary>
public class IntegerEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public int Value { get; }

    public IntegerEqualsFilter(string fieldName, int value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new TermQuery { Field = $"i_{FieldName}", Value = Value };
    }
}

/// <summary>
/// Filter for double field equality.
/// </summary>
public class DoubleEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public double Value { get; }

    public DoubleEqualsFilter(string fieldName, double value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new TermQuery { Field = $"n_{FieldName}", Value = Value };
    }
}

/// <summary>
/// Filter for boolean field equality.
/// </summary>
public class BooleanEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public bool Value { get; }

    public BooleanEqualsFilter(string fieldName, bool value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new TermQuery { Field = $"b_{FieldName}", Value = Value };
    }
}

/// <summary>
/// Filter for DateTime field equality.
/// </summary>
public class DateTimeEqualsFilter : IQueryFilter
{
    public string FieldName { get; }
    public System.DateTime Value { get; }

    public DateTimeEqualsFilter(string fieldName, System.DateTime value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public Query ToElasticQuery()
    {
        return new TermQuery { Field = $"d_{FieldName}", Value = Value.ToString("O") };
    }
}

/// <summary>
/// Filter for DateTime range.
/// </summary>
public class DateTimeRangeFilter : IQueryFilter
{
    public string FieldName { get; }
    public System.DateTime? From { get; }
    public System.DateTime? To { get; }

    public DateTimeRangeFilter(string fieldName, System.DateTime? from, System.DateTime? to)
    {
        FieldName = fieldName;
        From = from;
        To = to;
    }

    public Query ToElasticQuery()
    {
        var rangeQuery = new DateRangeQuery($"d_{FieldName}");
        if (From.HasValue)
            rangeQuery.Gte = From.Value;
        if (To.HasValue)
            rangeQuery.Lte = To.Value;
        return rangeQuery;
    }
}

/// <summary>
/// Filter for numeric (double) range.
/// </summary>
public class NumericRangeFilter : IQueryFilter
{
    public string FieldName { get; }
    public double? From { get; }
    public double? To { get; }

    public NumericRangeFilter(string fieldName, double? from, double? to)
    {
        FieldName = fieldName;
        From = from;
        To = to;
    }

    public Query ToElasticQuery()
    {
        var rangeQuery = new NumberRangeQuery($"n_{FieldName}");
        if (From.HasValue)
            rangeQuery.Gte = From.Value;
        if (To.HasValue)
            rangeQuery.Lte = To.Value;
        return rangeQuery;
    }
}

/// <summary>
/// Filter for integer range.
/// </summary>
public class IntegerRangeFilter : IQueryFilter
{
    public string FieldName { get; }
    public int? From { get; }
    public int? To { get; }

    public IntegerRangeFilter(string fieldName, int? from, int? to)
    {
        FieldName = fieldName;
        From = from;
        To = to;
    }

    public Query ToElasticQuery()
    {
        var rangeQuery = new NumberRangeQuery($"i_{FieldName}");
        if (From.HasValue)
            rangeQuery.Gte = From.Value;
        if (To.HasValue)
            rangeQuery.Lte = To.Value;
        return rangeQuery;
    }
}
