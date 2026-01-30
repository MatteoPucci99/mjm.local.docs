using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.DependencyInjection;
using LocalDocsAuthOptions = Mjm.LocalDocs.Core.Models.AuthenticationOptions;
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

// Add HttpClient for login API calls
builder.Services.AddHttpClient();

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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

// Bind authentication options from configuration
builder.Services.Configure<LocalDocsAuthOptions>(
    builder.Configuration.GetSection(LocalDocsAuthOptions.SectionName));

// Add Infrastructure services - configured from appsettings.json
// See LocalDocs:Embeddings section for provider configuration (Fake, OpenAI)
// See LocalDocs:Storage section for storage configuration (InMemory, Sqlite)
var connectionString = builder.Configuration.GetConnectionString("LocalDocs");
builder.Services.AddLocalDocsInfrastructure(builder.Configuration, connectionString);

var app = builder.Build();

// Ensure database and vector store are initialized
using (var scope = app.Services.CreateScope())
{
    // Initialize EF Core database (Projects, Documents, DocumentChunks tables)
    // Try to get DbContext directly (works for SQLite)
    var context = scope.ServiceProvider.GetService<LocalDocsDbContext>();
    
    // If not available directly, try via factory (works for SQL Server with pooled factory)
    if (context is null)
    {
        var factory = scope.ServiceProvider.GetService<IDbContextFactory<LocalDocsDbContext>>();
        context = factory?.CreateDbContext();
    }
    
    context?.Database.EnsureCreated();

    // Initialize vector store (chunk_embeddings table for SQL Server, etc.)
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
    await vectorStore.InitializeAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS redirect only for non-MCP requests (MCP tools connect via HTTP)
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/mcp"),
    appBuilder => appBuilder.UseHttpsRedirection());

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Login endpoint (POST) - browser form submission
app.MapPost("/api/login", async (
    HttpContext context,
    IOptions<LocalDocsAuthOptions> authOptions) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "true";
    
    var options = authOptions.Value;
    
    // Validate credentials against configuration
    if (!string.Equals(username, options.Username, StringComparison.OrdinalIgnoreCase) ||
        password != options.Password)
    {
        // Redirect back to login with error
        return Results.Redirect("/login?error=invalid");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, options.Username),
        new(ClaimTypes.NameIdentifier, options.Username)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var authProperties = new AuthenticationProperties
    {
        IsPersistent = rememberMe,
        ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
    };

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

    // Redirect to home page after successful login
    return Results.Redirect("/");
}).AllowAnonymous().DisableAntiforgery();

// Logout endpoint
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

// Map MCP endpoint
app.MapMcp("/mcp");

// Map Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
