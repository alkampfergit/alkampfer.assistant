using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Provides vector query operations for searching and retrieving VectorRecords.
/// </summary>
public interface IVectorQueryExecutor
{
    /// <summary>
    /// Executes a VectorQuery against the specified index.
    /// </summary>
    /// <param name="indexName">The name of the index to search.</param>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A VectorQueryResult containing matching records and metadata.</returns>
    Task<VectorQueryResult> ExecuteQueryAsync(
        string indexName,
        IVectorQuery query,
        CancellationToken cancellationToken = default);

}
