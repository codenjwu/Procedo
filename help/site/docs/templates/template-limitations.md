---
title: Template Limitations
description: Understand the intentional boundaries of the current Procedo template model.
sidebar_position: 5
---

Procedo templates are intentionally constrained in the current phase.

This keeps template behavior predictable, but it also means templates are not a full inheritance or fragment-composition system.

## Current Limits

The current implementation does not support:

- arbitrary stage, job, or step merging
- template fragment imports
- multiple template inheritance
- object or dictionary iteration for `${{ each }}`
- full source mapping for hypothetical future graph-composition features

## Child Workflow Restrictions

When using a base template, child workflows may not define:

- new stages
- new jobs
- new steps
- new parameter schema definitions

If a child workflow tries to add these, template loading fails.

## Practical Guidance

Templates are a good fit for:

- stable build/package/publish flows
- standardized deployment shapes
- workflows that mostly differ by values

Templates are not the best fit yet for:

- highly dynamic graph composition
- workflows that need heavy structural injection
- multi-template assembly patterns

## Related Content

- [Templates Overview](./templates-overview.md)
- [Template Parameters](./template-parameters.md)
