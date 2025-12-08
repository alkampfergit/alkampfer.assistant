using System;

namespace Alkampfer.Assistant.Interfaces.Bookmarks;

public class BookmarkId : Identity
{
    public BookmarkId(string value) : base(value)
    {
    }

    public BookmarkId(long numericId) : base(numericId)
    {
    }
}
