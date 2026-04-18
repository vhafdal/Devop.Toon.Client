# Build Configuration

This repository supports two ways of resolving `DevOp.Toon`:

- `Debug` builds can use a local `DevOp.Toon` project reference
- `Release` builds always use the published NuGet package

This lets each developer point to their own local checkout without hardcoding machine-specific paths into the repo.

## Default Behavior

By default, `DevOp.Toon.Client` resolves `DevOp.Toon` from NuGet:

- package: `DevOp.Toon`
- version: `0.2.0`

This is always the behavior for `Release` builds.

## Debug Local Override

For `Debug` builds, the project can switch to a local `DevOp.Toon` project reference if either of these is provided:

- MSBuild property: `ToonProjectPath`
- environment variable: `DEVOP_TOON_CSPROJ`

The value must point to the local `DevOp.Toon.csproj` file.

Example:

```bash
export DEVOP_TOON_CSPROJ=/home/valdi/Projects/DevOp.Toon/src/DevOp.Toon/DevOp.Toon.csproj
dotnet build -c Debug
```

Or per command:

```bash
dotnet build -c Debug -p:ToonProjectPath=/home/valdi/Projects/DevOp.Toon/src/DevOp.Toon/DevOp.Toon.csproj
```

## Resolution Rules

The build resolves `DevOp.Toon` in this order:

1. If `ToonProjectPath` is set, use that value
2. Otherwise, if `DEVOP_TOON_CSPROJ` is set, use that value
3. If the path exists and the build configuration is `Debug`, use a `ProjectReference`
4. Otherwise, use the NuGet package reference

## Practical Guidance

- Use the environment variable if you regularly work on both repos locally
- Use the MSBuild property for one-off builds
- Do not rely on the local override for `Release` packaging or CI
- Keep `Release` builds on NuGet so package outputs stay reproducible
