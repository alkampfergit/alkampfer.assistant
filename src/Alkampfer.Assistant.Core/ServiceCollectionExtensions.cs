using Alkampfer.Assistant.Core.Bookmarks;
using Alkampfer.Assistant.Core.Memories;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alkampfer.Assistant.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookmarkServices(this IServiceCollection services)
    {
        services.AddScoped<IContentExtractionService, ContentExtractionService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IBookmarkService>(sp => 
            new BookmarkService(
                sp.GetRequiredService<IRepository<Bookmark, BookmarkId>>(),
                sp.GetRequiredService<IMemoryService>(),
                sp.GetRequiredService<IContentExtractionService>(),
                sp.GetRequiredService<IFileStore>(),
                sp.GetRequiredService<ILogger<BookmarkService>>()
            ));
        
        return services;
    }
}
