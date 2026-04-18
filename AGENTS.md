# Repository Guidelines

## Project Structure & Module Organization
This repository contains a single .NET client library solution, [`DevOp.Toon.Client.slnx`](/home/valdi/Projects/DevOp.Toon.Client/DevOp.Toon.Client.slnx). Source code lives under [`src/DevOp.Toon.Client`](/home/valdi/Projects/DevOp.Toon.Client/src/DevOp.Toon.Client), with the main public surface split across `IToonClient`, `ToonClient`, `ToonClientOptions`, and `ToonClientServiceCollectionExtensions`. Package metadata and multi-targeting are defined in [`src/DevOp.Toon.Client/DevOp.Toon.Client.csproj`](/home/valdi/Projects/DevOp.Toon.Client/src/DevOp.Toon.Client/DevOp.Toon.Client.csproj). There is no `tests/` directory yet.

## Build, Test, and Development Commands
Use the solution file from the repository root:

```bash
dotnet restore DevOp.Toon.Client.slnx
dotnet build DevOp.Toon.Client.slnx
dotnet pack src/DevOp.Toon.Client/DevOp.Toon.Client.csproj -c Release
```

`dotnet build` is the main validation step and currently succeeds across `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0`. Use `dotnet pack` when validating package metadata, README packaging, and signed release output.

## Coding Style & Naming Conventions
Follow the established C# style in `src/`: file-scoped namespaces, 4-space indentation, nullable reference types enabled, and explicit guard clauses for invalid arguments. Public types use `PascalCase`; private fields use `camelCase` without underscores; asynchronous methods end in `Async`. Keep XML documentation on public APIs and prefer small, focused methods like the existing request helpers in `ToonClient.cs`.

## Testing Guidelines
There is no automated test project yet, so changes should at minimum pass `dotnet build`. When adding tests, create a sibling project under `tests/` such as `tests/DevOp.Toon.Client.Tests`, mirror the source namespace, and name test files after the unit under test, for example `ToonClientTests.cs`. Cover both TOON and JSON response paths, option validation, and DI registration behavior.

## Documentation Process
Treat documentation as part of the change, not follow-up work. Update repository-facing docs in `README.md`, `src/DevOp.Toon.Client/README.md`, and `Documentation/` or `Documentation/DocMost/` when package usage, public APIs, or setup steps change. Do not update Confluence as part of the workflow. If the DocMost site should reflect the change, call out the specific pages that need manual syncing. Use the GitHub repository <https://github.com/vhafdal/DevOp.Toon.Client> as the canonical code reference.

## Commit & Pull Request Guidelines
Git history is minimal and currently uses short imperative subjects, for example `Initial commit`. Keep commit messages concise and descriptive, such as `Add JSON fallback error handling`. Pull requests should explain the API or behavior change, list validation steps run locally, and note any package, target-framework, or serialization compatibility impact.
