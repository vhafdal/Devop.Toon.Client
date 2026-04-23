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
Treat documentation as part of the change, not follow-up work. Update repository-facing docs in `README.md`, `src/DevOp.Toon.Client/README.md`, and relevant `Documentation/` files when package usage, public APIs, or setup steps change. Do not update Confluence as part of the workflow. Do not assume `Documentation/DocMost/` is current; it is legacy reference material unless the user explicitly asks to use it. Use the GitHub repository <https://github.com/vhafdal/DevOp.Toon.Client> as the canonical code reference.

## Git Commit Workflow (with Package Release Notes)

When performing `git commit`, the agent must follow a **structured workflow** that ensures:
- accurate commit messages
- consistent `PackageReleaseNotes`
- alignment between code changes and published packages

---

## 1. Workflow Overview

When the user runs `git commit`, the agent must:

1. Inspect staged changes (`git diff --staged`)
2. Identify intent and impact of the change
3. Detect affected **packable projects** (`.csproj` with `PackageId`)
4. Update `PackageReleaseNotes` where relevant
5. Stage updated `.csproj` files
6. Generate a high-quality commit message
7. Perform the commit

If no files are staged:
- Do not guess
- Respond that there are no staged changes

---

## 2. Package Detection

A project is considered **packable** if:
- `.csproj` contains `<PackageId>`
- OR it is clearly part of a NuGet-distributed package

Only update release notes for:
- projects affected by the staged changes

---

## 3. When to Update `PackageReleaseNotes`

Update `PackageReleaseNotes` **only if the change impacts consumers**, including:

- New features or capabilities
- Bug fixes
- Behavior changes
- Performance improvements
- Serialization / protocol changes (important for DevOp.Toon)
- Compatibility changes (.NET targets, dependencies)
- Documentation that affects usage

Do **NOT** update if:
- purely internal refactoring with no external impact
- formatting, renaming, or non-functional changes
- test-only changes

---

## 4. Release Notes Format

`PackageReleaseNotes` must follow this structure:

- First line: **short summary sentence**
- Followed by **3–6 bullet points**
- Each bullet starts with a **strong verb**

### Allowed prefixes

- `Adds` – new functionality
- `Improves` – enhancements or performance
- `Fixes` – bug fixes
- `Breaking` – breaking changes (must be explicit)
- `Updates` – meaningful dependency/platform updates only

### Style rules

- Focus on **consumer impact**
- Be **specific** (no vague phrases like `updates` or `various fixes`)
- Use **present tense**
- Keep concise (max ~5–10 lines)
- Plain text only (NuGet-friendly)
- No marketing language or fluff

---

## 5. Example

```xml
<PackageReleaseNotes>
  Improves TOON protocol handling and serialization consistency.
  - Adds support for updated TOON 3.0 structures
  - Improves token efficiency and output formatting
  - Fixes edge cases in nested object serialization
  - Improves compatibility across .NET target frameworks
</PackageReleaseNotes>
```
