# Support Matrix

This document captures the current support matrix for Procedo Phase 1 packages and tools.

## Library target frameworks

Procedo library projects target:

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

These targets apply to the main library packages, including:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

## Runtime CLI host

The reference CLI host [Procedo.Runtime](/D:/Project/codenjwu/Procedo/src/Procedo.Runtime) targets:

- `net8.0`

This is the supported local operator host for examples, persistence, inspection, and resume flows.

## Contract coverage

Cross-target compatibility tests currently cover:

- `net6.0`
- `net8.0`
- `net10.0`

These tests validate public surface stability and serialized contract compatibility.

## Operating model

Phase 1 support assumptions:

- single-node execution only
- local file-backed persistence
- trusted-host deployment model
- no distributed scheduling or worker coordination

## Public package guidance

Recommended public entry packages:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

Implementation-detail projects still exist in the repository, but they are not part of the intended public NuGet surface.

For most embedders, `Procedo.Hosting` is the best starting point.
