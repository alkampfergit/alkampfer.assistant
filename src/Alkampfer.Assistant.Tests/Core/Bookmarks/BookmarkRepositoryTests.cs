using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using LiteDB;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Bookmarks;

public class BookmarkRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbRepository<Bookmark, BookmarkId> _repository;

    public BookmarkRepositoryTests()
    {
        _dbPath = Path.GetTempFileName();
        // LiteDbRepository takes connection string, not LiteDatabase instance
        _repository = new LiteDbRepository<Bookmark, BookmarkId>(_dbPath, "bookmarks");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistBookmark()
    {
        var bookmark = new Bookmark
        {
            Id = new BookmarkId(1),
            Title = "Test Bookmark",
            Url = "https://example.com",
            Tags = new List<string> { "tag1", "tag2" },
            Status = BookmarkStatus.Downloaded,
            MemoryId = new MemoryId(10)
        };

        await _repository.SaveAsync(bookmark);

        var saved = await _repository.LoadByIdAsync(new BookmarkId(1));
        Assert.NotNull(saved);
        Assert.Equal("Test Bookmark", saved.Title);
        Assert.Equal("https://example.com", saved.Url);
        Assert.Equal(2, saved.Tags.Count);
        Assert.Equal(BookmarkStatus.Downloaded, saved.Status);
        Assert.Equal(new MemoryId(10), saved.MemoryId);
    }
}
