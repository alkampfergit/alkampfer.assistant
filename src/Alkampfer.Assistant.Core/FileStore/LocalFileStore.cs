using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Alkampfer.Assistant.Interfaces;

namespace Alkampfer.Assistant.Core.FileStore;

public class LocalFileStore : IFileStore
{
    private readonly string _basePath;

    public LocalFileStore(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

        _basePath = basePath;
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await content.CopyToAsync(fileStream, cancellationToken);
    }

    public Task<Stream?> GetFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true));
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> ListFilesAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var searchPattern = string.IsNullOrEmpty(prefix) ? "*" : $"{prefix}*";
        // Note: This is a simple implementation. For deep hierarchies, we might need recursive search.
        // Assuming prefix is a relative path prefix.
        
        // If prefix contains directory separators, we need to adjust search
        var searchDir = _basePath;
        var pattern = "*";

        if (!string.IsNullOrEmpty(prefix))
        {
             // If prefix ends with slash, treat as directory
             if (prefix.EndsWith("/") || prefix.EndsWith("\\"))
             {
                 searchDir = Path.Combine(_basePath, prefix);
             }
             else
             {
                 var dir = Path.GetDirectoryName(prefix);
                 if (!string.IsNullOrEmpty(dir))
                 {
                     searchDir = Path.Combine(_basePath, dir);
                     pattern = Path.GetFileName(prefix) + "*";
                 }
                 else
                 {
                     pattern = prefix + "*";
                 }
             }
        }

        if (!Directory.Exists(searchDir))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = Directory.GetFiles(searchDir, pattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(_basePath, f).Replace('\\', '/')); // Normalize to forward slashes

        return Task.FromResult(files);
    }

    public Task<string?> GetPublicUrlAsync(string path, CancellationToken cancellationToken = default)
    {
        // Local file store doesn't support public URLs by default
        return Task.FromResult<string?>(null);
    }

    private string GetFullPath(string path)
    {
        // Prevent directory traversal
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, path));
        if (!fullPath.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access to path outside of base directory is denied.");
        }
        return fullPath;
    }
}
