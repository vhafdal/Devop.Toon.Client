using System.Net;
using System.Reflection;
using System.Text;
using DevOp.Toon.Core;
using Xunit;

namespace DevOp.Toon.Client.Tests;

public sealed class ProtocolVersionClientTests
{
    [Fact]
    public async Task Post_DifferentRequestAndResponseVersions_PreservesBothSelections()
    {
        using var handler = Handler("# server metadata\nCode: 42", "4.4");
        using var http = Http(handler);
        var options = new ToonClientOptions
        {
            EncodeOptions = new ToonEncodeOptions { ProtocolVersion = ToonProtocolVersion.CustomLegacy },
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions { ProtocolVersion = ToonProtocolVersion.V4_4 }
        };
        var client = new ToonClient(http, new ToonService(), options);
        var response = await client.PostAsync<ProtocolClientModel, ProtocolClientModel>("items", new ProtocolClientModel { Code = 7 });
        Assert.Equal(42, response!.Code);
        Assert.Equal("custom-legacy", Assert.Single(handler.RequestHeaders!["X-Toon-Version"]));
        Assert.Equal("4.4", Assert.Single(handler.RequestHeaders["X-Toon-Option-ProtocolVersion"]));
        Assert.Equal(7, ToonDecoder.Decode<ProtocolClientModel>(handler.RequestBody!, new ToonDecodeOptions { ProtocolVersion = ToonProtocolVersion.CustomLegacy, Indent = 1 })!.Code);
    }

    [Fact]
    public async Task Get_ErrorBodyDecoder_RetainsResponseVersionAfterDisposal()
    {
        using var handler = Handler("# error context\nCode: 42", "4.4", HttpStatusCode.UnprocessableEntity);
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        var error = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ProtocolClientModel>("items"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.StatusCode);
        Assert.Equal(42, error.Decode<ProtocolClientModel>()!.Code);
    }

    [Fact]
    public async Task Get_LegacyHashErrorBodyDecoder_DoesNotTreatDataAsComment()
    {
        using var handler = Handler("#literal", "custom-legacy", HttpStatusCode.BadRequest);
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        var error = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));
        Assert.Equal("#literal", error.Decode<string>());
    }

    [Fact]
    public async Task Get_TypedFallback_PassesResponseVersionToJsonConversion()
    {
        var service = DispatchProxy.Create<IToonService, VersionFallbackProxy>();
        var proxy = (VersionFallbackProxy)(object)service;
        using var handler = Handler("# response metadata\nCode: 42", "4.4");
        using var http = Http(handler);
        var client = new ToonClient(http, service);
        var value = await client.GetAsync<ProtocolClientModel>("items");
        Assert.Equal(42, value!.Code);
        Assert.Equal(1, proxy.TypedAttempts);
        Assert.Equal(1, proxy.JsonConversions);
        Assert.Equal(ToonProtocolVersion.V4_4, proxy.ConversionVersion);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("9.9")]
    [InlineData("4.4,4.0")]
    public async Task Get_InvalidResponseVersion_RejectsBody(string version)
    {
        using var handler = Handler("Code: 42", version);
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ProtocolClientModel>("items"));
    }

    [Fact]
    public async Task Get_MultipleResponseVersionFields_RejectsBody()
    {
        using var handler = new VersionResponseHandler(() => Response("Code: 42", new[] { "4.4", "4.0" }));
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ProtocolClientModel>("items"));
    }

    [Fact]
    public async Task Get_ResponseVersionConflictsWithPinnedDecoder_RejectsBody()
    {
        using var handler = Handler("Code: 42", "4.4");
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService(), new ToonClientOptions
        {
            DecodeOptions = new ToonDecodeOptions { ProtocolVersion = ToonProtocolVersion.V4_0 }
        });
        await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ProtocolClientModel>("items"));
    }

    [Fact]
    public async Task Get_AmbiguousBodyWithoutMetadata_RequiresVersionHint()
    {
        using var handler = new VersionResponseHandler(() => Response("# metadata\nCode: 42", Array.Empty<string>()));
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ProtocolClientModel>("items"));
    }

    [Fact]
    public async Task Get_JsonErrorWithIrrelevantToonVersion_PreservesJsonErrorDecoder()
    {
        using var handler = new VersionResponseHandler(() => Response("{\"code\":42}", new[] { "9.9" }, HttpStatusCode.BadRequest, "application/problem+json"));
        using var http = Http(handler);
        var client = new ToonClient(http, new ToonService());
        var error = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal(42, error.Decode<ProtocolClientModel>()!.Code);
    }

    [Fact]
    public async Task Constructor_OptionsMutatedLater_UsesClonedProtocolSelections()
    {
        using var handler = Handler("# metadata\nCode: 42", "4.4");
        using var http = Http(handler);
        var options = new ToonClientOptions
        {
            EncodeOptions = new ToonEncodeOptions { ProtocolVersion = ToonProtocolVersion.CustomLegacy },
            DecodeOptions = new ToonDecodeOptions { ProtocolVersion = ToonProtocolVersion.Auto },
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions { ProtocolVersion = ToonProtocolVersion.V4_4 }
        };
        var client = new ToonClient(http, new ToonService(), options);
        options.EncodeOptions.ProtocolVersion = ToonProtocolVersion.V4_4;
        options.DecodeOptions.ProtocolVersion = ToonProtocolVersion.CustomLegacy;
        options.ResponseEncodeOverrides.ProtocolVersion = ToonProtocolVersion.CustomLegacy;
        var result = await client.PostAsync<ProtocolClientModel, ProtocolClientModel>("items", new ProtocolClientModel { Code = 7 });
        Assert.Equal(42, result!.Code);
        Assert.Equal("custom-legacy", Assert.Single(handler.RequestHeaders!["X-Toon-Version"]));
        Assert.Equal("4.4", Assert.Single(handler.RequestHeaders["X-Toon-Option-ProtocolVersion"]));
    }

    private static HttpClient Http(HttpMessageHandler handler) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://example.test/") };
    private static VersionResponseHandler Handler(string body, string version, HttpStatusCode status = HttpStatusCode.OK)
        => new(() => Response(body, new[] { version }, status));
    private static HttpResponseMessage Response(string body, string[] versions, HttpStatusCode status = HttpStatusCode.OK, string contentType = "application/toon")
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };
        if (versions.Length > 0) response.Headers.TryAddWithoutValidation("X-Toon-Version", versions);
        return response;
    }

    private sealed class VersionResponseHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        internal Dictionary<string, string[]>? RequestHeaders { get; private set; }
        internal string? RequestBody { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestHeaders = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
            RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return response();
        }
    }
}

public sealed class ProtocolClientModel
{
    public int Code { get; set; }
}

public class VersionFallbackProxy : DispatchProxy
{
    private readonly ToonService service = new();
    public int TypedAttempts { get; private set; }
    public int JsonConversions { get; private set; }
    public ToonProtocolVersion? ConversionVersion { get; private set; }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == "Decode" && targetMethod.IsGenericMethod)
        {
            TypedAttempts++;
            throw new NotSupportedException("Exercise the existing client JSON fallback.");
        }
        if (targetMethod.Name == "Toon2Json")
        {
            JsonConversions++;
            ConversionVersion = args?.OfType<ToonDecodeOptions>().SingleOrDefault()?.ProtocolVersion;
        }
        return targetMethod.Invoke(service, args);
    }
}
