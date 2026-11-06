using System.Net;
using System.Net.Http;
using System.Text;
using DevOp.Toon;
using Xunit;

namespace DevOp.Toon.Client.Tests;

public sealed class ResponseIndentOverrideTests
{
    [Theory]
    [InlineData(2, "2")]
    [InlineData(4, "4")]
    public async Task Get_IntegerIndentOverride_SendsNumericWidth(int width, string expectedHeader)
    {
        using var handler = new IndentRecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var client = new ToonClient(http, new ToonService(), new ToonClientOptions
        {
            ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions { Indent = width }
        });

        await client.GetAsync<Dictionary<string, object>>("items");

        Assert.Equal(expectedHeader, Assert.Single(handler.IndentValues!));
    }

    [Fact]
    public async Task Constructor_IntegerIndentMutatedAfterConstruction_PreservesClonedWidth()
    {
        using var handler = new IndentRecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };
        var overrides = new ToonResponseEncodeOverrideOptions { Indent = 2 };
        var options = new ToonClientOptions { ResponseEncodeOverrides = overrides };
        var client = new ToonClient(http, new ToonService(), options);
        overrides.Indent = 4;

        await client.GetAsync<Dictionary<string, object>>("items");

        Assert.Equal("2", Assert.Single(handler.IndentValues!));
        Assert.Equal(4, overrides.Indent);
    }

    private sealed class IndentRecordingHandler : HttpMessageHandler
    {
        internal string[]? IndentValues { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IndentValues = request.Headers.GetValues("X-Toon-Option-Indent").ToArray();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }
}
