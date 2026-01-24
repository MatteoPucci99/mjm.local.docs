using Microsoft.Extensions.DependencyInjection;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Core.DependencyInjection;

/// <summary>
/// Extension methods for configuring Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core document services to the service collection.
    /// </summary>
    public static IServiceCollection AddLocalDocsCoreServices(this IServiceCollection services)
    {
        services.AddScoped<DocumentService>();
        return services;
    }
}
