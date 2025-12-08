using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Interfaces.Memories;
using LiteDB;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Memories;

public class MemoryRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly LiteDbRepository<Memory, MemoryId> _repository;

    public MemoryRepositoryTests()
    {
        _dbPath = Path.GetTempFileName();
        _repository = new LiteDbRepository<Memory, MemoryId>(_dbPath, "memories");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldPersistMemory()
    {
        var memory = new Memory
        {
            Id = new MemoryId(1),
            ContentPath = "path/to/content.md",
            Attachments = new List<Attachment> { new Attachment("img.png", "path/to/img.png") }
        };

        await _repository.SaveAsync(memory);

        var saved = await _repository.LoadByIdAsync(new MemoryId(1));
        Assert.NotNull(saved);
        Assert.Equal("path/to/content.md", saved.ContentPath);
        Assert.Single(saved.Attachments);
        Assert.Equal("img.png", saved.Attachments[0].Name);
    }
}
