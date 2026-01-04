using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Interface for vector query results.
/// </summary>
public interface IVectorQueryResult
{
    /// <summary>
    /// Gets the collection of records returned by the query.
    /// </summary>
    IReadOnlyList<VectorRecord> Records { get; }

    /// <summary>
    /// Gets the total number of matching records.
    /// </summary>
    long TotalCount { get; }

    /// <summary>
    /// Gets whether there are more results available.
    /// </summary>
    bool HasMore { get; }

    /// <summary>
    /// Gets the query execution time in milliseconds.
    /// </summary>
    long ExecutionTimeMs { get; }
}

/// <summary>
/// Represents an immutable query result containing VectorRecords and metadata.
/// </summary>
public class VectorQueryResult : IVectorQueryResult
{
    /// <summary>
    /// Gets the collection of records returned by the query.
    /// </summary>
    public IReadOnlyList<VectorRecord> Records { get; }

    /// <summary>
    /// Gets the total number of matching records (may be greater than Records.Count if limited).
    /// </summary>
    public long TotalCount { get; }

    /// <summary>
    /// Gets whether there are more results available.
    /// </summary>
    public bool HasMore => TotalCount > Records.Count;

    /// <summary>
    /// Gets the query execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; }

    /// <summary>
    /// Initializes a new instance of VectorQueryResult.
    /// </summary>
    public VectorQueryResult(
        IEnumerable<VectorRecord> records,
        long totalCount,
        long executionTimeMs)
    {
        Records = records.ToList();
        TotalCount = totalCount;
        ExecutionTimeMs = executionTimeMs;
    }

    /// <summary>
    /// Creates an empty query result.
    /// </summary>
    public static VectorQueryResult Empty()
    {
        return new VectorQueryResult([], 0, 0);
    }
}
