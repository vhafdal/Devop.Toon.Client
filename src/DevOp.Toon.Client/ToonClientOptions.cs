#nullable enable
using System;
using System.Text.Json;
using DevOp.Toon;

namespace DevOp.Toon.Client;

/// <summary>
/// Options for <see cref="ToonClient"/>.
/// </summary>
public sealed class ToonClientOptions
{
    /// <summary>
    /// Optional base address for requests created through DI registration.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Optional timeout for the underlying <see cref="HttpClient"/>.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Optional encode overrides used for TOON request bodies.
    /// </summary>
    public ToonEncodeOptions? EncodeOptions { get; set; }

    /// <summary>
    /// Optional decode overrides used for TOON responses.
    /// </summary>
    public ToonDecodeOptions? DecodeOptions { get; set; }

    /// <summary>
    /// Optional per-request response encode overrides sent through <c>X-Toon-Option-*</c> headers.
    /// </summary>
    public ToonResponseEncodeOverrideOptions? ResponseEncodeOverrides { get; set; }

    /// <summary>
    /// <see cref="System.Text.Json.JsonSerializerOptions"/> used when deserializing JSON responses or
    /// when TOON decoding falls back to JSON conversion via <c>Toon2Json</c>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="System.Text.Json.JsonSerializerDefaults.Web"/> settings
    /// (case-insensitive property matching, camelCase naming policy).
    /// </remarks>
#if NETSTANDARD2_0
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
#else
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);
#endif

    /// <summary>
    /// The <c>Content-Type</c> media type used when encoding TOON request bodies.
    /// </summary>
    /// <value>
    /// Defaults to <c>application/toon</c> (<see cref="ToonMediaTypes.Application"/>).
    /// Set to <c>text/toon</c> (<see cref="ToonMediaTypes.Text"/>) when the server requires it.
    /// </value>
    public string ToonMediaType { get; set; } = ToonMediaTypes.Application;

    /// <summary>
    /// When <see langword="true"/> (the default), the registered <see cref="HttpClient"/> requests
    /// GZip, Deflate, and (on .NET 5+) Brotli decompression automatically.
    /// Set to <see langword="false"/> to disable all automatic decompression.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    internal ToonClientOptions Clone()
    {
        return new ToonClientOptions
        {
            BaseAddress = BaseAddress,
            Timeout = Timeout,
            EncodeOptions = CloneEncodeOptions(EncodeOptions),
            DecodeOptions = CloneDecodeOptions(DecodeOptions),
            ResponseEncodeOverrides = ResponseEncodeOverrides?.Clone(),
            JsonSerializerOptions = new JsonSerializerOptions(JsonSerializerOptions),
            ToonMediaType = ToonMediaType,
            EnableCompression = EnableCompression
        };
    }

    private static ToonEncodeOptions? CloneEncodeOptions(ToonEncodeOptions? options)
    {
        if (options == null)
            return null;

        return new ToonEncodeOptions
        {
            Indent = options.Indent,
            Delimiter = options.Delimiter,
            KeyFolding = options.KeyFolding,
            FlattenDepth = options.FlattenDepth,
            ObjectArrayLayout = options.ObjectArrayLayout,
            IgnoreNullOrEmpty = options.IgnoreNullOrEmpty,
            ExcludeEmptyArrays = options.ExcludeEmptyArrays
        };
    }

    private static ToonDecodeOptions? CloneDecodeOptions(ToonDecodeOptions? options)
    {
        if (options == null)
            return null;

        return new ToonDecodeOptions
        {
            Indent = options.Indent,
            Strict = options.Strict,
            ExpandPaths = options.ExpandPaths,
            ObjectArrayLayout = options.ObjectArrayLayout
        };
    }
}
