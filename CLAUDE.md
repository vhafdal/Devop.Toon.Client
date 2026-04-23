# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

`DevOp.Toon.Client` is a typed `HttpClient` wrapper for TOON-first APIs. It encodes request bodies as TOON via `IToonService` and deserializes responses as TOON or falls back to JSON based on the `Content-Type` header. Targets `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`.

## Common Commands

```bash
dotnet restore
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~ToonClientTests"
dotnet pack src/DevOp.Toon.Client/DevOp.Toon.Client.csproj -c Release -o nupkg
dotnet format
```

## Local Cross-Repo Development

For Debug builds, substitute the NuGet `DevOp.Toon` and `DevOp.Toon.Core` packages with local project references:

| Environment variable      | Points to                                             |
|---------------------------|-------------------------------------------------------|
| `DEVOP_TOON_CSPROJ`       | local `DevOp.Toon/src/DevOp.Toon/DevOp.Toon.csproj`  |
| `DEVOP_TOON_CORE_CSPROJ`  | local `DevOp.Toon.Core/DevOp.Toon.Core.csproj`        |

```bash
export DEVOP_TOON_CSPROJ=/path/to/DevOp.Toon/src/DevOp.Toon/DevOp.Toon.csproj
dotnet build -c Debug
```

Or inline:
```bash
dotnet build -c Debug -p:ToonProjectPath=/path/to/DevOp.Toon.csproj
```

Release builds always use NuGet — never use local overrides for release or CI.

## Architecture

### Public surface

| Type | Role |
|------|------|
| `IToonClient` | Interface — GET, POST, PUT, PATCH, DELETE with typed responses |
| `ToonClient` | Sealed implementation. Encodes requests as TOON; decodes responses as TOON or JSON by `Content-Type` |
| `ToonClientOptions` | Per-client config: `BaseAddress`, `Timeout`, `EncodeOptions`, `DecodeOptions`, `ResponseEncodeOverrides`, `JsonSerializerOptions`, `ToonMediaType` |
| `ToonResponseEncodeOverrideOptions` | Nullable fields serialized as `X-Toon-Option-<PropertyName>` request headers to influence server-side encoding |
| `ToonClientServiceCollectionExtensions` | `AddToonClient(...)` — registers `IToonClient` as a typed `HttpClient` and auto-registers `IToonService` if absent |
| `ToonClientException` | Thrown on deserialization failures or unsupported response `Content-Type` |

### Request flow

`ToonClient` sets `Accept` headers in preference order: `application/toon` → `text/toon` (0.9) → `application/json` (0.8). On response, it branches by `Content-Type`: TOON types go through `IToonService.Decode<T>`; if that throws `NotSupportedException` it falls back to `Toon2Json` + `JsonSerializer`. JSON types go directly to `JsonSerializer`. Any other media type throws `ToonClientException`.

`ResponseEncodeOverrides` are applied by `ApplyResponseEncodeOverrideHeaders` — only non-null fields emit headers, so partial override is safe. `ToonClientOptions` is deep-cloned at registration time; the singleton copy is immutable at runtime.

### Assembly signing

The project signs with `DevOp.snk`. Do not replace or rotate the key file.

## Coding Style

File-scoped namespaces, 4-space indentation, `#nullable enable`, `PascalCase` public / `camelCase` private fields (no underscore prefix), async methods end in `Async`, XML docs on all public APIs.

## Testing

Framework: xUnit, single target `net8.0`. Tests live in `tests/DevOp.Toon.Client.Tests/`. Cover both TOON and JSON response paths, `X-Toon-Option-*` header emission, DI registration, and option validation.

## Git Commit Workflow

When committing changes that affect consumer behavior, update `<PackageReleaseNotes>` in `DevOp.Toon.Client.csproj` before staging. Format:

```
Short summary sentence.
- Adds ...
- Fixes ...
- Improves ...
- Breaking: ...
```

Only update release notes for externally visible changes (new features, bug fixes, behavior or compatibility changes). Skip for internal refactoring, test-only, or formatting changes.

## Documentation

Update `README.md` and `Documentation/` when public APIs or setup steps change. Do not update Confluence. `Documentation/DocMost/` is legacy reference material — only use it when explicitly requested.
