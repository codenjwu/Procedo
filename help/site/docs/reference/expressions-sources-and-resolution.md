---
title: Expression Sources and Resolution
description: Learn how Procedo resolves parameter, variable, and step-output references in expressions.
sidebar_position: 2
---

Procedo resolves expressions against a runtime variable map.

Current examples and resolver behavior support these common reference patterns:

- `params.<name>`
- `vars.<name>`
- `steps.<stepId>.outputs.<name>`

## Examples

```yaml
message: "${params.environment}"
message: "${vars.release_label}"
message: "${steps.producer.outputs.message}"
```

## Resolution Behavior

The current resolver behavior includes:

- direct variable lookup
- `vars.` lookup with both prefixed and short-key support
- `params.` lookup with both prefixed and short-key support
- `steps.<stepId>.outputs.<name>` lookup for step outputs

## What Happens If Resolution Fails

If a token cannot be resolved, the expression resolver raises an expression-resolution error.

## Related Content

- [Expression Functions](./expressions-functions.md)
- [Outputs](../author-workflows/outputs.md)
