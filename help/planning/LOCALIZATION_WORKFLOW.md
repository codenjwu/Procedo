# Localization Workflow

This guide defines the lightweight workflow for keeping the Procedo Help site aligned across:

- `en`
- `zh-Hans`

The goal is to make localization maintainable without turning every doc edit into a heavy process.

## Current Locale Layout

English source content lives in:

- `help/site/docs/`

Chinese localized content lives in:

- `help/site/i18n/zh-Hans/docusaurus-plugin-content-docs/current/`

UI translations live in:

- `help/site/i18n/zh-Hans/docusaurus-plugin-content-docs/current.json`
- `help/site/i18n/zh-Hans/docusaurus-theme-classic/navbar.json`
- `help/site/i18n/zh-Hans/docusaurus-theme-classic/footer.json`

## Source Of Truth Rules

- English docs are the authoring source of truth.
- Chinese docs are maintained locale copies, not independently drifting variants.
- Examples, commands, YAML, CLI flags, and code snippets should remain identical across locales unless localization requires explanation around them.
- Technical identifiers such as step types, field names, CLI options, and error codes should remain untranslated.

## When A Chinese Page Must Be Updated

Update the corresponding Chinese page when an English change affects:

- behavior
- commands
- CLI flags
- YAML shape
- code snippets
- step names or package names
- support or compatibility guidance
- release notes or known limitations

Minor wording-only edits in English do not always require immediate Chinese updates, but anything that changes user behavior or technical accuracy does.

## Change Categories

Use these categories when tracking localization impact:

- `none`
  English edit does not require Chinese action.
- `copy`
  English structure changed and the Chinese page should be refreshed from the new source before translating.
- `translate`
  Chinese wording needs an actual translation update.
- `verify`
  Commands or examples changed and should be rechecked in both locales.

## Recommended Update Flow

When editing English docs:

1. update the English page in `help/site/docs/`
2. identify the matching Chinese page under `help/site/i18n/zh-Hans/...`
3. update `help/content-map/TRANSLATION_STATUS.md`
4. apply the Chinese update if required
5. run the docs build
6. run snippet validation if commands or examples changed

## Adding A New Page

When adding a new English page:

1. add the English page under `help/site/docs/`
2. add it to the sidebar if needed
3. copy it into the matching `zh-Hans` locale folder
4. translate the title, description, headings, and prose
5. keep code blocks and technical identifiers aligned
6. mark the page status in `TRANSLATION_STATUS.md`

## Translation Quality Rules

- Translate explanations, not technical identifiers.
- Keep page structure aligned with English unless there is a strong readability reason not to.
- Prefer clear, product-style Simplified Chinese over literal word-for-word translation.
- Keep code blocks, commands, paths, and YAML examples technically unchanged unless the English source changed.
- Preserve links and make sure they still resolve in the locale build.

## Build And Verification

Build the full bilingual site from:

```powershell
cd help/site
npm run build
```

Validate snippets from the repository root when commands or examples changed:

```powershell
powershell -ExecutionPolicy Bypass -File help/tools/validate-snippets.ps1
```

## Practical Expectation

Not every English wording tweak needs immediate Chinese rewriting.

But these areas should stay closely synchronized:

- `Get Started`
- `Author Workflows`
- `Reference`
- `Run and Operate`
- `What's New`

Those sections are the highest-value user paths and should not drift materially.
