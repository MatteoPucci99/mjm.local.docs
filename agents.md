# AGENTS.md - mjm.local.docs

Local MCP Server for semantic document search via embeddings. Built with .NET 10, Microsoft Semantic Kernel, and Blazor Server.

## Quick Reference

| Item | Value |
|------|-------|
| SDK | .NET 10.0.102 |
| Solution | `mjm.local.docs/mjm.local.docs.sln` |
| Test Framework | xUnit 2.9.3 + NSubstitute 5.3.0 |
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

# Run all tests
dotnet test

# Run single test by method name
dotnet test --filter "FullyQualifiedName~SearchAsync_WithNoVectorResults_ReturnsEmptyList"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~DocumentServiceTests"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
mjm.local.docs/
├── mjm.local.docs.sln
├── global.json                       # SDK version pinned to 10.0.102
├── src/
│   ├── Mjm.LocalDocs.Core/           # Abstractions, models (no external deps)
│   │   ├── Abstractions/             # IDocumentRepository, IVectorStore, etc.
│   │   ├── Models/                   # Document, DocumentChunk, SearchResult
│   │   └── Services/                 # DocumentService
│   ├── Mjm.LocalDocs.Infrastructure/ # Implementations (embeddings, vector store)
│   │   ├── Documents/                # PDF, Word, PlainText readers
│   │   ├── Embeddings/               # Fake and SemanticKernel implementations
│   │   └── Persistence/              # SQLite repositories
│   └── Mjm.LocalDocs.Server/         # MCP Server + Blazor Web App
│       └── McpTools/                 # MCP tool classes
└── tests/
    └── Mjm.LocalDocs.Tests/          # xUnit tests
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
| Test methods | `MethodName_Scenario_ExpectedResult` | `SearchAsync_WithNoResults_ReturnsEmptyList` |

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

### XML Documentation

- Document all public types, methods, and properties
- Use `<inheritdoc />` for interface implementations

### Nullable Reference Types

- `Nullable` enabled project-wide
- Use `?` for nullable: `string? collection = null`
- Check with `string.IsNullOrEmpty()`
- Use `!` sparingly and only when certain

### Collections and Error Handling

- Return `IReadOnlyList<T>` for read-only collections
- Use `IEnumerable<T>` for input parameters
- Use collection expressions: `[]` instead of `new List<T>()`
- Return early for edge cases: `if (chunks.Count == 0) return [];`

### Test Patterns

- Use `#region` blocks to group related tests by method
- Create helper methods: `CreateTestDocument()`, `CreateTestChunk()`
- Use Arrange-Act-Assert pattern with comments
- Name SUT variable `_sut` (System Under Test)
- Mock dependencies with `Substitute.For<T>()`

```csharp
[Fact]
public async Task SearchAsync_WithNoResults_ReturnsEmptyList()
{
    // Arrange
    _vectorStore.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(new List<VectorSearchResult>());

    // Act
    var results = await _sut.SearchAsync("query");

    // Assert
    Assert.Empty(results);
}
```

## Architecture

### Clean Architecture Layers

1. **Core** - Domain models, interfaces (no external dependencies)
2. **Infrastructure** - Implementations of Core interfaces
3. **Server** - Composition root, MCP tools, Web UI

### Dependency Injection

Register via extension methods in `DependencyInjection/` folders:
- `AddLocalDocsCoreServices()` - core services
- `AddLocalDocsInfrastructure()` - configurable via appsettings.json
- `AddLocalDocsFakeInfrastructure()` - development (fake embeddings, in-memory storage)

### MCP Tools

Decorate tool classes with `[McpServerToolType]` and methods with `[McpServerTool]`:

```csharp
[McpServerToolType]
public sealed class SearchDocsTool
{
    private readonly DocumentService _documentService;

    public SearchDocsTool(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [McpServerTool(Name = "search_docs")]
    [Description("Search for documents using semantic search.")]
    public async Task<string> SearchDocsAsync(
        [Description("The search query")] string query,
        [Description("Optional project filter")] string? projectId = null,
        CancellationToken cancellationToken = default) { ... }
}
```

## Key Packages

| Package | Purpose |
|---------|---------|
| Microsoft.SemanticKernel | AI/ML orchestration |
| Microsoft.Extensions.VectorData.Abstractions | Vector data interfaces |
| ModelContextProtocol.AspNetCore | MCP server for ASP.NET Core |
| Microsoft.EntityFrameworkCore.Sqlite | SQLite persistence |
| xunit + NSubstitute | Testing and mocking |
