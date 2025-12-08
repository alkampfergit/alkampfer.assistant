using Alkampfer.Assistant.Core.Bookmarks;
using Alkampfer.Assistant.Core.Memories;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using Microsoft.Extensions.DependencyInjection;

namespace Alkampfer.Assistant.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBookmarkServices(this IServiceCollection services)
    {
        services.AddScoped<IContentExtractionService, ContentExtractionService>();
        services.AddScoped<IMemoryService, MemoryService>();
        services.AddScoped<IBookmarkService, BookmarkService>();
        
        return services;
    }
}
