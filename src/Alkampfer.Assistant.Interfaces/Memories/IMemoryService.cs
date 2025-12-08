using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Memories;

/// <summary>
/// Service for managing memories (stored content).
/// </summary>
public interface IMemoryService
{
    /// <summary>
    /// Creates a new memory with content and attachments.
    /// </summary>
    /// <param name="content">The text content of the memory.</param>
    /// <param name="attachments">A list of attachments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created memory.</returns>
    Task<Memory> CreateMemoryAsync(string content, List<Attachment> attachments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a memory by its ID.
    /// </summary>
    /// <param name="id">The ID of the memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The memory, or null if not found.</returns>
    Task<Memory?> GetMemoryAsync(MemoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full text content of a memory.
    /// </summary>
    /// <param name="id">The ID of the memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content string, or null if not found.</returns>
    Task<string?> GetContentAsync(MemoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a memory and its associated content file.
    /// </summary>
    /// <param name="id">The ID of the memory to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteMemoryAsync(MemoryId id, CancellationToken cancellationToken = default);
}
