---
title: Install and Setup
description: Prepare your environment so you can run Procedo examples locally.
sidebar_position: 1
---

Use this page to get your machine ready for the first Procedo workflow run.

## Prerequisites

- .NET SDK installed locally
- a clone of the Procedo repository
- a terminal opened at the repository root

## SDK Requirement

The repository currently pins the .NET SDK through `global.json`:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

That means the safest setup is a local .NET 10 SDK compatible with that requirement.

## Framework Coverage

The shared build configuration currently targets these framework versions for library projects:

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

## Verify The SDK

```powershell
dotnet --info
```

## Build The Solution

```powershell
dotnet build Procedo.sln
```

## What A Successful Setup Gives You

After the build completes, you should be able to:

- run YAML workflows with `src/Procedo.Runtime`
- execute example projects under `examples/`
- validate the help-site snippets against the current repo state

## First Command To Try

Once the solution builds, run:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

## Good Next Steps

- [Run Your First Workflow](./run-your-first-workflow.md)
- [Procedo CLI Basics](./procedo-cli-basics.md)
