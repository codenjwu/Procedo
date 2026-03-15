# Page Template Standard

Every page in Procedo Help should follow a predictable structure so users can scan quickly and know where to find examples, requirements, and edge cases.

## Required Frontmatter

Each docs page should eventually define:

- `title`
- `description`
- `sidebar_position`
- `slug`
- `tags` when helpful

## Standard Page Shape

### 1. Summary

One short paragraph explaining what the page helps the user do.

### 2. When To Use This

Optional for small reference pages, but recommended for concept and how-to pages.

### 3. Prerequisites

Only include prerequisites that truly matter for the task on the page.

### 4. Minimal Working Example

The smallest snippet that proves the feature or task works.

Rules:

- Prefer a complete runnable snippet over a fragment.
- If a fragment is used, link to the full runnable example.
- Clearly label the example intent, such as `Minimal`, `Common`, or `Advanced`.

### 5. Explanation

Explain how the example works in plain language.

Focus on:

- what each important field or call does
- what the runtime behavior is
- what the user should expect to see

### 6. Additional Examples

Use for:

- advanced scenarios
- variations
- failure cases
- production-oriented patterns

### 7. Common Mistakes Or Limits

Call out:

- common validation failures
- unsupported patterns
- security considerations
- behavior that often surprises users

### 8. Related Content

Link to the next natural pages.

## Snippet Rules

- Prefer snippets sourced from `examples/`.
- Keep hand-written snippets to a minimum.
- Do not include untested snippets in published pages.
- Favor short snippets in the page body and link to the full sample when the full file is long.
- If the page describes behavior that changes based on parameters, include at least one example command invocation.

## Writing Style

- Be direct and practical.
- Favor concrete examples over abstract claims.
- Explain behavior, not just syntax.
- Avoid repeating internal repository phrasing unless it is already the clearest wording.
- Assume the user wants to succeed quickly and safely.

## Split Rules

Split a page when any of the following becomes true:

- the page tries to cover more than one major task
- the number of examples becomes hard to scan
- the page mixes tutorial and reference goals
- the page would need a long table plus many examples plus troubleshooting notes

When in doubt, prefer smaller focused pages.
