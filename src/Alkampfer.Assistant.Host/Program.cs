using Alkampfer.Assistant.Core.LiteDbIntegration;
using Alkampfer.Assistant.Core.Configuration;
using Alkampfer.Assistant.Core;
using Alkampfer.Assistant.Host;
using Serilog;
using Alkampfer.Assistant.Host.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with serilog
ConfigurationHelper.ConfigureLogging();

// Add Serilog to standard logging
builder.Services.AddLogging(loggingBuilder =>
{
    //clear existing providers and add only serilog.
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog(dispose: true);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Register LiteDbRepository for IRepository<ModelDefinition>
builder.Services.AddSingleton<IRepository<ModelDefinition>>(sp =>
    new LiteDbRepository<ModelDefinition>("Filename=ModelDefinitions.db;Mode=Shared", "model_definitions"));

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
