using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Bookmarks;

public class BookmarkServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _filesPath;
    private readonly IBookmarkService _bookmarkService;
    private readonly IMemoryService _memoryService;
    private readonly IFileStore _fileStore;
    private readonly IRepository<Bookmark, BookmarkId> _bookmarkRepo;

    public BookmarkServiceTests()
    {
        _dbPath = Path.GetTempFileName();
        _filesPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        _bookmarkRepo = new LiteDbRepository<Bookmark, BookmarkId>(_dbPath, "bookmarks");
        var memoryRepo = new LiteDbRepository<Memory, MemoryId>(_dbPath, "memories");
        _fileStore = new LocalFileStore(_filesPath);
        
        var extractionServiceMock = new Mock<IContentExtractionService>();
        
        _memoryService = new MemoryService(memoryRepo, _fileStore, NullLogger<MemoryService>.Instance);
        _bookmarkService = new BookmarkService(_bookmarkRepo, _memoryService, extractionServiceMock.Object, _fileStore, NullLogger<BookmarkService>.Instance);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (Directory.Exists(_filesPath)) Directory.Delete(_filesPath, true);
    }

    [Fact]
    public async Task UploadContentAsync_WithZipFile_ShouldExtractMarkdownAndImages()
    {
        // Arrange
        var bookmark = await _bookmarkService.CreateBookmarkAsync("https://example.com", "Test", new[] { "tag1" });
        
        // Create a zip file in memory with markdown + images
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            // Add markdown file
            var markdownEntry = archive.CreateEntry("content.md");
            using (var markdownStream = markdownEntry.Open())
            {
                var markdownContent = Encoding.UTF8.GetBytes("# Test Content\n\nThis is a test.\n\n![Image](image1.png)");
                await markdownStream.WriteAsync(markdownContent, 0, markdownContent.Length);
            }
            
            // Add image files
            var imageEntry1 = archive.CreateEntry("image1.png");
            using (var imageStream1 = imageEntry1.Open())
            {
                var imageData1 = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
                await imageStream1.WriteAsync(imageData1, 0, imageData1.Length);
            }
            
            var imageEntry2 = archive.CreateEntry("images/image2.jpg");
            using (var imageStream2 = imageEntry2.Open())
            {
                var imageData2 = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
                await imageStream2.WriteAsync(imageData2, 0, imageData2.Length);
            }
        }
        
        zipStream.Position = 0;
        
        // Act
        await _bookmarkService.UploadContentAsync(bookmark.Id, zipStream);
        
        // Assert
        var updatedBookmark = await _bookmarkService.GetBookmarkAsync(bookmark.Id);
        Assert.NotNull(updatedBookmark);
        Assert.Equal(BookmarkStatus.Downloaded, updatedBookmark.Status);
        Assert.NotNull(updatedBookmark.MemoryId);
        
        var memory = await _memoryService.GetMemoryAsync(updatedBookmark.MemoryId);
        Assert.NotNull(memory);
        Assert.NotEmpty(memory.Attachments);
        Assert.Equal(2, memory.Attachments.Count); // 2 images
        
        var content = await _memoryService.GetContentAsync(updatedBookmark.MemoryId);
        Assert.Contains("# Test Content", content);
        Assert.Contains("This is a test", content);
    }

    [Fact]
    public async Task UploadContentAsync_WithZipFileNoMarkdown_ShouldThrow()
    {
        // Arrange
        var bookmark = await _bookmarkService.CreateBookmarkAsync("https://example.com", "Test", new[] { "tag1" });
        
        // Create a zip file with only images, no markdown
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var imageEntry = archive.CreateEntry("image.png");
            using var imageStream = imageEntry.Open();
            var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            await imageStream.WriteAsync(imageData, 0, imageData.Length);
        }
        
        zipStream.Position = 0;
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _bookmarkService.UploadContentAsync(bookmark.Id, zipStream);
        });
    }

    [Fact]
    public async Task UploadContentAsync_WithCorruptZip_ShouldThrow()
    {
        // Arrange
        var bookmark = await _bookmarkService.CreateBookmarkAsync("https://example.com", "Test", new[] { "tag1" });
        
        // Create corrupted zip data
        using var corruptStream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02, 0x03 });
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await _bookmarkService.UploadContentAsync(bookmark.Id, corruptStream);
        });
    }

    [Fact]
    public async Task UploadContentAsync_WithMultipleMarkdownFiles_ShouldUseFirst()
    {
        // Arrange
        var bookmark = await _bookmarkService.CreateBookmarkAsync("https://example.com", "Test", new[] { "tag1" });
        
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
        {
            var md1 = archive.CreateEntry("first.md");
            using (var md1Stream = md1.Open())
            {
                var md1Content = Encoding.UTF8.GetBytes("# First Document");
                await md1Stream.WriteAsync(md1Content, 0, md1Content.Length);
            }
            
            var md2 = archive.CreateEntry("second.md");
            using (var md2Stream = md2.Open())
            {
                var md2Content = Encoding.UTF8.GetBytes("# Second Document");
                await md2Stream.WriteAsync(md2Content, 0, md2Content.Length);
            }
        }
        
        zipStream.Position = 0;
        
        // Act
        await _bookmarkService.UploadContentAsync(bookmark.Id, zipStream);
        
        // Assert
        var updatedBookmark = await _bookmarkService.GetBookmarkAsync(bookmark.Id);
        Assert.NotNull(updatedBookmark?.MemoryId);
        
        var content = await _memoryService.GetContentAsync(updatedBookmark.MemoryId);
        Assert.Contains("# First Document", content);
    }
}
