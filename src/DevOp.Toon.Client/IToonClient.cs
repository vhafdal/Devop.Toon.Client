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
    /// <typeparam name="T">The type to deserialize the response body into.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A task that resolves to the deserialized response body, or <see langword="null"/> if the response has no body.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">
    /// Thrown when the server returns a non-success status code, the response content type is unsupported,
    /// or deserialization of the response body fails.
    /// </exception>
    Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a TOON-encoded body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type to serialize as the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response body into.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="request">The object to serialize and send as the request body.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A task that resolves to the deserialized response body, or <see langword="null"/> if the response has no body.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">
    /// Thrown when the server returns a non-success status code, the response content type is unsupported,
    /// or deserialization of the response body fails.
    /// </exception>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with a TOON-encoded body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type to serialize as the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response body into.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="request">The object to serialize and send as the request body.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A task that resolves to the deserialized response body, or <see langword="null"/> if the response has no body.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">
    /// Thrown when the server returns a non-success status code, the response content type is unsupported,
    /// or deserialization of the response body fails.
    /// </exception>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PATCH request with a TOON-encoded body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type to serialize as the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response body into.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="request">The object to serialize and send as the request body.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A task that resolves to the deserialized response body, or <see langword="null"/> if the response has no body.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">
    /// Thrown when the server returns a non-success status code, the response content type is unsupported,
    /// or deserialization of the response body fails.
    /// </exception>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request and ignores any response body.
    /// </summary>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the request succeeds.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">Thrown when the server returns a non-success status code.</exception>
    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response body into.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A task that resolves to the deserialized response body, or <see langword="null"/> if the response has no body.
    /// </returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="requestUri"/> is null or whitespace.</exception>
    /// <exception cref="ToonClientException">
    /// Thrown when the server returns a non-success status code, the response content type is unsupported,
    /// or deserialization of the response body fails.
    /// </exception>
    Task<T?> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default);
}
