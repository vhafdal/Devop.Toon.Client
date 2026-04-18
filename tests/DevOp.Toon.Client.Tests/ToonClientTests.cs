using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using DevOp.Toon.Core;
using DevOp.Toon;
using Xunit;

namespace DevOp.Toon.Client.Tests;

public sealed class ToonClientTests
{
    [Fact]
    public async Task GetAsync_DoesNotSendOverrideHeaders_WhenOverridesAreNotConfigured()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, new ToonClientOptions());

        _ = await client.GetAsync<object>("items");

        Assert.NotNull(handler.Request);
        Assert.False(handler.Request!.Headers.Contains("X-Toon-Option-Indent"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-Delimiter"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-KeyFolding"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-FlattenDepth"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-ObjectArrayLayout"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-IgnoreNullOrEmpty"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-ExcludeEmptyArrays"));
    }

    [Fact]
    public async Task GetAsync_SendsOnlyConfiguredOverrideHeaders()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, new ToonClientOptions
        {
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
            {
                Delimiter = ToonDelimiter.COMMA,
                KeyFolding = ToonKeyFolding.Off,
                IgnoreNullOrEmpty = true,
                FlattenDepth = 3
            }
        });

        _ = await client.GetAsync<object>("items");

        Assert.NotNull(handler.Request);
        AssertHeader(handler.Request!, "X-Toon-Option-Delimiter", "COMMA");
        AssertHeader(handler.Request, "X-Toon-Option-KeyFolding", "Off");
        AssertHeader(handler.Request, "X-Toon-Option-IgnoreNullOrEmpty", "true");
        AssertHeader(handler.Request, "X-Toon-Option-FlattenDepth", "3");
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-Indent"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-ObjectArrayLayout"));
        Assert.False(handler.Request.Headers.Contains("X-Toon-Option-ExcludeEmptyArrays"));
    }

    [Fact]
    public async Task PostAsync_PreservesExistingRequestBehavior_WhenSendingOverrideHeaders()
    {
        var handler = new RecordingHandler();
        var client = CreateClient(handler, new ToonClientOptions
        {
            ToonMediaType = ToonMediaTypes.Text,
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
            {
                ObjectArrayLayout = ToonObjectArrayLayout.Columnar,
                ExcludeEmptyArrays = false
            }
        });

        _ = await client.PostAsync<object, object>("items", new { Name = "Widget" });

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(ToonMediaTypes.Text, handler.Request.Content!.Headers.ContentType!.MediaType);
        AssertHeader(handler.Request, "X-Toon-Option-ObjectArrayLayout", "Columnar");
        AssertHeader(handler.Request, "X-Toon-Option-ExcludeEmptyArrays", "false");
        Assert.Contains(handler.Request.Headers.Accept, value => value.MediaType == ToonMediaTypes.Application);
        Assert.Contains(handler.Request.Headers.Accept, value => value.MediaType == ToonMediaTypes.Text);
        Assert.Contains(handler.Request.Headers.Accept, value => value.MediaType == "application/json");
    }

    [Fact]
    public async Task Constructor_ClonesResponseEncodeOverrides()
    {
        var handler = new RecordingHandler();
        var options = new ToonClientOptions
        {
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
            {
                Delimiter = ToonDelimiter.COMMA,
                IgnoreNullOrEmpty = true
            }
        };

        var client = CreateClient(handler, options);
        options.ResponseEncodeOverrides!.Delimiter = ToonDelimiter.TAB;
        options.ResponseEncodeOverrides.IgnoreNullOrEmpty = false;

        _ = await client.GetAsync<object>("items");

        Assert.NotNull(handler.Request);
        AssertHeader(handler.Request!, "X-Toon-Option-Delimiter", "COMMA");
        AssertHeader(handler.Request, "X-Toon-Option-IgnoreNullOrEmpty", "true");
    }

    private static ToonClient CreateClient(RecordingHandler handler, ToonClientOptions options)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new ToonClient(httpClient, new ToonService(), options);
    }

    private static void AssertHeader(HttpRequestMessage request, string name, string expectedValue)
    {
        Assert.True(request.Headers.TryGetValues(name, out var values));
        Assert.Equal(expectedValue, Assert.Single(values));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }

}
