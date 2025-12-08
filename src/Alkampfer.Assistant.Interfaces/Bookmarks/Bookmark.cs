using System;
using System.Collections.Generic;
using Alkampfer.Assistant.Interfaces.Memories;

namespace Alkampfer.Assistant.Interfaces.Bookmarks;

public class Bookmark : BaseEntity<BookmarkId>
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public BookmarkStatus Status { get; set; } = BookmarkStatus.New;
    public string? LastError { get; set; }
    public MemoryId? MemoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
