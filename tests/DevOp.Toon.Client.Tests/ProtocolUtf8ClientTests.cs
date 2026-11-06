using System.Net;
using System.Text;
using DevOp.Toon;
using Xunit;

namespace DevOp.Toon.Client.Tests;

public sealed class ProtocolUtf8ClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task GetAsync_InvalidToonUtf8_PreservesClientExceptionMetadata(HttpStatusCode status)
    {
        using var http = new HttpClient(new InvalidUtf8Handler(status)) { BaseAddress = new Uri("https://example.test/") };
        var client = new ToonClient(http, new ToonService());
        var error = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));
        Assert.Equal(status, error.StatusCode);
        Assert.Equal("application/toon", error.ContentType);
        Assert.IsType<DecoderFallbackException>(error.InnerException);
    }

    private sealed class InvalidUtf8Handler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status) { Content = new ByteArrayContent([0xff]) };
            response.Content.Headers.ContentType = new("application/toon");
            response.Headers.Add("X-Toon-Version", "4.4");
            return Task.FromResult(response);
        }
    }
}
