---
title: "Built-in Steps: Archive and Hash"
description: Use archive, hash, and encoding helpers for packaging workflow artifacts and generating deterministic digests.
sidebar_position: 24
---

Procedo includes built-in helpers for packaging artifacts and generating deterministic hashes.

Representative step types include:

- `system.hash`
- `system.zip_create`
- `system.zip_extract`
- `system.base64_encode`
- `system.base64_decode`

## Hash And Encoding Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/34_system_encoding_hash_demo.yaml
```

The validated output includes a SHA256 line shaped like:

```text
sha256=<hash>
```

## Archive Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/35_system_archive_demo.yaml
```

The validated output includes:

```text
archive-read=alpha
```

## When To Use These Steps

- package handoff artifacts
- verify file integrity
- prepare bundles for downstream transfer
- create digest outputs for release or audit workflows

## Related Content

- [Built-in Steps: File and Directory](./built-in-steps-file-and-directory.md)
- [Templates Overview](../templates/templates-overview.md)
