using System;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Filter for string field equality (analyzed, full-text match).
/// </summary>
public record StringEqualsFilter(string FieldName, string Value) : IQueryFilter;

/// <summary>
/// Filter for keyword field equality (case-insensitive, exact match).
/// </summary>
public record KeywordEqualsFilter(string FieldName, string Value) : IQueryFilter;

/// <summary>
/// Filter for integer field equality.
/// </summary>
public record IntegerEqualsFilter(string FieldName, int Value) : IQueryFilter;

/// <summary>
/// Filter for double field equality.
/// </summary>
public record DoubleEqualsFilter(string FieldName, double Value) : IQueryFilter;

/// <summary>
/// Filter for boolean field equality.
/// </summary>
public record BooleanEqualsFilter(string FieldName, bool Value) : IQueryFilter;

/// <summary>
/// Filter for DateTime field equality.
/// </summary>
public record DateTimeEqualsFilter(string FieldName, DateTime Value) : IQueryFilter;

/// <summary>
/// Filter for DateTime range.
/// </summary>
public record DateTimeRangeFilter(string FieldName, DateTime? From, DateTime? To) : IQueryFilter;

/// <summary>
/// Filter for numeric (double) range.
/// </summary>
public record NumericRangeFilter(string FieldName, double? From, double? To) : IQueryFilter;

/// <summary>
/// Filter for integer range.
/// </summary>
public record IntegerRangeFilter(string FieldName, int? From, int? To) : IQueryFilter;

/// <summary>
/// Composite filter that combines multiple filters with AND logic.
/// All child filters must match.
/// </summary>
public record AndFilter(IReadOnlyList<IQueryFilter> Filters) : IQueryFilter
{
    public string FieldName => string.Empty; // Not applicable for composite filters
}

/// <summary>
/// Composite filter that combines multiple filters with OR logic.
/// At least one child filter must match.
/// </summary>
public record OrFilter(IReadOnlyList<IQueryFilter> Filters) : IQueryFilter
{
    public string FieldName => string.Empty; // Not applicable for composite filters
}

/// <summary>
/// Composite filter that negates another filter (NOT logic).
/// </summary>
public record NotFilter(IQueryFilter Filter) : IQueryFilter
{
    public string FieldName => string.Empty; // Not applicable for composite filters
}
