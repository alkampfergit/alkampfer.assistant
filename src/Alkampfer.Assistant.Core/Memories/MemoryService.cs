using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.Logging;

namespace Alkampfer.Assistant.Core.Memories;

public class MemoryService : IMemoryService
{
    private readonly IRepository<Memory, MemoryId> _repository;
    private readonly IFileStore _fileStore;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(IRepository<Memory, MemoryId> repository, IFileStore fileStore, ILogger<MemoryService> logger)
    {
        _repository = repository;
        _fileStore = fileStore;
        _logger = logger;
    }

    public async Task<Memory> CreateMemoryAsync(string content, List<Attachment> attachments, CancellationToken cancellationToken = default)
    {
        var id = new MemoryId(DateTime.UtcNow.Ticks);
        var contentPath = $"memories/{id.NumericId}.md";
        
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _fileStore.SaveFileAsync(contentPath, stream, cancellationToken);
        
        var memory = new Memory
        {
            Id = id,
            ContentPath = contentPath,
            Attachments = attachments,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await _repository.SaveAsync(memory, cancellationToken);
        return memory;
    }

    public async Task<Memory?> GetMemoryAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        return await _repository.LoadByIdAsync(id, cancellationToken);
    }

    public async Task<string?> GetContentAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        var memory = await _repository.LoadByIdAsync(id, cancellationToken);
        if (memory == null) return null;
        
        var stream = await _fileStore.GetFileAsync(memory.ContentPath, cancellationToken);
        if (stream == null) return null;
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task DeleteMemoryAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        var memory = await _repository.LoadByIdAsync(id, cancellationToken);
        if (memory != null)
        {
            await _fileStore.DeleteFileAsync(memory.ContentPath, cancellationToken);
            foreach (var attachment in memory.Attachments)
            {
                await _fileStore.DeleteFileAsync(attachment.Path, cancellationToken);
            }
            await _repository.DeleteAsync(id, cancellationToken);
        }
    }
}
