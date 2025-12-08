using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.Logging;

namespace Alkampfer.Assistant.Core.Bookmarks;

public class BookmarkService : IBookmarkService
{
    private readonly IRepository<Bookmark, BookmarkId> _repository;
    private readonly IMemoryService _memoryService;
    private readonly IContentExtractionService _extractionService;
    private readonly ILogger<BookmarkService> _logger;

    public BookmarkService(IRepository<Bookmark, BookmarkId> repository, IMemoryService memoryService, IContentExtractionService extractionService, ILogger<BookmarkService> logger)
    {
        _repository = repository;
        _memoryService = memoryService;
        _extractionService = extractionService;
        _logger = logger;
    }

    public async Task<Bookmark> CreateBookmarkAsync(string url, string title, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating bookmark for {Url} with title '{Title}'", url, title);
        var id = new BookmarkId(DateTime.UtcNow.Ticks);
        
        var bookmark = new Bookmark
        {
            Id = id,
            Url = url,
            Title = title,
            Tags = tags.ToList(),
            Status = BookmarkStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.SaveAsync(bookmark, cancellationToken);
        _logger.LogInformation("Bookmark created with ID {Id}", id);
        return bookmark;
    }

    public async Task<Bookmark?> GetBookmarkAsync(BookmarkId id, CancellationToken cancellationToken = default)
    {
        return await _repository.LoadByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Bookmark>> ListBookmarksAsync(string? tag = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing bookmarks with tag filter: {Tag}", tag ?? "None");
        var query = _repository.AsQueryable;
        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(b => b.Tags.Contains(tag));
        }
        return await Task.Run(() => query.ToList(), cancellationToken);
    }

    public async Task<Bookmark> UpdateBookmarkAsync(BookmarkId id, string title, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating bookmark {Id}", id);
        var bookmark = await _repository.LoadByIdAsync(id, cancellationToken);
        if (bookmark == null) 
        {
            _logger.LogWarning("Bookmark {Id} not found for update", id);
            throw new KeyNotFoundException($"Bookmark {id} not found");
        }

        bookmark.Title = title;
        bookmark.Tags = tags.ToList();
        bookmark.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveAsync(bookmark, cancellationToken);
        _logger.LogInformation("Bookmark {Id} updated successfully", id);
        return bookmark;
    }

    public async Task DeleteBookmarkAsync(BookmarkId id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting bookmark {Id}", id);
        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Bookmark {Id} deleted", id);
    }

    public async Task DownloadContentAsync(BookmarkId id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting content download for bookmark {Id}", id);
        var bookmark = await _repository.LoadByIdAsync(id, cancellationToken);
        if (bookmark == null) 
        {
            _logger.LogWarning("Bookmark {Id} not found for download", id);
            throw new KeyNotFoundException($"Bookmark {id} not found");
        }

        try
        {
            var result = await _extractionService.ExtractAsync(bookmark.Url, cancellationToken);
            
            var memory = await _memoryService.CreateMemoryAsync(result.Content, result.Attachments, cancellationToken);
            
            bookmark.MemoryId = memory.Id;
            bookmark.Status = BookmarkStatus.Downloaded;
            bookmark.LastError = null;
            bookmark.UpdatedAt = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(bookmark.Title) || bookmark.Title == bookmark.Url)
            {
                bookmark.Title = result.Title;
            }
            
            await _repository.SaveAsync(bookmark, cancellationToken);
            _logger.LogInformation("Content downloaded and memory created for bookmark {Id}. Memory ID: {MemoryId}", id, memory.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download content for bookmark {Id}", id);
            bookmark.Status = BookmarkStatus.Error;
            bookmark.LastError = ex.Message;
            bookmark.UpdatedAt = DateTime.UtcNow;
            await _repository.SaveAsync(bookmark, cancellationToken);
            throw;
        }
    }

    public async Task UploadContentAsync(BookmarkId id, string content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading manual content for bookmark {Id}", id);
        var bookmark = await _repository.LoadByIdAsync(id, cancellationToken);
        if (bookmark == null) 
        {
            _logger.LogWarning("Bookmark {Id} not found for manual upload", id);
            throw new KeyNotFoundException($"Bookmark {id} not found");
        }

        var memory = await _memoryService.CreateMemoryAsync(content, new List<Attachment>(), cancellationToken);
        
        bookmark.MemoryId = memory.Id;
        bookmark.Status = BookmarkStatus.Downloaded;
        bookmark.UpdatedAt = DateTime.UtcNow;
        
        await _repository.SaveAsync(bookmark, cancellationToken);
        _logger.LogInformation("Manual content uploaded for bookmark {Id}. Memory ID: {MemoryId}", id, memory.Id);
    }

    public Task UploadContentAsync(BookmarkId id, Stream zipStream, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Zip upload not implemented yet");
    }
}
