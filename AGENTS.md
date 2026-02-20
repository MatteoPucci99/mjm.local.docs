# AGENTS.md - mjm.local.docs

Local MCP Server for semantic document search via embeddings. Built with .NET 10, Microsoft Semantic Kernel, and Blazor Server.

## Quick Reference

| Item | Value |
|------|-------|
| SDK | .NET 10.0.102 (pinned via `global.json`, `rollForward: latestMinor`) |
| Solution | `mjm.local.docs/mjm.local.docs.sln` |
| Test Framework | xUnit 2.9.3 + NSubstitute 5.3.0 |
| MCP Endpoint | `http://localhost:5024/mcp` |

## Build & Run Commands

All commands run from the `mjm.local.docs/` directory.

```bash
dotnet restore
dotnet build
dotnet run --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
dotnet watch --project src/Mjm.LocalDocs.Server/Mjm.LocalDocs.Server.csproj
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run a single test by method name (fastest for iteration)
dotnet test --filter "FullyQualifiedName~SearchAsync_WithNoVectorResults_ReturnsEmptyList"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~DocumentServiceTests"

# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Code coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
mjm.local.docs/
├── src/
│   ├── Mjm.LocalDocs.Core/           # Domain models, interfaces — no external deps
│   │   ├── Abstractions/             # IDocumentRepository, IVectorStore, IEmbeddingService, etc.
│   │   ├── Configuration/            # LocalDocsOptions and nested config classes/enums
│   │   ├── Models/                   # Document, DocumentChunk, Project, SearchResult
│   │   └── Services/                 # DocumentService, ApiTokenService
│   ├── Mjm.LocalDocs.Infrastructure/ # Implementations of Core interfaces
│   │   ├── Documents/                # PDF, Word, Markdown, PlainText readers
│   │   ├── Embeddings/               # FakeEmbeddingService, SemanticKernelEmbeddingService
│   │   ├── FileStorage/              # Database, FileSystem, AzureBlob providers
│   │   └── Persistence/              # EF Core DbContext, SQLite/SQL Server repos, vector stores
│   └── Mjm.LocalDocs.Server/         # Composition root: ASP.NET Core, Blazor, MCP tools
│       └── McpTools/                 # One class per MCP tool group
└── tests/
    └── Mjm.LocalDocs.Tests/          # xUnit unit tests (mirrors src structure)
```

## Architecture Constraints

- **Core** must have zero external package dependencies — only `Microsoft.Extensions.*` abstractions.
- **Infrastructure** implements Core interfaces; never reference Server.
- **Tests** reference Core and Infrastructure only (never Server).
- **Clean Architecture**: domain rules live in Core.Services, not in Infrastructure or MCP tools.
- Chunk IDs use the format `{documentId}_chunk_{index}` — required for prefix-based deletion.
- Vector embeddings are stored separately from chunk metadata (`IVectorStore` vs `IDocumentRepository`).

## Dependency Injection

Register via extension methods in each project's `DependencyInjection/` folder:

```csharp
AddLocalDocsCoreServices()          // Core services (Scoped)
AddLocalDocsInfrastructure()        // Reads appsettings.json; dispatches to storage/embedding config
AddLocalDocsFakeInfrastructure()    // Dev/test: fake embeddings + in-memory storage
AddLocalDocsSqliteInfrastructure()  // Sqlite with brute-force vector search
```

Lifetimes: `Scoped` for services and EF repositories; `Singleton` for vector store, embedding service, document readers, and file storage. Use `AddPooledDbContextFactory<>` when both a scoped `DbContext` and singleton `IDbContextFactory` are needed.

## Code Style

### Namespaces and Imports

File-scoped namespaces throughout. `ImplicitUsings` is enabled (System.* are implicit). Import order: `System.*` → third-party (alphabetical) → `Mjm.LocalDocs.*`.

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
| Private fields | `_camelCase` | `_repository`, `_sut` |
| Parameters / locals | `camelCase` | `queryEmbedding` |
| Properties / types | `PascalCase` | `DocumentId`, `DocumentChunk` |
| Config section constant | `const string SectionName` | `"LocalDocs"` |
| Test SUT variable | `_sut` | |
| Test helper methods | `CreateTest{Entity}()` | `CreateTestDocument()` |
| Test methods | `Method_Scenario_Expected` | `SearchAsync_WithNoResults_ReturnsEmptyList` |

### Classes and Types

- Mark all implementation classes `sealed`.
- Use `required` + `init` for immutable domain model properties; use `get; set;` only on EF entities.
- Prefer traditional constructors for DI; primary constructors are acceptable for simple cases.
- EF entities live in `Persistence/Entities/` and are mapped to domain models via private `MapToModel()` methods — no AutoMapper.

```csharp
public sealed class DocumentChunk
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public string? Title { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
```

### Async / Await

- Every async method has the `Async` suffix.
- Last parameter is always `CancellationToken cancellationToken = default`.
- Never use `.Result` or `.Wait()` — always `await`.

### Error Handling

- Throw `ArgumentException` (with `nameof(param)`) for invalid inputs.
- Throw `InvalidOperationException` for domain rule violations.
- Return `null` or `false` for "not found" — do not throw.
- Return early with `[]` for empty collections: `if (chunks.Count == 0) return [];`
- In MCP tool methods, catch exceptions and return `"Error: {ex.Message}"` as a string — do not let exceptions propagate.

### Collections and Nullability

- Return type: `IReadOnlyList<T>` from service/repository methods.
- Input type: `IEnumerable<T>` for collection parameters.
- Empty collections: `[]` (collection expression), not `new List<T>()`.
- `Nullable` is enabled everywhere. Use `?`, guard with `string.IsNullOrEmpty()`, pattern-match with `if (x is null)`.
- Use `!` (null-forgiving) only when genuinely certain — never to silence warnings.
- EF read-only queries must use `.AsNoTracking()`.

### XML Documentation

- Document every public type, method, and property.
- Use `/// <inheritdoc />` (self-closing) on interface implementations.
- Summaries use imperative form: "Gets a document", not "This method gets a document".
- Nullable returns: explicitly state "or `null` if not found."

## MCP Tool Pattern

```csharp
[McpServerToolType]
public sealed class SearchDocsTool
{
    private readonly DocumentService _documentService;

    public SearchDocsTool(DocumentService documentService) =>
        _documentService = documentService;

    [McpServerTool(Name = "search_docs")]
    [Description("Search for documents using semantic search.")]
    public async Task<string> SearchDocsAsync(
        [Description("The search query")] string query,
        [Description("Optional project ID to filter results")] string? projectId = null,
        CancellationToken cancellationToken = default)
    {
        try { /* ... */ }
        catch (Exception ex) { return $"Error searching documents: {ex.Message}"; }
    }
}
```

## Test Patterns

```csharp
/// <summary>Unit tests for <see cref="DocumentService"/>.</summary>
public sealed class DocumentServiceTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly DocumentService _sut;

    public DocumentServiceTests() =>
        _sut = new DocumentService(_repository, _vectorStore, ...);

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNoVectorResults_ReturnsEmptyList()
    {
        // Arrange
        _vectorStore.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var results = await _sut.SearchAsync("query");

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Helper Methods

    private static Document CreateTestDocument(string id = "doc-1") => new()
    {
        Id = id,
        ProjectId = "project-1",
        FileName = "test.txt",
        CreatedAt = DateTimeOffset.UtcNow
    };

    #endregion
}
```

- Group tests with `#region {MethodName} Tests` blocks.
- Static `#region Helper Methods` block for `CreateTest*()` factories.
- Every test uses `// Arrange`, `// Act`, `// Assert` comments.
- Use `Arg.Any<CancellationToken>()` in all substitute setups/verifications.
- `IDisposable.Dispose()` for cleanup (e.g., deleting temp directories).
