using Alkampfer.Assistant.Interfaces.Bookmarks;
using Xunit;

namespace Alkampfer.Assistant.Tests.Core.Bookmarks;

public class BookmarkTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        var bookmark = new Bookmark();
        
        Assert.NotNull(bookmark.Tags);
        Assert.Empty(bookmark.Tags);
        Assert.Equal(BookmarkStatus.New, bookmark.Status);
        Assert.NotEqual(default, bookmark.CreatedAt);
        Assert.NotEqual(default, bookmark.UpdatedAt);
    }
}
