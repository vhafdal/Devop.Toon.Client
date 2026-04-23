using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DevOp.Toon.Core;
using DevOp.Toon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

    // --- Exception property tests ---

    [Fact]
    public async Task GetAsync_ThrowsToonClientException_WithStatusCodeAndBody_WhenResponseIsNonSuccess()
    {
        var handler = new RespondingHandler(HttpStatusCode.BadRequest, "{\"error\":\"bad\"}", "application/json");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("application/json", ex.ContentType);
        Assert.Equal("{\"error\":\"bad\"}", ex.Content);
    }

    [Fact]
    public async Task GetAsync_ExceptionDecodeT_DeserializesJsonErrorBody()
    {
        var handler = new RespondingHandler(HttpStatusCode.UnprocessableEntity, "{\"code\":42}", "application/json");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        var decoded = ex.Decode<ErrorBody>();
        Assert.NotNull(decoded);
        Assert.Equal(42, decoded!.Code);
    }

    [Fact]
    public async Task GetAsync_ExceptionDecodeT_DeserializesToonErrorBody()
    {
        var toonService = new ToonService();
        var errorObj = new ErrorBody { Code = 99 };
        var toonBody = toonService.Encode(errorObj);

        var handler = new RespondingHandler(HttpStatusCode.Conflict, toonBody, ToonMediaTypes.Application);
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        var decoded = ex.Decode<ErrorBody>();
        Assert.NotNull(decoded);
        Assert.Equal(99, decoded!.Code);
    }

    [Fact]
    public async Task GetAsync_ExceptionDecodeT_ThrowsInvalidOperationException_ForUnsupportedContentType()
    {
        var handler = new RespondingHandler(HttpStatusCode.BadRequest, "raw text error", "text/plain");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Equal("text/plain", ex.ContentType);
        Assert.Equal("raw text error", ex.Content);
        Assert.Throws<ToonClientException>(() => ex.Decode<object>());
    }

    [Fact]
    public async Task GetAsync_ExceptionIncludesStatusCode_WhenDeserializationFailsOnSuccessResponse()
    {
        var handler = new RespondingHandler(HttpStatusCode.OK, "this is not json", "application/json");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ErrorBody>("items"));

        Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
        Assert.Equal("application/json", ex.ContentType);
        Assert.Equal("this is not json", ex.Content);
    }

    [Fact]
    public async Task GetAsync_ThrowsToonClientException_ForUnsupportedSuccessContentType()
    {
        var handler = new RespondingHandler(HttpStatusCode.OK, "hello", "text/plain");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Equal(HttpStatusCode.OK, ex.StatusCode);
        Assert.Equal("text/plain", ex.ContentType);
        Assert.Equal("hello", ex.Content);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public async Task GetAsync_ExceptionDecodeT_ThrowsToonClientException_ForUnsupportedSuccessContentType()
    {
        var handler = new RespondingHandler(HttpStatusCode.OK, "hello", "text/plain");
        var client = CreateClient(handler, new ToonClientOptions());

        var ex = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Throws<ToonClientException>(() => ex.Decode<object>());
    }

    [Fact]
    public void Decode_ThrowsInvalidOperationException_WhenExceptionCreatedViaPublicConstructor()
    {
        var ex = new ToonClientException("test message");
        Assert.Throws<InvalidOperationException>(() => ex.Decode<object>());
    }

    [Fact]
    public void ToonClientException_PublicProperties_AreNullWhenCreatedViaPublicConstructor()
    {
        var ex = new ToonClientException("test");
        Assert.Null(ex.Content);
        Assert.Null(ex.ContentType);
        Assert.Null(ex.StatusCode);
    }

    // --- Logger tests ---

    [Fact]
    public async Task GetAsync_LogsDebug_WhenRequestSucceeds()
    {
        var logger = new RecordingLogger<ToonClient>();
        var handler = new RecordingHandler();
        var client = CreateClient(handler, new ToonClientOptions(), logger);

        _ = await client.GetAsync<object>("items");

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Sending"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("200"));
    }

    [Fact]
    public async Task GetAsync_LogsWarning_WhenRequestFails()
    {
        var logger = new RecordingLogger<ToonClient>();
        var handler = new RespondingHandler(HttpStatusCode.BadRequest, "{\"error\":\"bad\"}", "application/json");
        var client = CreateClient(handler, new ToonClientOptions(), logger);

        _ = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("400"));
    }

    [Fact]
    public async Task GetAsync_LogsWarning_WhenDeserializationFails()
    {
        var logger = new RecordingLogger<ToonClient>();
        var handler = new RespondingHandler(HttpStatusCode.OK, "not json", "application/json");
        var client = CreateClient(handler, new ToonClientOptions(), logger);

        _ = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<ErrorBody>("items"));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("deserialize"));
    }

    [Fact]
    public async Task GetAsync_LogsWarning_WhenContentTypeIsUnsupported()
    {
        var logger = new RecordingLogger<ToonClient>();
        var handler = new RespondingHandler(HttpStatusCode.OK, "hello", "text/plain");
        var client = CreateClient(handler, new ToonClientOptions(), logger);

        _ = await Assert.ThrowsAsync<ToonClientException>(() => client.GetAsync<object>("items"));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("text/plain"));
    }

    // --- Compression DI smoke tests ---

    [Fact]
    public void AddToonClient_BuildsSuccessfully_WhenEnableCompressionIsTrue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToonClient(o => o.EnableCompression = true);
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IToonClient>());
    }

    [Fact]
    public void AddToonClient_BuildsSuccessfully_WhenEnableCompressionIsFalse()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToonClient(o => o.EnableCompression = false);
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IToonClient>());
    }

    // --- Helpers ---

    private static ToonClient CreateClient(RecordingHandler handler, ToonClientOptions options, ILogger<ToonClient>? logger = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new ToonClient(httpClient, new ToonService(), options, logger);
    }

    private static ToonClient CreateClient(RespondingHandler handler, ToonClientOptions options, ILogger<ToonClient>? logger = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        return new ToonClient(httpClient, new ToonService(), options, logger);
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

    private sealed class RespondingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;
        private readonly string _mediaType;

        public RespondingHandler(HttpStatusCode statusCode, string body, string mediaType)
        {
            _statusCode = statusCode;
            _body = body;
            _mediaType = mediaType;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, _mediaType)
            });
        }
    }

    private sealed class ErrorBody
    {
        public int Code { get; set; }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
