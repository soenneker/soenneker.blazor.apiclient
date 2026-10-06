using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Soenneker.Blazor.Utils.Session.Abstract;
using Soenneker.Blazor.LogJson.Abstract;
using Soenneker.Utils.HttpClientCache.Abstract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Blazor.ApiClient.Abstract;
using Soenneker.Blazor.LogJson.Registrars;
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
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    [RequiresUnreferencedCode("The default client serializer uses reflection. Pass a generated JSON context.")]
    [RequiresDynamicCode("The default client serializer may require runtime code generation. Pass a generated JSON context.")]
    public static IServiceCollection AddApiClientAsScoped(this IServiceCollection services)
    {
        services.AddLogJsonInteropAsScoped().AddSessionUtilAsScoped().AddHttpClientCacheAsSingleton().TryAddScoped<IApiClient, ApiClient>();

        return services;
    }
    /// <summary>Registers the API client with generated metadata for its request payloads.</summary>
    public static IServiceCollection AddApiClientAsScoped(this IServiceCollection services, JsonSerializerContext jsonContext)
    {
        ArgumentNullException.ThrowIfNull(jsonContext);
        services.AddLogJsonInteropAsScoped().AddSessionUtilAsScoped().AddHttpClientCacheAsSingleton();
        services.TryAddScoped<IApiClient>(provider => new ApiClient(provider.GetRequiredService<ISessionUtil>(),
            provider.GetRequiredService<ILogJsonInterop>(), provider.GetRequiredService<IHttpClientCache>(), jsonContext));
        return services;
    }
}
