#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    /// <remarks>
    /// Also registers <see cref="IToonService"/> if it has not already been registered.
    /// When <see cref="ToonClientOptions.EnableCompression"/> is <see langword="true"/> (the default),
    /// the primary HTTP handler is configured for automatic response decompression.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to register into.</param>
    /// <param name="configure">
    /// An optional callback to configure <see cref="ToonClientOptions"/> before registration.
    /// </param>
    /// <returns>The <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="ToonClientOptions.ToonMediaType"/> is null or whitespace after configuration.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="ToonClientOptions.Timeout"/> is zero or negative.
    /// </exception>
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
        })
        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var registeredOptions = serviceProvider.GetRequiredService<ToonClientOptions>();
            var handler = new HttpClientHandler();

            if (registeredOptions.EnableCompression)
            {
#if NETSTANDARD2_0
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
#else
                handler.AutomaticDecompression = DecompressionMethods.All;
#endif
            }

            return handler;
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
