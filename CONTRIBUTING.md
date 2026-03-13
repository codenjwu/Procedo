# Contributing to Procedo

Thanks for contributing to Procedo.

## Prerequisites

- .NET SDK that can build target frameworks used in this repo
- PowerShell (commands below assume PowerShell syntax)

## Build

```powershell
dotnet build Procedo.sln -m:1
```

Use `-m:1` in this environment to avoid occasional file-lock contention during parallel builds.

## Solution layout note

`Procedo.sln` includes some internal/non-packable projects such as:

- `Procedo.Core`
- `Procedo.DSL`
- `Procedo.Expressions`
- `Procedo.Observability`
- `Procedo.Persistence`
- `Procedo.Validation`

These are real internal projects in the working solution and they do participate in the normal solution build.

Reason:

- the repo now uses true internal project references instead of source-linking shared code into public package-owning projects
- this keeps Visual Studio/project navigation honest and lets internal assemblies build normally during local development

So:

- keep them visible for navigation and editing
- treat them as normal internal build units during local development
- keep public package verification tied to `scripts/pack-nuget.ps1`, which still controls the five-package public publish surface

## Test

```powershell
dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj -m:1
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj -m:1
dotnet test tests/Procedo.ContractTests/Procedo.ContractTests.csproj -m:1
```

## Project conventions

- Keep Procedo as an engine/runtime library set. No UI logic in engine components.
- Add new workflow behavior behind tests first (unit + integration as needed).
- Preserve observability contract compatibility (`ExecutionEvent` + contract tests).
- Prefer additive schema evolution (`SchemaVersion`, optional fields, compatibility tests).

## Pull request checklist

- Build succeeds locally.
- Relevant tests added/updated and passing.
- Docs updated for new flags, contracts, or behavior changes.
- Changelog updated for user-visible changes.
