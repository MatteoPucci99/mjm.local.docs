using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Infrastructure.Documents;
using Mjm.LocalDocs.Infrastructure.Embeddings;
using Mjm.LocalDocs.Infrastructure.VectorStore;

namespace Mjm.LocalDocs.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring Infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services with in-memory vector store (for dev/test).
    /// </summary>
    public static IServiceCollection AddLocalDocsInMemoryInfrastructure(
        this IServiceCollection services,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
    {
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(
            new SemanticKernelEmbeddingService(embeddingGenerator, embeddingDimension));
        
        return services;
    }

    /// <summary>
    /// Adds infrastructure services with custom implementations.
    /// </summary>
    public static IServiceCollection AddLocalDocsInfrastructure<TRepository>(
        this IServiceCollection services,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        int embeddingDimension = 1536)
        where TRepository : class, IDocumentRepository
    {
        services.AddSingleton<IDocumentRepository, TRepository>();
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(
            new SemanticKernelEmbeddingService(embeddingGenerator, embeddingDimension));
        
        return services;
    }

    /// <summary>
    /// Adds infrastructure services with fake embedding for development/testing.
    /// Uses in-memory vector store and deterministic fake embeddings.
    /// NOT suitable for production use.
    /// </summary>
    public static IServiceCollection AddLocalDocsFakeInfrastructure(
        this IServiceCollection services,
        int embeddingDimension = 1536)
    {
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        services.AddSingleton<IDocumentProcessor>(new SimpleDocumentProcessor());
        services.AddSingleton<IEmbeddingService>(new FakeEmbeddingService(embeddingDimension));
        
        return services;
    }
}
