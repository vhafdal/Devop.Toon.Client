#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOp.Toon;
using DevOp.Toon.Core;
using Microsoft.Extensions.Logging;

namespace DevOp.Toon.Client;

/// <summary>
/// Typed HTTP client for TOON-first APIs with JSON fallback.
/// </summary>
public sealed class ToonClient : IToonClient
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Encoding ReplacingUtf8 = new UTF8Encoding(false, false);
    private const string OptionHeaderPrefix = "X-Toon-Option-";
    private static readonly HttpMethod PatchMethod = new("PATCH");

    private readonly HttpClient httpClient;
    private readonly IToonService toon;
    private readonly ToonClientOptions options;
    private readonly ILogger<ToonClient>? logger;

    /// <summary>
    /// Creates a TOON-first typed client over the supplied <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The underlying HTTP client used to send requests.</param>
    /// <param name="toon">The TOON serialization service used to encode request bodies and decode responses.</param>
    /// <param name="options">
    /// Optional configuration; a default <see cref="ToonClientOptions"/> instance is used when <see langword="null"/>.
    /// The options are deep-cloned so later mutations to the passed instance have no effect.
    /// </param>
    /// <param name="logger">
    /// Optional logger. When <see langword="null"/> no output is produced.
    /// In DI scenarios the logger is resolved automatically from the container.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> or <paramref name="toon"/> is <see langword="null"/>.
    /// </exception>
    public ToonClient(HttpClient httpClient, IToonService toon, ToonClientOptions? options = null, ILogger<ToonClient>? logger = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.toon = toon ?? throw new ArgumentNullException(nameof(toon));
        this.options = options?.Clone() ?? new ToonClientOptions();
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, requestUri, content: null, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TResponse?> PatchAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(PatchMethod, requestUri, CreateToonContent(request), cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendWithoutBodyAsync(HttpMethod.Delete, requestUri, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T?> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Delete, requestUri, content: null, cancellationToken);
    }

    private async Task SendWithoutBodyAsync(HttpMethod method, string requestUri, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, requestUri, content: null);
        logger?.LogDebug("Sending {Method} {Uri}", method, request.RequestUri);

        var sw = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var elapsedMs = sw.ElapsedMilliseconds;

        await EnsureSuccessAsync(response, elapsedMs).ConfigureAwait(false);

        logger?.LogDebug("{Method} {Uri} → {StatusCode} {ElapsedMs}ms",
            method, request.RequestUri, (int)response.StatusCode, elapsedMs);
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string requestUri, HttpContent? content, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, requestUri, content);
        logger?.LogDebug("Sending {Method} {Uri}", method, request.RequestUri);

        var sw = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var elapsedMs = sw.ElapsedMilliseconds;

        await EnsureSuccessAsync(response, elapsedMs).ConfigureAwait(false);

        if (response.Content == null)
            return default;

        if (response.Content.Headers.ContentLength == 0)
            return default;

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var decodeOptions = ResolveResponseOptions(response);
        var body = await ReadResponseBodyAsync(response, contentType, decodeOptions).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(body))
            return default;

        try
        {
            T? result;

            if (IsToonMediaType(contentType))
                result = DecodeToon<T>(body, decodeOptions);
            else if (IsJsonMediaType(contentType))
                result = JsonSerializer.Deserialize<T>(body, options.JsonSerializerOptions);
            else
            {
                logger?.LogWarning("Unsupported response content type {ContentType} for {Method} {Uri}",
                    contentType ?? "<missing>", method, request.RequestUri);

                throw new ToonClientException(
                    $"Unsupported response content type '{contentType ?? "<missing>"}' for {method} {request.RequestUri}.",
                    content: body,
                    contentType: contentType,
                    statusCode: response.StatusCode,
                    decoder: CreateDecoder(contentType, body, decodeOptions),
                    innerException: null);
            }

            logger?.LogDebug("{Method} {Uri} → {StatusCode} ({ContentType}) {ElapsedMs}ms",
                method, request.RequestUri, (int)response.StatusCode, contentType, elapsedMs);

            return result;
        }
        catch (Exception ex) when (ex is not ToonClientException)
        {
            logger?.LogWarning(ex, "Failed to deserialize {Method} {Uri} response as {Type} from {ContentType}",
                method, request.RequestUri, typeof(T).Name, contentType ?? "<missing>");

            throw new ToonClientException(
                $"Failed to deserialize {method} {request.RequestUri} as {typeof(T).FullName} from '{contentType ?? "<missing>"}'.",
                content: body,
                contentType: contentType,
                statusCode: response.StatusCode,
                decoder: CreateDecoder(contentType, body, decodeOptions),
                innerException: ex);
        }
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, string? contentType, ToonDecodeOptions? decode)
    {
        var content = response.Content!;
        if (!IsToonMediaType(contentType)) return await content.ReadAsStringAsync().ConfigureAwait(false);
        var replaceInvalid = decode?.ProtocolVersion is ToonProtocolVersion.CustomLegacy or ToonProtocolVersion.V3_0
            || decode?.Strict == false && decode.ProtocolVersion is ToonProtocolVersion.V4_2 or ToonProtocolVersion.V4_3;
        var bytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
        try { return (replaceInvalid ? ReplacingUtf8 : StrictUtf8).GetString(bytes); }
        catch (DecoderFallbackException error)
        {
            throw new ToonClientException("The TOON response body is not valid UTF-8.", content: null,
                contentType: contentType, statusCode: response.StatusCode, decoder: null, innerException: error);
        }
    }

    private ToonDecodeOptions? ResolveResponseOptions(HttpResponseMessage response)
    {
        if (!IsToonMediaType(response.Content?.Headers.ContentType?.MediaType)) return null;
        var decode = options.DecodeOptions?.Clone();
        if (!response.Headers.TryGetValues("X-Toon-Version", out var headers)) return decode;
        using var values = headers.GetEnumerator();
        if (!values.MoveNext() || !ToonProtocolVersions.TryParse(values.Current, out var version) || values.MoveNext()) throw new ToonClientException("Invalid or unsupported response protocol version.");
        decode ??= new ToonDecodeOptions();
        if (decode.ProtocolVersion != ToonProtocolVersion.Auto && decode.ProtocolVersion != version) throw new ToonClientException("Response protocol version conflicts with configured decoder.");
        decode.ProtocolVersion = version;
        return decode;
    }

    private T? DecodeToon<T>(string body, ToonDecodeOptions? decodeOptions)
    {
        if (decodeOptions != null && options.DecodeOptions == null) decodeOptions = ToonDecoder.DetectOptions(body, decodeOptions);
        try
        {
            if (decodeOptions != null)
                return toon.Decode<T>(body, decodeOptions);

            return toon.Decode<T>(body);
        }
        catch (NotSupportedException)
        {
            logger?.LogDebug("TOON native decode not supported for {Type}, falling back to Toon2Json", typeof(T).Name);

            var json = decodeOptions != null
                ? toon.Toon2Json(body, decodeOptions)
                : toon.Toon2Json(body);

            return JsonSerializer.Deserialize<T>(json, options.JsonSerializerOptions);
        }
    }

    private Func<Type, object?> CreateDecoder(string? contentType, string? content, ToonDecodeOptions? decodeOptions)
    {
        return type =>
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            try
            {
                if (IsToonMediaType(contentType))
                {
                    if (decodeOptions != null && options.DecodeOptions == null) decodeOptions = ToonDecoder.DetectOptions(content!, decodeOptions);
                    var json = decodeOptions != null
                        ? toon.Toon2Json(content!, decodeOptions)
                        : toon.Toon2Json(content!);
                    return JsonSerializer.Deserialize(json, type, options.JsonSerializerOptions);
                }

                if (IsJsonMediaType(contentType))
                    return JsonSerializer.Deserialize(content!, type, options.JsonSerializerOptions);
            }
            catch (Exception ex) when (ex is not ToonClientException)
            {
                throw new ToonClientException(
                    $"Failed to decode error body as {type.FullName} from '{contentType ?? "<missing>"}'.", ex);
            }

            throw new ToonClientException(
                $"Unsupported content type '{contentType ?? "<missing>"}' for Decode<T>().");
        };
    }

    private sealed class VersionedToonContent : StringContent
    {
        internal VersionedToonContent(string payload, string mediaType, ToonProtocolVersion version) : base(payload, Encoding.UTF8, mediaType) => Version = version;
        internal ToonProtocolVersion Version { get; }
    }

    private StringContent CreateToonContent<T>(T request)
    {
        var encode = options.EncodeOptions ?? ToonEncoder.DefaultOptions;
        var payload = toon.Encode(request, encode);
        return new VersionedToonContent(payload, options.ToonMediaType, encode.ProtocolVersion);
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
        if (content is VersionedToonContent toonContent) request.Headers.TryAddWithoutValidation("X-Toon-Version", ToonProtocolVersions.Format(toonContent.Version));
        return request;
    }

    private void ApplyResponseEncodeOverrideHeaders(HttpRequestMessage request)
    {
        var overrides = options.ResponseEncodeOverrides;
        if (overrides == null)
            return;

        if (overrides.ProtocolVersion.HasValue) request.Headers.TryAddWithoutValidation("X-Toon-Option-ProtocolVersion", ToonProtocolVersions.Format(overrides.ProtocolVersion.Value));
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

    private async Task EnsureSuccessAsync(HttpResponseMessage response, long elapsedMs)
    {
        if (response.IsSuccessStatusCode)
            return;

        string? errorBody = null;
        string? errorContentType = null;
        var decodeOptions = ResolveResponseOptions(response);

        if (response.Content != null)
        {
            errorContentType = response.Content.Headers.ContentType?.MediaType;
            errorBody = await ReadResponseBodyAsync(response, errorContentType, decodeOptions).ConfigureAwait(false);
        }

        var statusCode = response.StatusCode;

        logger?.LogWarning("{Method} {Uri} failed with {StatusCode} {ElapsedMs}ms",
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            (int)statusCode,
            elapsedMs);

        throw new ToonClientException(
            $"Request failed with status code {(int)statusCode} ({statusCode}).",
            content: errorBody,
            contentType: errorContentType,
            statusCode: statusCode,
            decoder: CreateDecoder(errorContentType, errorBody, decodeOptions),
            innerException: null);
    }
}
