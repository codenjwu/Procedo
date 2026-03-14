# Translation Status

This file tracks the current localization status of the Procedo Help site.

Status values:

- `translated`
  Chinese page is intentionally localized and reviewed.
- `mirrored`
  Chinese locale file exists but is still effectively the English source copy.
- `needs-review`
  Chinese page exists but should be revisited after recent English changes.
- `missing`
  Chinese locale file has not been created yet.

## UI Translation Files

| Area | Status | Notes |
| --- | --- | --- |
| Navbar | translated | `help/site/i18n/zh-Hans/docusaurus-theme-classic/navbar.json` |
| Footer | translated | `help/site/i18n/zh-Hans/docusaurus-theme-classic/footer.json` |
| Sidebar labels | translated | `help/site/i18n/zh-Hans/docusaurus-plugin-content-docs/current.json` |

## Overview

| Page | Status |
| --- | --- |
| Introduction | translated |
| Why Procedo | translated |
| Core Concepts | translated |

## Get Started

| Page | Status |
| --- | --- |
| Install and Setup | translated |
| Run Your First Workflow | translated |
| Create Your First Workflow | translated |
| Procedo CLI Basics | translated |

## Author Workflows

| Page | Status |
| --- | --- |
| Workflow Structure Overview | translated |
| Steps | translated |
| Parameters | translated |
| Variables | translated |
| Outputs | translated |
| Expressions Overview | translated |
| Conditions | translated |

## Run and Operate

| Page | Status |
| --- | --- |
| Persistence | translated |
| Observability | translated |
| Validation | translated |

## Templates

| Page | Status |
| --- | --- |
| Templates Overview | translated |
| Template Parameters | translated |
| Template Conditions | translated |
| Template Loops | translated |
| Template Limitations | translated |

## Extend Procedo

| Page | Status |
| --- | --- |
| Plugin Authoring Overview | translated |
| Create a Custom Step | translated |
| Method Binding | translated |
| Dependency Injection Integration | translated |

## Use in .NET

| Page | Status |
| --- | --- |
| Embedding Procedo | translated |
| ProcedoHostBuilder | translated |
| Execute YAML from Code | translated |
| Custom Runtime Composition | translated |

## Reference

| Page | Status |
| --- | --- |
| CLI Overview | translated |
| YAML Workflow Schema Overview | translated |
| YAML `name` and `version` | translated |
| YAML `parameters` | translated |
| YAML `stages`, `jobs`, and `steps` | translated |
| YAML `with` and `depends_on` | translated |
| YAML `condition` | translated |
| Expressions Reference Overview | translated |
| Expression Sources and Resolution | translated |
| Expression Functions | translated |
| Expression Condition Rules | translated |
| Built-in Steps Overview | translated |
| Built-in Steps: Core Utilities | translated |
| Built-in Steps: Data Formats | translated |
| Built-in Steps: HTTP | translated |
| Built-in Steps: Archive and Hash | translated |
| Built-in Steps: File and Directory | translated |
| Built-in Steps: Process and Security | translated |
| Built-in Steps: Wait and Resume | translated |
| Built-in Steps: Secure Runtime | translated |
| Runtime Statuses | translated |
| Runtime Persistence State | translated |
| Runtime Error Codes | translated |

## Recipes

| Page | Status |
| --- | --- |
| Minimal Pipeline | translated |
| Passing Data Between Steps | translated |
| Conditional Execution | translated |
| Dependencies and Execution Order | translated |

## Troubleshooting

| Page | Status |
| --- | --- |
| Common Validation Errors | translated |
| FAQ | translated |

## What's New

| Page | Status |
| --- | --- |
| Release Notes Index | translated |
| Phase 1 Release Notes | translated |
| Known Limitations | translated |
| Support Matrix | translated |
| Roadmap | translated |

## Maintenance Notes

- After any substantial English docs change, update this file if page status changes to `needs-review`.
- When adding a new page, add it here immediately even if the Chinese page is still `missing`.
- If a page is temporarily copied from English into the Chinese locale but not yet localized, mark it as `mirrored`.
