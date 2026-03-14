# Deployment Workflow

This document describes the recommended deployment path for the Procedo Help site.

## Recommended Host

Use Azure Static Web Apps for the published help site.

Why this is the best fit for the current help project:

- static hosting works well for Docusaurus output
- GitHub pull requests can get preview environments
- the docs build can be gated by snippet validation
- custom domains can be added later without changing the docs project structure

## Deployment Model

The repository now includes a GitHub Actions workflow at:

- `.github/workflows/azure-static-web-apps-help.yml`

That workflow does four things:

1. installs the repo toolchain
2. validates the documented snippets
3. builds the Docusaurus site
4. deploys the built output to Azure Static Web Apps

## Trigger Scope

The workflow triggers when relevant files change, including:

- `help/**`
- `examples/**`
- `src/**`
- `docs/**`
- `README.md`
- `global.json`

This is intentional. The help site depends on runtime behavior and examples, so docs deployment should react to more than just markdown changes.

## Required Azure Setup

To activate the workflow, create an Azure Static Web App and add the deployment token to the GitHub repository secrets as:

- `AZURE_STATIC_WEB_APPS_API_TOKEN`

## Recommended Repository Secret

Use this exact secret name:

```text
AZURE_STATIC_WEB_APPS_API_TOKEN
```

The workflow is already written against that name.

## Build Strategy

The GitHub Actions workflow builds the docs site before deployment and then deploys the prebuilt static output.

That means the Azure Static Web Apps deploy action is configured with:

- `app_location: help/site/build`
- `skip_app_build: true`

This is deliberate because the workflow validates snippets before deployment, and we want the deployed artifact to be exactly the one produced after validation.

## Pull Request Behavior

For pull requests against `main`:

- the workflow builds and deploys a preview environment
- when the pull request is closed, the workflow closes the preview environment

## Local Verification Before Pushing

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File help/tools/validate-snippets.ps1
```

Then from `help/site`:

```powershell
npm run build
```

If both pass locally, the deployment workflow should be in a good state for CI.

## Future Improvements

Reasonable later improvements:

- add a docs-only status badge to the README
- surface the preview URL in pull request comments
- add branch protection requiring the docs deployment workflow to pass
- add a production custom domain such as `docs.procedo.dev`
