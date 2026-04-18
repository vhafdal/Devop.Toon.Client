#nullable enable
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DevOp.Toon;

namespace DevOp.Toon.Client;

/// <summary>
/// Dependency injection registration helpers for <see cref="IToonClient"/>.
/// </summary>
public static class ToonClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IToonClient"/> as a typed <see cref="HttpClient"/> with TOON-first defaults.
    /// </summary>
    public static IServiceCollection AddToonClient(this IServiceCollection services, Action<ToonClientOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (!services.Any(service => service.ServiceType == typeof(IToonService)))
            services.AddToon();

        var options = new ToonClientOptions();
        configure?.Invoke(options);
        Validate(options);

        services.TryAddSingleton(options.Clone());
        services.AddHttpClient<IToonClient, ToonClient>((serviceProvider, httpClient) =>
        {
            var registeredOptions = serviceProvider.GetRequiredService<ToonClientOptions>();
            if (registeredOptions.BaseAddress != null)
                httpClient.BaseAddress = registeredOptions.BaseAddress;

            if (registeredOptions.Timeout.HasValue)
                httpClient.Timeout = registeredOptions.Timeout.Value;
        });

        return services;
    }

    private static void Validate(ToonClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ToonMediaType))
            throw new ArgumentException("TOON media type must be provided.", nameof(options));

        if (options.Timeout.HasValue && options.Timeout.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), options.Timeout, "Timeout must be greater than zero.");
    }
}
