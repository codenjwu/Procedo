---
title: Built-in Steps Overview
description: Use the built-in system step catalog for common workflow tasks without writing custom plugins.
sidebar_position: 20
---

Procedo ships with a built-in `system.*` step catalog that covers common workflow tasks.

Current registrations in the system plugin include step types for:

- echo and utility values
- waiting and resume behavior
- HTTP
- file and directory operations
- hashing and archive helpers
- JSON, CSV, and XML helpers
- process execution

## Registered Built-in Step Types

The current system plugin registers step types such as:

- `system.echo`
- `system.guid`
- `system.now`
- `system.concat`
- `system.sleep`
- `system.wait_signal`
- `system.wait_until`
- `system.wait_file`
- `system.http`
- `system.file_write_text`
- `system.file_read_text`
- `system.file_copy`
- `system.file_move`
- `system.file_delete`
- `system.base64_encode`
- `system.base64_decode`
- `system.hash`
- `system.zip_create`
- `system.zip_extract`
- `system.dir_create`
- `system.dir_list`
- `system.dir_delete`
- `system.json_get`
- `system.json_set`
- `system.json_merge`
- `system.process_run`
- `system.csv_read`
- `system.csv_write`
- `system.xml_get`
- `system.xml_set`

## How To Read This Section

- use `Core Utilities` for basic workflow helper steps
- use `File and Directory` for filesystem-related tasks
- use `Process and Security` when invoking external executables
- use `Wait and Resume` for operator or signal-driven workflows

## Related Content

- [Steps](../author-workflows/steps.md)
- [Templates Overview](../templates/templates-overview.md)
