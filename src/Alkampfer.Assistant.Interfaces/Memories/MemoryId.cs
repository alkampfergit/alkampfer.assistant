using System;

namespace Alkampfer.Assistant.Interfaces.Memories;

public class MemoryId : Identity
{
    public MemoryId(string value) : base(value)
    {
    }

    public MemoryId(long numericId) : base(numericId)
    {
    }
}
