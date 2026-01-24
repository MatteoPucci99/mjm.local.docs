# AGENTS.md - mjm.local.docs

Local MCP Server for semantic document search via embeddings. Built with .NET 10, Microsoft Semantic Kernel, and Blazor Server.

## Quick Reference

| Item | Value |
|------|-------|
| SDK | .NET 10.0.102 |
| Solution | `mjm.local.docs/mjm.local.docs.sln` |
| Test Framework | xUnit 2.9.3 |
| MCP Endpoint | `/mcp` |
| Dev URL | `http://localhost:5024` |

## Build Commands

```bash
cd mjm.local.docs

dotnet restore                 # Restore dependencies
dotnet build                   # Build all projects
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj   # Run server
dotnet watch --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj # Hot reload
```

## Test Commands

```bash
cd mjm.local.docs

dotnet test                                                    # Run all tests
dotnet test --filter "FullyQualifiedName~TestMethodName"       # Single test by name
dotnet test --filter "FullyQualifiedName~ClassName"            # All tests in class
dotnet test --logger "console;verbosity=detailed"              # Verbose output
dotnet test --collect:"XPlat Code Coverage"                    # With coverage
```

## Project Structure

```
mjm.local.docs/
├── mjm.local.docs/
│   ├── mjm.local.docs.sln
│   ├── global.json
│   ├── src/
│   │   ├── Mjm.LocalDocs.Core/           # Abstractions, models (no external deps)
│   │   ├── Mjm.LocalDocs.Infrastructure/ # Implementations (embeddings, vector store)
│   │   └── Mjm.LocalDocs.Server/         # MCP Server + Blazor Web App
│   └── tests/
│       └── Mjm.LocalDocs.Tests/          # xUnit tests
```

## Code Style

### Namespaces and Imports

- **File-scoped namespaces** (single line with semicolon)
- `ImplicitUsings` enabled - common System namespaces auto-imported
- Order: System > third-party > project namespaces

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.McpTools;
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Interfaces | `I` prefix | `IDocumentRepository` |
| Async methods | `Async` suffix | `SearchAsync` |
| Private fields | `_camelCase` | `_repository` |
| Parameters | `camelCase` | `queryEmbedding` |
| Properties | `PascalCase` | `DocumentId` |

### Classes and Types

- Use `sealed` on implementation classes
- Use `required` on required init-only properties
- Prefer `init` over `set` for immutable models
- Use primary constructors or traditional constructors for DI

```csharp
public sealed class DocumentChunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### Async/Await

- All async methods must have `Async` suffix
- Always accept `CancellationToken cancellationToken = default` as last parameter
- Never use `.Result` or `.Wait()` - always await

```csharp
public async Task<IReadOnlyList<SearchResult>> SearchAsync(
    ReadOnlyMemory<float> queryEmbedding,
    string? collection = null,
    int limit = 10,
    CancellationToken cancellationToken = default)
```

### XML Documentation

- Document all public types, methods, and properties
- Use `<inheritdoc />` for interface implementations

```csharp
/// <summary>
/// Searches for chunks similar to the query embedding.
/// </summary>
/// <param name="queryEmbedding">The query embedding vector.</param>
/// <returns>Search results ordered by relevance.</returns>
Task<IReadOnlyList<SearchResult>> SearchAsync(...);
```

### Nullable Reference Types

- `Nullable` enabled project-wide
- Use `?` for nullable: `string? collection = null`
- Check with `string.IsNullOrEmpty()`
- Use `!` sparingly and only when certain

### Collections and Error Handling

- Return `IReadOnlyList<T>` for read-only collections
- Use `IEnumerable<T>` for input parameters
- Use collection expressions: `[]` instead of `new List<T>()`
- Return early for edge cases
- Use `Math.Clamp()` for range validation

```csharp
if (chunks.Count == 0)
    return;

limit = Math.Clamp(limit, 1, 20);
```

## Architecture

### Clean Architecture Layers

1. **Core** - Domain models, interfaces (no external dependencies)
2. **Infrastructure** - Implementations of Core interfaces
3. **Server** - Composition root, MCP tools, Web UI

### Dependency Injection

Register via extension methods in `DependencyInjection/` folders:
- `AddLocalDocsCoreServices()` - core services
- `AddLocalDocsFakeInfrastructure()` - development (fake embeddings, no API key)

### MCP Tools

Decorate with `[McpServerToolType]` and `[McpServerTool]`:

```csharp
[McpServerToolType]
public sealed class SearchDocsTool
{
    [McpServerTool(Name = "search_docs")]
    [Description("Search for documents using semantic search.")]
    public async Task<string> SearchDocsAsync(
        [Description("The search query")] string query,
        CancellationToken cancellationToken = default)
    { ... }
}
```

## Key Packages

| Package | Purpose |
|---------|---------|
| Microsoft.SemanticKernel | AI/ML orchestration |
| Microsoft.Extensions.VectorData.Abstractions | Vector data interfaces |
| ModelContextProtocol.AspNetCore | MCP server for ASP.NET Core |
| xunit | Testing framework |
