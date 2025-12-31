using System;
using System.Collections.Generic;
using System.Linq;

namespace Alkampfer.Assistant.ElasticSearch;

/// <summary>
/// Represents the result of a bulk indexing operation.
/// </summary>
public record BulkIndexResult
{
    /// <summary>
    /// Gets the total number of records attempted to index.
    /// </summary>
    public int TotalRecords { get; init; }

    /// <summary>
    /// Gets the number of records successfully indexed.
    /// </summary>
    public int SuccessfulRecords { get; init; }

    /// <summary>
    /// Gets the number of records that failed to index.
    /// </summary>
    public int FailedRecords { get; init; }

    /// <summary>
    /// Gets the list of errors for failed records.
    /// </summary>
    public IReadOnlyList<BulkIndexError> Errors { get; init; } = Array.Empty<BulkIndexError>();

    /// <summary>
    /// Gets a value indicating whether all records were successfully indexed.
    /// </summary>
    public bool IsSuccess => FailedRecords == 0;

    /// <summary>
    /// Combines multiple bulk index results into a single result.
    /// </summary>
    /// <param name="results">The collection of results to combine.</param>
    /// <returns>A combined bulk index result.</returns>
    public static BulkIndexResult Combine(IEnumerable<BulkIndexResult> results)
    {
        var resultsList = results.ToList();

        if (resultsList.Count == 0)
        {
            return new BulkIndexResult
            {
                TotalRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0,
                Errors = Array.Empty<BulkIndexError>()
            };
        }

        return new BulkIndexResult
        {
            TotalRecords = resultsList.Sum(r => r.TotalRecords),
            SuccessfulRecords = resultsList.Sum(r => r.SuccessfulRecords),
            FailedRecords = resultsList.Sum(r => r.FailedRecords),
            Errors = resultsList.SelectMany(r => r.Errors).ToList()
        };
    }
}

/// <summary>
/// Represents an error that occurred during bulk indexing of a single record.
/// </summary>
public record BulkIndexError
{
    /// <summary>
    /// Gets the ID of the record that failed to index.
    /// </summary>
    public required string RecordId { get; init; }

    /// <summary>
    /// Gets the error message describing why the indexing failed.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the HTTP status code associated with the error, if available.
    /// </summary>
    public int? StatusCode { get; init; }
}
