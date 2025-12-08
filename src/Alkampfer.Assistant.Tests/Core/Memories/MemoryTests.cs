using Alkampfer.Assistant.Interfaces.Memories;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Memories;

public class MemoryTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var memory = new Memory();
        
        Assert.NotNull(memory.Attachments);
        Assert.Empty(memory.Attachments);
        Assert.NotEqual(default, memory.CreatedAt);
        Assert.NotEqual(default, memory.UpdatedAt);
    }
}
