# Procedo Help Authoring Charter

Procedo Help is the user-facing source of truth for learning, using, and operating Procedo.

It should read like product documentation rather than internal engineering notes.

## Goals

- Help new users succeed quickly with minimal working examples.
- Give experienced users precise reference pages for YAML, CLI, runtime behavior, and extension points.
- Make runnable code snippets a core feature of the documentation.
- Keep help content aligned with Procedo releases and feature changes.

## Audience

- Users running Procedo workflows from YAML
- Engineers embedding Procedo into .NET applications
- Plugin authors extending Procedo
- Operators diagnosing validation, runtime, persistence, and observability behavior

## Content Types

### Concept

Explains what something is, why it exists, and when to use it.

### How-to

Shows how to accomplish a task step by step.

### Reference

Defines exact syntax, options, behavior, contracts, and limits.

### Recipe

Provides a complete tested example for a practical scenario.

## Editorial Rules

- Every important feature page should include at least one runnable example.
- Snippets should come from tested examples whenever possible.
- Large topics should be split before they become hard to scan.
- Existing repository docs may be used as reference material, but public help content should be rewritten with more detail and clearer user guidance.
- Tutorials and recipes should stay outcome-focused.
- Reference pages should stay precise, stable, and easy to search.

## Source Of Truth

- `examples/` for runnable workflows and sample apps
- `src/` for actual behavior and API truth
- `docs/` for engineering background only
- `help/site/` for curated user-facing help content

## Quality Bar

A page is ready only if:

- the explanation is clear enough for a new user
- examples are minimal but real
- snippets are verified or traceable to tested sample files
- related pages are linked
- limitations and common mistakes are called out where needed

## Maintenance Rules

- New features should update help pages in the same change or immediately after.
- Changes to YAML shape, CLI behavior, built-in steps, runtime states, or package APIs should trigger a docs review.
- Release notes should include a docs impact check.
- Broken snippet tests should block docs publication.

## Hosting Direction

The help site is planned for static hosting, with Azure Static Web Apps as the preferred target because it supports preview environments and fits a docs workflow gated by snippet validation.
