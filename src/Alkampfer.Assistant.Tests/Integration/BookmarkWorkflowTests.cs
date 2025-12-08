using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.Bookmarks;
using Alkampfer.Assistant.Core.FileStore;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Core.Memories;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Alkampfer.Assistant.Tests.Integration;

public class BookmarkWorkflowTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _filesPath;
    private readonly IBookmarkService _bookmarkService;
    private readonly IMemoryService _memoryService;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public BookmarkWorkflowTests()
    {
        _dbPath = Path.GetTempFileName();
        _filesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        var bookmarkRepo = new LiteDbRepository<Bookmark, BookmarkId>(_dbPath, "bookmarks");
        var memoryRepo = new LiteDbRepository<Memory, MemoryId>(_dbPath, "memories");
        var fileStore = new LocalFileStore(_filesPath);
        
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        var extractionService = new ContentExtractionService(httpClient, fileStore, NullLogger<ContentExtractionService>.Instance);
        
        _memoryService = new MemoryService(memoryRepo, fileStore, NullLogger<MemoryService>.Instance);
        _bookmarkService = new BookmarkService(bookmarkRepo, _memoryService, extractionService, NullLogger<BookmarkService>.Instance);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (Directory.Exists(_filesPath)) Directory.Delete(_filesPath, true);
    }

    [Fact]
    public async Task FullWorkflow_CreateDownloadVerify()
    {
        // Arrange
        var html = "<html><body><h1>Test Page</h1><p>Some content</p></body></html>";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(html) });

        // Act
        // 1. Create Bookmark
        var bookmark = await _bookmarkService.CreateBookmarkAsync("https://example.com", "Test", new[] { "tag1" });
        Assert.NotNull(bookmark);
        Assert.Equal(BookmarkStatus.New, bookmark.Status);

        // 2. Download Content
        await _bookmarkService.DownloadContentAsync(bookmark.Id);

        // Assert
        // 3. Verify Status
        var updatedBookmark = await _bookmarkService.GetBookmarkAsync(bookmark.Id);
        Assert.NotNull(updatedBookmark);
        Assert.Equal(BookmarkStatus.Downloaded, updatedBookmark.Status);
        Assert.NotNull(updatedBookmark.MemoryId);

        // 4. Verify Memory
        var memory = await _memoryService.GetMemoryAsync(updatedBookmark.MemoryId);
        Assert.NotNull(memory);
        
        var content = await _memoryService.GetContentAsync(updatedBookmark.MemoryId);
        Assert.Contains("# Test Page", content);
    }
}
