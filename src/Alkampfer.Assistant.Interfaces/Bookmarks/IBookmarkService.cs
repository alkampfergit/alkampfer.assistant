using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alkampfer.Assistant.Interfaces.Bookmarks;

/// <summary>
/// Service for managing bookmarks and their content.
/// </summary>
public interface IBookmarkService
{
    /// <summary>
    /// Creates a new bookmark.
    /// </summary>
    /// <param name="url">The URL of the bookmark.</param>
    /// <param name="title">The title of the bookmark.</param>
    /// <param name="tags">A list of tags associated with the bookmark.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created bookmark.</returns>
    Task<Bookmark> CreateBookmarkAsync(string url, string title, IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a bookmark by its ID.
    /// </summary>
    /// <param name="id">The ID of the bookmark.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The bookmark, or null if not found.</returns>
    Task<Bookmark?> GetBookmarkAsync(BookmarkId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists bookmarks, optionally filtered by a tag.
    /// </summary>
    /// <param name="tag">The tag to filter by (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of bookmarks.</returns>
    Task<IEnumerable<Bookmark>> ListBookmarksAsync(string? tag = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing bookmark.
    /// </summary>
    /// <param name="id">The ID of the bookmark to update.</param>
    /// <param name="title">The new title.</param>
    /// <param name="tags">The new list of tags.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated bookmark.</returns>
    Task<Bookmark> UpdateBookmarkAsync(BookmarkId id, string title, IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a bookmark.
    /// </summary>
    /// <param name="id">The ID of the bookmark to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteBookmarkAsync(BookmarkId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers the download and extraction of content for a bookmark.
    /// </summary>
    /// <param name="id">The ID of the bookmark.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DownloadContentAsync(BookmarkId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads manual content for a bookmark (e.g., text content).
    /// </summary>
    /// <param name="id">The ID of the bookmark.</param>
    /// <param name="content">The text content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadContentAsync(BookmarkId id, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads content from a ZIP file for a bookmark.
    /// </summary>
    /// <param name="id">The ID of the bookmark.</param>
    /// <param name="zipStream">The stream of the ZIP file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadContentAsync(BookmarkId id, System.IO.Stream zipStream, CancellationToken cancellationToken = default);
}
