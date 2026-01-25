using Mjm.LocalDocs.Core.DependencyInjection;
using Mjm.LocalDocs.Infrastructure.DependencyInjection;
using Mjm.LocalDocs.Server.Components;
using Mjm.LocalDocs.Server.McpTools;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

// Add Infrastructure services
// For development: use fake embeddings (no external API needed)
// For production: configure a real embedding provider
if (builder.Environment.IsDevelopment())
{
    // Fake embeddings for local development
    builder.Services.AddLocalDocsFakeInfrastructure();
}
else
{
    // TODO: Configure real embedding provider for production
    // Example with OpenAI:
    // var embeddingGenerator = new OpenAIClient(apiKey).AsEmbeddingGenerator("text-embedding-3-small");
    // builder.Services.AddLocalDocsInMemoryInfrastructure(embeddingGenerator);

    // For now, fallback to fake in non-dev environments too
    builder.Services.AddLocalDocsFakeInfrastructure();
}

var app = builder.Build();

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
