---
title: "Built-in Steps: Data Formats"
description: Use built-in JSON, CSV, XML, and encoding helpers for structured data transformation inside workflows.
sidebar_position: 22
---

Procedo includes built-in steps for common structured data and encoding tasks.

Representative step types include:

- `system.json_get`
- `system.json_set`
- `system.json_merge`
- `system.csv_read`
- `system.csv_write`
- `system.xml_get`
- `system.xml_set`
- `system.base64_encode`
- `system.base64_decode`

## JSON Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/37_system_json_demo.yaml
```

The validated output includes:

```text
json-version=2
```

## CSV Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/38_system_csv_demo.yaml
```

The validated output includes:

```text
csv-rows=2
```

## XML Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/39_system_xml_demo.yaml
```

The validated output includes:

```text
xml-mode=prod
```

## Encoding Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/34_system_encoding_hash_demo.yaml
```

## When To Use These Steps

- transform small workflow-local payloads
- extract values for later conditions or reporting
- round-trip simple structured documents without leaving the workflow
- perform lightweight encoding or decoding before downstream use

## Related Content

- [Built-in Steps: Archive and Hash](./built-in-steps-archive-and-hash.md)
- [Expressions Functions](./expressions-functions.md)
