using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces;

/// <summary>
/// Abstraction for file storage operations.
/// </summary>
public interface IFileStore
{
    /// <summary>
    /// Saves a file to the store.
    /// </summary>
    /// <param name="path">The relative path of the file.</param>
    /// <param name="content">The content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a file from the store.
    /// </summary>
    /// <param name="path">The relative path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content stream, or null if not found.</returns>
    Task<Stream?> GetFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the store.
    /// </summary>
    /// <param name="path">The relative path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in the store matching a prefix.
    /// </summary>
    /// <param name="prefix">The prefix to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of file paths.</returns>
    Task<IEnumerable<string>> ListFilesAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public URL for the file, if supported.
    /// </summary>
    /// <param name="path">The relative path of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public URL, or null if not supported or not found.</returns>
    Task<string?> GetPublicUrlAsync(string path, CancellationToken cancellationToken = default);
}
