using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Core.FileStore;
using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Host.Components;
using Alkampfer.Assistant.Interfaces;
using Alkampfer.Assistant.Interfaces.Bookmarks;
using Alkampfer.Assistant.Interfaces.Memories;
using MudBlazor;
using MudBlazor.Services;
using Alkampfer.Assistant.Host; // Add this for ConfigurationHelper

var builder = WebApplication.CreateBuilder(args);

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

// Persistence (LiteDB for now)
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "assistant.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddSingleton<IRepository<Bookmark, BookmarkId>>(sp => 
    new LiteDbRepository<Bookmark, BookmarkId>(dbPath, "bookmarks"));
builder.Services.AddSingleton<IRepository<Memory, MemoryId>>(sp => 
    new LiteDbRepository<Memory, MemoryId>(dbPath, "memories"));

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
