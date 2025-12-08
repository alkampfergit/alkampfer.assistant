using System;
using System.Collections.Generic;

namespace Alkampfer.Assistant.Interfaces.Memories;

public class Memory : BaseEntity<MemoryId>
{
    public string ContentPath { get; set; } = string.Empty;
    public List<Attachment> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
