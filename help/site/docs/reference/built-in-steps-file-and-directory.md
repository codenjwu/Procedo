---
title: "Built-in Steps: File and Directory"
description: Use built-in filesystem steps for reading, writing, moving, deleting, and organizing local files.
sidebar_position: 22
---

Procedo includes built-in filesystem steps for common local file operations.

Representative step types include:

- `system.file_write_text`
- `system.file_read_text`
- `system.file_copy`
- `system.file_move`
- `system.file_delete`
- `system.dir_create`
- `system.dir_list`
- `system.dir_delete`

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/32_system_file_ops_demo.yaml
```

This example demonstrates:

- creating a source file
- copying it
- reading its content
- moving it
- reporting the content
- deleting the final artifact

## Expected Behavior

The validated output includes:

```text
file-content=Hello from system.file_write_text
```

## Security Note

Filesystem steps are controlled by the system plugin security options. In the current security model, file access can be disabled entirely or constrained to allowed path roots.

## Related Content

- [Built-in Steps: Process and Security](./built-in-steps-process-and-security.md)
- [Validation](../run-and-operate/validation.md)
