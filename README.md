# DevOp.Toon.Client

`DevOp.Toon.Client` is the TOON-first HTTP client package for .NET applications.

It wraps `HttpClient`, uses `DevOp.Toon` for TOON serialization, and falls back to JSON when a server responds with JSON instead of TOON.

## Installation

```bash
dotnet add package DevOp.Toon.Client
```

## Features

- Typed `HttpClient` wrapper for TOON-first APIs
- TOON request encoding through `IToonService`
- TOON and JSON response handling
- DI registration with `AddToonClient(...)`
- Shared configuration for base address, timeout, serializer options, request TOON options, and response-format override headers

## Registration

```csharp
using DevOp.Toon.Client;
using DevOp.Toon.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddToonClient(options =>
{
    options.BaseAddress = new Uri("https://api.somedomain.com/");
    options.Timeout = TimeSpan.FromSeconds(30);
    options.ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
    {
        ObjectArrayLayout = ToonObjectArrayLayout.Columnar,
        KeyFolding = ToonKeyFolding.Off,
        IgnoreNullOrEmpty = true
    };
});
```

`EncodeOptions` controls how this client serializes outbound TOON request bodies and defaults `ByteArrayFormat` to `ToonByteArrayFormat.Base64String`. `ResponseEncodeOverrides` controls which `X-Toon-Option-*` headers are sent so a TOON-aware server can format the response differently for this client.

## Usage

```csharp
using DevOp.Toon.Client;
using DevOp.Toon.Core;

public sealed class CatalogSync
{
    private readonly IToonClient client;

    public CatalogSync(IToonClient client)
    {
        this.client = client;
    }

    public Task<List<Product>?> GetProductsAsync(CancellationToken cancellationToken)
    {
        return client.GetAsync<List<Product>>("api/products", cancellationToken);
    }
}
```

## Package Notes

`DevOp.Toon.Client` depends on `DevOp.Toon` and is intended for outbound HTTP integration. Use `DevOp.Toon` directly when you only need TOON encoding and decoding without `HttpClient`.
