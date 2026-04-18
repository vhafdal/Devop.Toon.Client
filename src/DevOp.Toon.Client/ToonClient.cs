#nullable enable
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOp.Toon;

namespace DevOp.Toon.Client;

/// <summary>
/// Typed HTTP client for TOON-first APIs with JSON fallback.
/// </summary>
public sealed class ToonClient : IToonClient
{
    private const string OptionHeaderPrefix = "X-Toon-Option-";
    private static readonly HttpMethod PatchMethod = new("PATCH");

    private readonly HttpClient httpClient;
    private readonly IToonService toon;
    private readonly ToonClientOptions options;

    /// <summary>
    /// Creates a TOON-first typed client over the supplied <see cref="HttpClient"/>.
    /// </summary>
    public ToonClient(HttpClient httpClient, IToonService toon, ToonClientOptions? options = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.toon = toon ?? throw new ArgumentNullException(nameof(toon));
        this.options = options?.Clone() ?? new ToonClientOptions();
    }

    /// <summary>
    /// Sends a GET request and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, requestUri, content: null, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    public Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <summary>
    /// Sends a PATCH request with a TOON body and deserializes the response body to <typeparamref name="TResponse"/>.
    /// </summary>
    public Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(PatchMethod, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request and ignores any response body.
    /// </summary>
    public Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendWithoutBodyAsync(HttpMethod.Delete, requestUri, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    public Task<T?> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Delete, requestUri, content: null, cancellationToken);
    }

    private async Task SendWithoutBodyAsync(HttpMethod method, string requestUri, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, requestUri, content: null);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string requestUri, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, requestUri, content);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        if (response.Content == null)
            return default;

        if (response.Content.Headers.ContentLength == 0)
            return default;

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(body))
            return default;

        try
        {
            if (IsToonMediaType(contentType))
                return DecodeToon<T>(body);

            if (IsJsonMediaType(contentType))
                return JsonSerializer.Deserialize<T>(body, options.JsonSerializerOptions);
        }
        catch (Exception ex) when (ex is not ToonClientException)
        {
            throw new ToonClientException($"Failed to deserialize {method} {request.RequestUri} as {typeof(T).FullName} from '{contentType ?? "<missing>"}'.", ex);
        }

        throw new ToonClientException($"Unsupported response content type '{contentType ?? "<missing>"}' for {method} {request.RequestUri}.");
    }

    private T? DecodeToon<T>(string body)
    {
        try
        {
            if (options.DecodeOptions != null)
                return toon.Decode<T>(body, options.DecodeOptions);

            return toon.Decode<T>(body);
        }
        catch (NotSupportedException)
        {
            var json = options.DecodeOptions != null
                ? toon.Toon2Json(body, options.DecodeOptions)
                : toon.Toon2Json(body);

            return JsonSerializer.Deserialize<T>(json, options.JsonSerializerOptions);
        }
    }

    private StringContent CreateToonContent<T>(T request)
    {
        var payload = options.EncodeOptions != null
            ? toon.Encode(request, options.EncodeOptions)
            : toon.Encode(request);

        return new StringContent(payload, Encoding.UTF8, options.ToonMediaType);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, HttpContent? content)
    {
        if (string.IsNullOrWhiteSpace(requestUri))
            throw new ArgumentException("Request URI must be provided.", nameof(requestUri));

        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ToonMediaTypes.Application));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ToonMediaTypes.Text, 0.9));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.8));
        ApplyResponseEncodeOverrideHeaders(request);
        request.Content = content;
        return request;
    }

    private void ApplyResponseEncodeOverrideHeaders(HttpRequestMessage request)
    {
        var overrides = options.ResponseEncodeOverrides;
        if (overrides == null)
            return;

        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.Indent), overrides.Indent);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.Delimiter), overrides.Delimiter);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.KeyFolding), overrides.KeyFolding);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.FlattenDepth), overrides.FlattenDepth);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.ObjectArrayLayout), overrides.ObjectArrayLayout);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.IgnoreNullOrEmpty), overrides.IgnoreNullOrEmpty);
        AddOverrideHeader(request, nameof(ToonResponseEncodeOverrideOptions.ExcludeEmptyArrays), overrides.ExcludeEmptyArrays);
    }

    private static void AddOverrideHeader<T>(HttpRequestMessage request, string optionName, T? value)
        where T : struct
    {
        if (!value.HasValue)
            return;

        request.Headers.TryAddWithoutValidation(OptionHeaderPrefix + optionName, FormatHeaderValue(value.Value));
    }

    private static string FormatHeaderValue<T>(T value)
        where T : struct
    {
        return value switch
        {
            bool booleanValue => booleanValue ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool IsToonMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            return false;

        var value = mediaType!;
        return value.Equals(ToonMediaTypes.Application, StringComparison.OrdinalIgnoreCase)
            || value.Equals(ToonMediaTypes.Text, StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("+toon", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
            return false;

        var value = mediaType!;
        return value.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || value.Equals("text/json", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string? errorBody = null;

        if (response.Content != null)
            errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        var message = $"Request failed with status code {(int)response.StatusCode} ({response.StatusCode}).";
        if (!string.IsNullOrWhiteSpace(errorBody))
            message = $"{message} Response body: {errorBody}";

        throw new HttpRequestException(message);
    }
}
