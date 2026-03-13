# Troubleshooting

## Runtime says workflow file not found

- CLI now reports this as `PR203`.
- Ensure positional workflow path is correct.
- If using config file, verify `workflowPath` in `procedo.runtime.json`.

## Validation fails before execution

- CLI now reports the summary failure as `PR201`.
- Re-run with strict mode details:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --strict-validation
```

- Fix reported `PVxxx` issue codes in YAML.

## Run fails with `PR101` (plugin not found)

- Ensure plugin registration exists in runtime startup.
- Verify step `type` matches registered plugin key exactly.

## Run fails with timeout (`PR104`)

- Increase `timeout_ms` on the step or `defaultStepTimeoutMs` in config.
- Check plugin logic for cancellation-aware execution.

## Runtime reports `PR200` (workflow load failure)

- Check template paths, inheritance rules, and YAML flow/control block structure.
- This is the common code for template expansion and workflow-load errors before execution begins.

## Runtime reports `PR202` (invalid runtime configuration or CLI usage)

- Check conflicting flags such as `--resume-signal` without `--resume`.
- Check invalid bulk-delete combinations or security-policy conflicts.
- Fix the reported option/config problem and rerun.

## Resume not finding run state

- Verify `--state-dir` matches original run directory.
- Ensure `--resume <runId>` corresponds to an existing `<runId>.json` in state dir.

## NU1900 warning during restore/test

- In restricted-network environments, vulnerability feed access may fail.
- Warning is non-blocking for local build/test in this workspace.
