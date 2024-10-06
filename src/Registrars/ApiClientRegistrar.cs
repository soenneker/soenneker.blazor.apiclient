using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.Utils.Session.Registrars;
using Soenneker.Utils.HttpClientCache.Registrar;

namespace Soenneker.Blazor.ApiClient.Registrars;

/// <summary>
/// A lightweight and efficient API client wrapper for Blazor applications, simplifying HTTP communication with support for asynchronous calls, cancellation tokens, and JSON serialization.
/// </summary>
public static class ApiClientRegistrar
{
    /// <summary>
    /// Adds <see cref="IApiClient"/> as a scoped service. <para/>
    /// </summary>
    public static void AddApiClientAsScoped(this IServiceCollection services)
    {
        services.AddSessionUtil();
        services.AddHttpClientCache();
        services.TryAddScoped<IApiClient, ApiClient>();
    }
}