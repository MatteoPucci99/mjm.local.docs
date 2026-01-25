using Microsoft.EntityFrameworkCore;
using Mjm.LocalDocs.Core.DependencyInjection;
using Mjm.LocalDocs.Infrastructure.DependencyInjection;
using Mjm.LocalDocs.Infrastructure.Persistence;
using Mjm.LocalDocs.Server.Components;
using Mjm.LocalDocs.Server.McpTools;
using ModelContextProtocol.AspNetCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add MCP Server with tools
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ProjectTools>()
    .WithTools<AddDocumentTool>()
    .WithTools<DocumentTools>()
    .WithTools<ListProjectsTool>()
    .WithTools<SearchDocsTool>();

// Add LocalDocs services
builder.Services.AddLocalDocsCoreServices();

// Add Infrastructure services - configured from appsettings.json
// See LocalDocs:Embeddings section for provider configuration (Fake, OpenAI)
// See LocalDocs:Storage section for storage configuration (InMemory, Sqlite)
var connectionString = builder.Configuration.GetConnectionString("LocalDocs");
builder.Services.AddLocalDocsInfrastructure(builder.Configuration, connectionString);

var app = builder.Build();

// Ensure SQLite database is created (when using SQLite storage provider)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetService<LocalDocsDbContext>();
    context?.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// Map MCP endpoint
app.MapMcp("/mcp");

// Map Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
