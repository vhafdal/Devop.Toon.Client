# DevOp.Toon.Client

A typed `HttpClient` integration for TOON request bodies and TOON/JSON responses. Targets .NET Standard 2.0, .NET 8, .NET 9, and .NET 10.

**Version scope:** this guide covers Client 0.2.0 with runtime 0.3.0 and Core 0.2.0.

```bash
dotnet add package DevOp.Toon.Client
```

```csharp
using DevOp.Toon;
using DevOp.Toon.Client;
using DevOp.Toon.Core;

builder.Services.AddToonClient(options =>
{
    options.BaseAddress = new Uri("https://api.example.com/");
    options.Timeout = TimeSpan.FromSeconds(30);
    options.EncodeOptions = new ToonEncodeOptions
    {
        ProtocolVersion = ToonProtocolVersion.V4_4
    };
    options.ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
    {
        ProtocolVersion = ToonProtocolVersion.CustomV4_4,
        Indent = 2,
        IgnoreNullOrEmpty = false,
        ExcludeEmptyArrays = false
    };
});
```

Inject `IToonClient` and pass cancellation through each operation:

```csharp
public sealed class CatalogSync(IToonClient client)
{
    public Task<List<Product>?> GetProductsAsync(CancellationToken cancellationToken)
        => client.GetAsync<List<Product>>("api/products", cancellationToken);
}
```

`Product` is the application's response model.

## Request and response versions

- `EncodeOptions.ProtocolVersion` selects the outbound request body format. The client sends its concrete version in `X-Toon-Version`.
- `ResponseEncodeOverrides.ProtocolVersion` sends `X-Toon-Option-ProtocolVersion`, asking a compatible server to select the response format. It does not change the outbound body.
- A TOON response's `X-Toon-Version` supplies the decode hint. Invalid, unsupported, multiple, or conflicting response hints produce `ToonClientException`. An explicit `DecodeOptions.ProtocolVersion` must agree with the header.
- Without a response header, decoding uses the configured options, defaulting to `Auto`. Ambiguous version-dependent data requires a concrete hint rather than a parse retry. Configure `DecodeOptions` for a known headerless legacy endpoint.

Supported concrete versions are `V3_0`, `V4_0` through `V4_4`, `CustomLegacy`, and `CustomV4_4`. `Auto` is for decoding only. The transport names are `3.0`, `4.0` through `4.4`, `custom-legacy`, and `custom-4.4`.

## Defaults and migration

Requests now default to official 4.4, one-space indentation, comma delimiters, key folding off, and both omission options false. Byte sequences default to `Base64String`. Select `CustomLegacy` for the previous custom wire profile, or `CustomV4_4` for nested header groups plus row/keyed spills. Both custom profiles default `IgnoreNullOrEmpty` and `ExcludeEmptyArrays` to true; explicitly set both false for data-preserving output. Custom formats require compatible readers.

`ResponseEncodeOverrides.Indent` is now `int?` (a breaking change from `bool?`): set an integer width such as `2`, or leave it null to send no indentation override. DevOp.Toon.API accepts HTTP indentation widths from **1 through 16**; this input limit is separate from trusted direct runtime configuration. The API defaults to two spaces when no override is supplied.

`ResponseEncodeOverrides` controls only the headers sent to a compatible API. Other options cover response formatting, not local decoding. JSON responses use `JsonSerializerOptions` (web defaults). Automatic decompression is enabled by default for gzip/deflate, with Brotli on supported modern targets; set `EnableCompression` false to disable it.

Source: [DevOp.Toon.Client repository](https://github.com/vhafdal/DevOp.Toon.Client). Use `DevOp.Toon` directly for serialization without HTTP.
