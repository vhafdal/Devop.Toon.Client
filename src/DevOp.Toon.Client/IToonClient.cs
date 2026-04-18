#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace DevOp.Toon.Client;

/// <summary>
/// Typed HTTP client for TOON-first APIs with JSON fallback.
/// </summary>
public interface IToonClient
{
    /// <summary>
    /// Sends a GET request and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PATCH request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request and ignores any response body.
    /// </summary>
    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    Task<T?> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default);
}
