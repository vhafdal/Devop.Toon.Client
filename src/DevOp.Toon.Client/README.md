# DevOp.Toon.Client

`DevOp.Toon.Client` provides TOON-first `HttpClient` integration for .NET applications.

## Installation

```bash
dotnet add package DevOp.Toon.Client
```

## Features

- Registers a typed `HttpClient` with `AddToonClient(...)`
- Encodes requests with TOON by default
- Supports TOON and JSON response payloads
- Reuses `DevOp.Toon` for TOON runtime behavior

## Basic Usage

```csharp
using DevOp.Toon.Client;
using DevOp.Toon.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddToonClient(options =>
{
    options.BaseAddress = new Uri("https://api.somedomain.com/");
    options.ResponseEncodeOverrides = new ToonResponseEncodeOverrideOptions
    {
        Delimiter = ToonDelimiter.COMMA,
        KeyFolding = ToonKeyFolding.Off
    };
});
```

`EncodeOptions` applies to outbound TOON request bodies. `ResponseEncodeOverrides` sends `X-Toon-Option-*` headers so servers using `DevOp.Toon.API` can override response formatting per request.

## Package Notes

`DevOp.Toon.Client` depends on `DevOp.Toon`.
