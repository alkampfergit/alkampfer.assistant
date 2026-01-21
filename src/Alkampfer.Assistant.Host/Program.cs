using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.FileStore;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Core.MongoDbIntegration;
using Alkampfer.Assistant.Host.Components;
using Alkampfer.Assistant.Host.Configuration;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using MudBlazor;
using MudBlazor.Services;
using Alkampfer.Assistant.Host; // Add this for ConfigurationHelper

var builder = WebApplication.CreateBuilder(args);

// Add environment variables with ALKASS prefix
builder.Configuration.AddEnvironmentVariables(prefix: "ALKASS_");

// Add override configuration from alkampfer.assistant.json if present
ConfigurationHelper.AddOverrideConfiguration(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
});

builder.Services.AddHttpClient();

// Feature: Bookmarks
builder.Services.AddFileStore(builder.Configuration);
builder.Services.AddBookmarkServices();

// Database Persistence Configuration
var dbConfig = builder.Configuration.GetSection("Database").Get<DatabaseConfiguration>()
    ?? new DatabaseConfiguration();
dbConfig.Validate();

// Register repositories based on database type
if (dbConfig.IsLiteDb)
{
    // LiteDB: Use ConnectionString as file path
    var dbPath = dbConfig.ConnectionString;
    var directory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(directory))
    {
        Directory.CreateDirectory(directory);
    }

    builder.Services.AddSingleton<IRepository<Bookmark, BookmarkId>>(sp =>
        new LiteDbRepository<Bookmark, BookmarkId>(dbPath, "bookmarks"));
    builder.Services.AddSingleton<IRepository<Memory, MemoryId>>(sp =>
        new LiteDbRepository<Memory, MemoryId>(dbPath, "memories"));
}
else if (dbConfig.IsMongoDb)
{
    // MongoDB: Use ConnectionString as MongoDB connection string
    // Extract database name from connection string or use default
    var connectionString = dbConfig.ConnectionString;
    var databaseName = ExtractDatabaseNameFromMongoConnection(connectionString) ?? "alkampfer_assistant";

    builder.Services.AddSingleton<IRepository<Bookmark, BookmarkId>>(sp =>
        new MongoRepository<Bookmark, BookmarkId>(connectionString, databaseName, "bookmarks"));
    builder.Services.AddSingleton<IRepository<Memory, MemoryId>>(sp =>
        new MongoRepository<Memory, MemoryId>(connectionString, databaseName, "memories"));
}
else
{
    throw new InvalidOperationException($"Unsupported database type: {dbConfig.Type}");
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Extracts the database name from a MongoDB connection string.
/// Examples:
/// - mongodb://localhost:27017/mydb -> mydb
/// - mongodb://localhost:27017 -> null
/// </summary>
static string? ExtractDatabaseNameFromMongoConnection(string connectionString)
{
    try
    {
        var uri = new Uri(connectionString);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[0] : null;
    }
    catch
    {
        return null;
    }
}
