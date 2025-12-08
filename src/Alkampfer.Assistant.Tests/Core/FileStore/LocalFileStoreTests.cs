using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.FileStore;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.FileStore;

public class LocalFileStoreTests : IDisposable
{
    private readonly string _tempPath;
    private readonly LocalFileStore _sut;

    public LocalFileStoreTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sut = new LocalFileStore(_tempPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateFile()
    {
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        await _sut.SaveFileAsync("test.txt", content);

        Assert.True(File.Exists(Path.Combine(_tempPath, "test.txt")));
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnContent_WhenFileExists()
    {
        var content = new byte[] { 1, 2, 3 };
        var path = Path.Combine(_tempPath, "test.txt");
        Directory.CreateDirectory(_tempPath);
        await File.WriteAllBytesAsync(path, content);

        using var stream = await _sut.GetFileAsync("test.txt");
        
        Assert.NotNull(stream);
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms);
        Assert.Equal(content, ms.ToArray());
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnNull_WhenFileDoesNotExist()
    {
        var stream = await _sut.GetFileAsync("nonexistent.txt");
        Assert.Null(stream);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        var path = Path.Combine(_tempPath, "test.txt");
        Directory.CreateDirectory(_tempPath);
        await File.WriteAllBytesAsync(path, new byte[] { 1 });

        await _sut.DeleteFileAsync("test.txt");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ListFilesAsync_ShouldReturnFiles()
    {
        Directory.CreateDirectory(_tempPath);
        await File.WriteAllBytesAsync(Path.Combine(_tempPath, "a.txt"), new byte[] { 1 });
        await File.WriteAllBytesAsync(Path.Combine(_tempPath, "b.txt"), new byte[] { 1 });
        
        var files = await _sut.ListFilesAsync("");
        
        Assert.Contains("a.txt", files);
        Assert.Contains("b.txt", files);
    }
}
