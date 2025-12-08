using System;
using Alkampfer.Assistant.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alkampfer.Assistant.Core.FileStore;

public class FileStoreConfiguration
{
    public string Type { get; set; } = "Local";
    public string BasePath { get; set; } = "./data/files";
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "files";
}

public static class FileStoreExtensions
{
    public static IServiceCollection AddFileStore(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("FileStore");
        var config = section.Get<FileStoreConfiguration>() ?? new FileStoreConfiguration();
        
        services.AddSingleton(config);

        if (string.Equals(config.Type, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new InvalidOperationException("FileStore:ConnectionString is required for AzureBlob storage type.");
            }
            
            services.AddSingleton<IFileStore>(sp => 
                new AzureBlobFileStore(config.ConnectionString, config.Container));
        }
        else
        {
            services.AddSingleton<IFileStore>(sp => 
                new LocalFileStore(config.BasePath));
        }

        return services;
    }
}
