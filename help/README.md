# Procedo Help

This folder is the side-project home for the user-facing Procedo help site.

It is intentionally separate from the repository's existing engineering docs in [`../docs`](../docs) so we can optimize this area for:

- product-style help content
- snippet-first writing
- tested examples
- long-term versioning and publishing
- a public documentation site experience

## Structure

- `site/`
  Future docs-site project and published content.
- `snippets/`
  Reusable snippet fragments when a page needs a focused excerpt rather than a full example file.
- `tests/snippet-tests/`
  Validation assets for snippet and example verification.
- `tools/`
  Scripts for snippet extraction, validation, and docs maintenance.
- `content-map/`
  Inventories that map pages to source material and snippet sources.
- `planning/`
  The current planning and authoring standards for the help site.

## Source Of Truth

- `../examples/` for runnable workflows and sample apps
- `../src/` for runtime and API behavior
- `../docs/` for background and engineering guidance
- `help/site/` for user-facing help content

## Authoring Principles

- Prefer tested snippets over hand-written snippets.
- Rewrite explanations for users; do not copy internal docs verbatim.
- Split large topics early.
- Keep reference pages precise and stable.
- Treat examples as part of the product experience.

## Current Validation Workflow

The first-pass snippet validation harness lives at:

- `help/tests/snippet-tests/commands.json`
- `help/tools/validate-snippets.ps1`

Run it from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File help/tools/validate-snippets.ps1
```

The validator runs commands sequentially because parallel `dotnet run` executions can contend on restore/build state in the same worktree.

## Local Development With Multiple Languages

Docusaurus development mode runs one locale at a time.

Use these commands from `help/site/`:

```powershell
npm run start:en
npm run start:zh-Hans
```

If you start the English dev server and then switch to Chinese in the locale dropdown, Docusaurus can show a local 404 because the dev server is still serving only the English locale process.

The reverse is also true:

- `npm run start:en` cannot switch to Chinese through the dropdown
- `npm run start:zh-Hans` cannot switch to English through the dropdown

For a full bilingual check at once, use:

```powershell
npm run preview
```

`npm run preview` builds both locales and serves the production-style output, so the language switcher works correctly in both directions.

## Localization Workflow

The bilingual documentation workflow is documented here:

- `planning/LOCALIZATION_WORKFLOW.md`
- `content-map/TRANSLATION_STATUS.md`

Use those files to:

- decide when English changes require Chinese updates
- track which sections are fully translated versus mirrored from English
- keep `en` and `zh-Hans` aligned as new pages are added

## Deployment Workflow

The recommended production deployment path is documented here:

- `planning/DEPLOYMENT_WORKFLOW.md`

The repository also includes an Azure Static Web Apps GitHub Actions workflow:

- `../.github/workflows/azure-static-web-apps-help.yml`

That workflow:

- validates snippets before deployment
- builds the Docusaurus site
- deploys the prebuilt static output
- supports pull request preview environments
