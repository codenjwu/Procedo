---
id: support-matrix
title: Support Matrix
sidebar_label: Support Matrix
description: Review the current target framework coverage, supported host assumptions, and intended public package surface for Procedo.
---

# Support matrix

This page summarizes the support position for Procedo as of the current Phase 1 release line.

## Library target frameworks

The main Procedo library packages multi-target:

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

That coverage applies to the primary public packages:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

## CLI host support

The reference CLI host, `Procedo.Runtime`, is the operator-facing host used throughout the examples and help-site snippets. The current repo documentation describes it as targeting:

- `net8.0`

For practical docs usage in this workspace, the CLI host is being exercised through the repo’s current SDK/toolchain and validated with the example suite.

## Contract coverage

The repo’s support docs call out compatibility coverage on:

- `net6.0`
- `net8.0`
- `net10.0`

That coverage is intended to protect the public contract surface and serialized runtime/event compatibility.

## Operating model assumptions

The current supported operating model is:

- single-node execution
- local file-backed persistence
- trusted-host deployment assumptions
- no distributed scheduling or worker coordination

Those assumptions matter as much as target framework support. A technically compatible target framework does not change the current runtime scope.

## Public package guidance

For most users, the recommended entry packages are:

- `Procedo.Hosting`
- `Procedo.Plugin.System`

Then add these only when needed:

- `Procedo.Engine` for lower-level execution control
- `Procedo.Plugin.SDK` for custom plugin authoring
- `Procedo.Extensions.DependencyInjection` for `IServiceCollection` integration

## Related content

- [Package Overview](/)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo)
- [Known Limitations](./known-limitations)
- [Phase 1 Release Notes](./phase-1-release-notes)
