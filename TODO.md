# TODO

## Migrate ToonClient to use IToonService.Deserialize<T>

**Goal**: Remove direct `System.Text.Json` dependency from `DevOp.Toon.Client` by routing all
response deserialization through `IToonService.Deserialize<T>(string body, string contentType)`.

`DevOp.Toon` exposes this method as of `DevOp.Toon` v0.2.4+ (added `Deserialize<T>` on
`IToonService`/`ToonService`). The client currently calls `JsonSerializer.Deserialize` directly
in two places.

### What changes

**`src/DevOp.Toon.Client/ToonClient.cs`**

Replace the `SendAsync<T>` deserialization block:

```csharp
// Before
if (IsToonMediaType(contentType))
    result = DecodeToon<T>(body);
else if (IsJsonMediaType(contentType))
    result = JsonSerializer.Deserialize<T>(body, options.JsonSerializerOptions);
```

```csharp
// After
result = toon.Deserialize<T>(body, contentType);
```

Also simplify `DecodeToon<T>` (or remove it) — its JSON fallback for `NotSupportedException`
will need to move into `IToonService` or be handled by the caller catching `NotSupportedException`
and retrying via `Toon2Json` + STJ.

Replace `CreateDecoder` — the error-body decoder used by `ToonClientException.Decode<T>()` —
to call `toon.Deserialize<T>(content, contentType)` via reflection instead of calling STJ
directly. Signature: `Func<Type, object?>`.

**`src/DevOp.Toon.Client/ToonClientOptions.cs`**

Remove `JsonSerializerOptions` property (used only for STJ fallback paths).
Update `Clone()` accordingly.

**`src/DevOp.Toon.Client/DevOp.Toon.Client.csproj`**

Remove the explicit `<PackageReference>` for `System.Text.Json` once STJ is no longer called
directly. (The transitive reference from `DevOp.Toon` is sufficient if any STJ types appear
in the public surface.)

**`tests/DevOp.Toon.Client.Tests/ToonClientTests.cs`**

Remove `JsonSerializerOptions`-specific assertions; add/update tests that exercise the
`Deserialize<T>` path end-to-end.

### Known limitation

`IToonService.Deserialize<T>` converts JSON to TOON first (via `Json2Toon`), then decodes.
Types that are not natively supported by the TOON decoder will throw `NotSupportedException`
from the JSON path, even though they worked before (STJ handled them directly). A fallback
`Toon2Json` + STJ path is needed for those types — this must be coordinated with the
`DevOp.Toon` team or handled in the client with a try/catch + STJ fallback retained for
the unsupported-type case only.

### Minimum version dependency

Bump the `DevOp.Toon` NuGet reference from `>= 0.2.3` to `>= 0.2.4` after that version is
published with `Deserialize<T>` included.
