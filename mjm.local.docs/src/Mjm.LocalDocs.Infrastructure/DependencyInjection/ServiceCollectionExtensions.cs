using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Infrastructure.Documents;
using Mjm.LocalDocs.Infrastructure.Embeddings;
using Mjm.LocalDocs.Infrastructure.Persistence;
using Mjm.LocalDocs.Infrastructure.Persistence.Repositories;
using Mjm.LocalDocs.Infrastructure.VectorStore;

namespace Mjm.LocalDocs.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services with SQLite persistence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQLite connection string (e.g., "Data Source=localdocs.db").</param>
    /// <param name="embeddingGenerator">The embedding generator to use.</param>
    /// <param name="embeddingDimension">Dimension of embedding vectors (default 1536).</param>
    public static IServiceCollection AddLocalDocsSqliteInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
    {
        // DbContext
        services.AddDbContext<LocalDocsDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();
        services.AddScoped<IDocumentRepository, SqliteDocumentRepository>();
        
        // Vector store (singleton with its own connection)
        services.AddSingleton<IVectorStore>(sp =>
            new SqliteVectorStore(connectionString, embeddingDimension));

        // Processing services
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(
            new SemanticKernelEmbeddingService(embeddingGenerator, embeddingDimension));

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with SQLite persistence and fake embeddings.
    /// Useful for development/testing with persistence but without external API calls.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQLite connection string (e.g., "Data Source=localdocs.db").</param>
    /// <param name="embeddingDimension">Dimension of embedding vectors (default 1536).</param>
    public static IServiceCollection AddLocalDocsSqliteFakeInfrastructure(
        this IServiceCollection services,
        string connectionString,
        int embeddingDimension = 1536)
    {
        // DbContext
        services.AddDbContext<LocalDocsDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IProjectRepository, SqliteProjectRepository>();
        services.AddScoped<IDocumentRepository, SqliteDocumentRepository>();
        
        // Vector store (singleton with its own connection)
        services.AddSingleton<IVectorStore>(sp =>
            new SqliteVectorStore(connectionString, embeddingDimension));

        // Processing services
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(new FakeEmbeddingService(embeddingDimension));

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with in-memory storage (for dev/test).
    /// </summary>
    public static IServiceCollection AddLocalDocsInMemoryInfrastructure(
        this IServiceCollection services,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
    {
        // Repositories
        services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();

        // Processing services
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(
            new SemanticKernelEmbeddingService(embeddingGenerator, embeddingDimension));

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with fake embedding for development/testing.
    /// Uses in-memory storage and deterministic fake embeddings.
    /// NOT suitable for production use.
    /// </summary>
    public static IServiceCollection AddLocalDocsFakeInfrastructure(
        this IServiceCollection services,
        int embeddingDimension = 1536)
    {
        // Repositories
        services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();

        // Processing services
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(new FakeEmbeddingService(embeddingDimension));

        return services;
    }

    /// <summary>
    /// Adds infrastructure services with custom implementations.
    /// </summary>
    public static IServiceCollection AddLocalDocsInfrastructure<TProjectRepository, TDocumentRepository, TVectorStore>(
        this IServiceCollection services,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
        where TProjectRepository : class, IProjectRepository
        where TDocumentRepository : class, IDocumentRepository
        where TVectorStore : class, IVectorStore
    {
        // Repositories
        services.AddSingleton<IProjectRepository, TProjectRepository>();
        services.AddSingleton<IDocumentRepository, TDocumentRepository>();
        services.AddSingleton<IVectorStore, TVectorStore>();

        // Processing services
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(
            new SemanticKernelEmbeddingService(embeddingGenerator, embeddingDimension));

        return services;
    }
}
