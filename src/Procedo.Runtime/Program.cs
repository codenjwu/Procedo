
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Observability.Sinks;
using Procedo.Persistence.Stores;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;
using Procedo.Validation;
using Procedo.Validation.Models;

namespace Procedo.Runtime;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logger = new ConsoleLogger();
        try
        {
            if (HasHelpFlag(args))
            {
                PrintHelp();
                return 0;
            }

            var options = ParseArguments(args);

            if (options.ListWaiting)
            {
                return await ListWaitingRunsAsync(logger, options).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(options.ShowRunId))
            {
                return await ShowRunAsync(logger, options).ConfigureAwait(false);
            }

            if (options.HasBulkDelete)
            {
                return await DeleteRunsAsync(logger, options).ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(options.DeleteRunId))
            {
                return await DeleteRunAsync(logger, options).ConfigureAwait(false);
            }

            if (!File.Exists(options.WorkflowPath))
            {
                logger.LogError($"[{RuntimeErrorCodes.WorkflowFileNotFound}] Workflow file not found: {options.WorkflowPath}");
                return 1;
            }

            WorkflowDefinition workflow;
            try
            {
                var loader = new WorkflowTemplateLoader();
                workflow = loader.LoadFromFile(options.WorkflowPath, options.Parameters);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError($"[{RuntimeErrorCodes.WorkflowLoadFailed}] {ex.Message}");
                return 1;
            }

            IPluginRegistry registry = new PluginRegistry();
            registry.AddSystemPlugin(options.SystemSecurity);
            registry.AddDemoPlugin();

            var validationOptions = options.StrictValidation ? ValidationOptions.Strict : ValidationOptions.Permissive;
            var validation = new ProcedoWorkflowValidator().Validate(workflow, registry, validationOptions);
            if (validation.Issues.Count > 0)
            {
                LogValidationIssues(validation, logger);

                if (validation.HasErrors)
                {
                    logger.LogError($"[{RuntimeErrorCodes.ValidationFailed}] Workflow validation failed. Fix validation errors before execution.");
                    return 1;
                }
            }

            var eventSink = BuildEventSink(options.EventJsonPath, options.EmitConsoleEvents);
            var engine = new ProcedoWorkflowEngine();

            WorkflowRunResult result;
            if (!string.IsNullOrWhiteSpace(options.ResumeSignalType))
            {
                result = await ResumeWithPersistenceAsync(engine, workflow, registry, logger, options, eventSink).ConfigureAwait(false);
            }
            else
            {
                var usePersistence = options.Persist || !string.IsNullOrWhiteSpace(options.ResumeRunId);
                result = usePersistence
                    ? await ExecuteWithPersistenceAsync(engine, workflow, registry, logger, options, eventSink).ConfigureAwait(false)
                    : await engine.ExecuteAsync(workflow, registry, logger, eventSink, default, options.Execution).ConfigureAwait(false);
            }

            if (!result.Success)
            {
                if (result.Waiting)
                {
                    logger.LogInformation($"[{result.ErrorCode ?? RuntimeErrorCodes.Waiting}] {result.Error ?? "Workflow is waiting."}");
                    if (!string.IsNullOrWhiteSpace(result.RunId))
                    {
                        logger.LogInformation($"Run id: {result.RunId}");
                    }

                    if (options.Persist || !string.IsNullOrWhiteSpace(options.ResumeRunId) || !string.IsNullOrWhiteSpace(options.ResumeSignalType))
                    {
                        logger.LogInformation($"Run state directory: {options.StateDirectory}");
                    }

                    if (!string.IsNullOrWhiteSpace(options.EventJsonPath))
                    {
                        logger.LogInformation($"Events file: {Path.GetFullPath(options.EventJsonPath)}");
                    }

                    return 2;
                }

                var failureMessage = string.IsNullOrWhiteSpace(result.SourcePath)
                    ? $"[{result.ErrorCode ?? RuntimeErrorCodes.JobFailed}] {result.Error ?? "Unknown error"}"
                    : $"[{result.ErrorCode ?? RuntimeErrorCodes.JobFailed}] {result.Error ?? "Unknown error"} (Source: {result.SourcePath})";
                logger.LogError(failureMessage);
                if (!string.IsNullOrWhiteSpace(result.RunId))
                {
                    logger.LogInformation($"Run id: {result.RunId}");
                }

                return 1;
            }

            logger.LogInformation($"Run id: {result.RunId}");
            if (options.Persist || !string.IsNullOrWhiteSpace(options.ResumeRunId) || !string.IsNullOrWhiteSpace(options.ResumeSignalType))
            {
                logger.LogInformation($"Run state directory: {options.StateDirectory}");
            }

            if (!string.IsNullOrWhiteSpace(options.EventJsonPath))
            {
                logger.LogInformation($"Events file: {Path.GetFullPath(options.EventJsonPath)}");
            }

            return 0;
        }
        catch (FileNotFoundException ex)
        {
            logger.LogError($"[{RuntimeErrorCodes.WorkflowFileNotFound}] {ex.Message}");
            return 1;
        }
        catch (JsonException ex)
        {
            logger.LogError($"[{RuntimeErrorCodes.ConfigurationInvalid}] {ex.Message}");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError($"[{RuntimeErrorCodes.ConfigurationInvalid}] {ex.Message}");
            return 1;
        }
    }

    private static bool HasHelpFlag(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Procedo Runtime (single-node)");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/Procedo.Runtime -- [workflow.yaml] [options]");
        Console.WriteLine();
        Console.WriteLine("Core options:");
        Console.WriteLine("  --config <path>                    Runtime config JSON file");
        Console.WriteLine("  --strict-validation                Enable strict validation mode");
        Console.WriteLine("  --persist                          Persist run state to local store");
        Console.WriteLine("  --resume <runId>                   Resume a persisted run by run id");
        Console.WriteLine("  --resume-signal <type>             Resume a waiting run with the supplied signal type");
        Console.WriteLine("  --resume-payload-json <json|path>  Resume payload as inline JSON or JSON file path");
        Console.WriteLine("  --state-dir <path>                 Persistence directory (default .procedo/runs)");
        Console.WriteLine("  --list-waiting                     List persisted runs currently in Waiting state");
        Console.WriteLine("  --show-run <runId>                 Show persisted run details by run id");
        Console.WriteLine("  --delete-run <runId>               Delete a persisted run state file by run id");
        Console.WriteLine("  --delete-completed                 Delete persisted completed runs");
        Console.WriteLine("  --delete-failed                    Delete persisted failed runs");
        Console.WriteLine("  --delete-all-older-than <timespan> Delete persisted runs older than a threshold");
        Console.WriteLine("  --delete-waiting-older-than <timespan> Delete persisted waiting runs older than a threshold");
        Console.WriteLine("  --param <key=value>                Supply or override workflow parameter values (string, JSON literal, or @json-file)");
        Console.WriteLine();
        Console.WriteLine("Execution policy options:");
        Console.WriteLine("  --max-parallelism <n>              Default max parallelism");
        Console.WriteLine("  --default-retries <n>              Default step retries");
        Console.WriteLine("  --default-timeout-ms <n>           Default step timeout in milliseconds");
        Console.WriteLine("  --continue-on-error                Continue independent steps after failures");
        Console.WriteLine("  --retry-initial-backoff-ms <n>     Initial retry backoff");
        Console.WriteLine("  --retry-backoff-multiplier <n>     Retry backoff multiplier");
        Console.WriteLine("  --retry-max-backoff-ms <n>         Max retry backoff");
        Console.WriteLine();
        Console.WriteLine("Observability options:");
        Console.WriteLine("  --events-console                   Emit structured events to console");
        Console.WriteLine("  --events-json <path>               Emit structured events to JSONL file");
        Console.WriteLine();
        Console.WriteLine("Config precedence:");
        Console.WriteLine("  defaults < JSON config < environment variables < CLI");
        Console.WriteLine();
        Console.WriteLine("Help:");
        Console.WriteLine("  -h, --help                         Show this help text");
    }

    private static async Task<WorkflowRunResult> ExecuteWithPersistenceAsync(
        ProcedoWorkflowEngine engine,
        WorkflowDefinition workflow,
        IPluginRegistry registry,
        ILogger logger,
        RuntimeOptions options,
        IExecutionEventSink? eventSink)
    {
        var runStateStore = new FileRunStateStore(options.StateDirectory);
        return await engine.ExecuteWithPersistenceAsync(
            workflow,
            registry,
            logger,
            runStateStore,
            options.ResumeRunId,
            eventSink,
            default,
            options.Execution).ConfigureAwait(false);
    }

    private static async Task<WorkflowRunResult> ResumeWithPersistenceAsync(
        ProcedoWorkflowEngine engine,
        WorkflowDefinition workflow,
        IPluginRegistry registry,
        ILogger logger,
        RuntimeOptions options,
        IExecutionEventSink? eventSink)
    {
        if (string.IsNullOrWhiteSpace(options.ResumeRunId))
        {
            throw new InvalidOperationException("Resume signal requires --resume <runId>.");
        }

        var runStateStore = new FileRunStateStore(options.StateDirectory);
        return await engine.ResumeAsync(
            workflow,
            registry,
            logger,
            runStateStore,
            options.ResumeRunId,
            new ResumeRequest
            {
                SignalType = options.ResumeSignalType,
                Payload = options.ResumePayload
            },
            eventSink,
            default,
            options.Execution).ConfigureAwait(false);
    }

    private static async Task<int> ListWaitingRunsAsync(ILogger logger, RuntimeOptions options)
    {
        var runStateStore = new FileRunStateStore(options.StateDirectory);
        var runs = await runStateStore.ListRunsAsync().ConfigureAwait(false);
        var waitingRuns = runs.Where(static run => run.Status == RunStatus.Waiting).ToArray();

        if (waitingRuns.Length == 0)
        {
            logger.LogInformation($"No waiting runs found in {options.StateDirectory}");
            return 0;
        }

        foreach (var run in waitingRuns)
        {
            var waitingStep = FindWaitingStep(run);
            var waitReason = waitingStep?.Wait?.Reason ?? "waiting";
            logger.LogInformation($"RunId={run.RunId} Workflow={run.WorkflowName} WaitingStep={waitingStep?.StepId ?? "unknown"} WaitType={waitingStep?.Wait?.Type ?? "unknown"} Since={run.WaitingSinceUtc:O} Reason={waitReason}");
        }

        return 0;
    }

    private static async Task<int> ShowRunAsync(ILogger logger, RuntimeOptions options)
    {
        var runStateStore = new FileRunStateStore(options.StateDirectory);
        var run = await runStateStore.GetRunAsync(options.ShowRunId!).ConfigureAwait(false);

        if (run is null)
        {
            logger.LogWarning($"Run id '{options.ShowRunId}' was not found in {options.StateDirectory}");
            return 1;
        }

        foreach (var line in BuildRunInspectionLines(run))
        {
            logger.LogInformation(line);
        }

        return 0;
    }

    private static IReadOnlyList<string> BuildRunInspectionLines(WorkflowRunState run)
    {
        var lines = new List<string>
        {
            $"RunId: {run.RunId}",
            $"Workflow: {run.WorkflowName} v{run.WorkflowVersion}",
            $"Status: {run.Status}",
            $"CreatedUtc: {run.CreatedAtUtc:O}",
            $"UpdatedUtc: {run.UpdatedAtUtc:O}"
        };

        if (!string.IsNullOrWhiteSpace(run.Error))
        {
            lines.Add($"Error: {run.Error}");
        }

        var waitingStep = FindWaitingStep(run);
        if (waitingStep is not null)
        {
            lines.Add($"WaitingStep: {waitingStep.StepId}");
            if (run.WaitingSinceUtc is not null)
            {
                lines.Add($"WaitingSinceUtc: {run.WaitingSinceUtc:O}");
            }

            if (!string.IsNullOrWhiteSpace(waitingStep.Wait?.Type))
            {
                lines.Add($"WaitType: {waitingStep.Wait.Type}");
            }

            if (!string.IsNullOrWhiteSpace(waitingStep.Wait?.Reason))
            {
                lines.Add($"WaitReason: {waitingStep.Wait.Reason}");
            }
        }

        var grouped = run.Steps.Values
            .GroupBy(static step => step.Status)
            .ToDictionary(group => group.Key, group => group.Count());
        lines.Add($"StepCounts: Pending={GetStepCount(grouped, StepRunStatus.Pending)} Running={GetStepCount(grouped, StepRunStatus.Running)} Waiting={GetStepCount(grouped, StepRunStatus.Waiting)} Skipped={GetStepCount(grouped, StepRunStatus.Skipped)} Completed={GetStepCount(grouped, StepRunStatus.Completed)} Failed={GetStepCount(grouped, StepRunStatus.Failed)}");

        if (run.Steps.Count == 0)
        {
            lines.Add("Steps: none");
            return lines;
        }

        lines.Add("Steps:");
        foreach (var step in run.Steps.Values
                     .OrderBy(static step => step.Stage, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static step => step.Job, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static step => step.StepId, StringComparer.OrdinalIgnoreCase))
        {
            var detail = $"- [{step.Status}] {step.Stage}/{step.Job}/{step.StepId}";
            if (!string.IsNullOrWhiteSpace(step.Error))
            {
                detail += $" Error={step.Error}";
            }
            else if (step.Wait is not null && !string.IsNullOrWhiteSpace(step.Wait.Reason))
            {
                detail += $" WaitReason={step.Wait.Reason}";
            }
            else if (step.Outputs.Count > 0)
            {
                detail += $" Outputs={string.Join(", ", step.Outputs.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase))}";
            }

            lines.Add(detail);
        }

        return lines;
    }

    private static int GetStepCount(IReadOnlyDictionary<StepRunStatus, int> grouped, StepRunStatus status)
        => grouped.TryGetValue(status, out var count) ? count : 0;
    private static async Task<int> DeleteRunsAsync(ILogger logger, RuntimeOptions options)
    {
        var runStateStore = new FileRunStateStore(options.StateDirectory);
        var runs = await runStateStore.ListRunsAsync().ConfigureAwait(false);
        var selected = runs.Where(run => ShouldDeleteRun(run, options)).ToArray();

        if (selected.Length == 0)
        {
            logger.LogInformation($"No persisted runs matched the requested cleanup filter in {options.StateDirectory}");
            return 0;
        }

        var deleted = 0;
        foreach (var run in selected)
        {
            if (await runStateStore.DeleteRunAsync(run.RunId).ConfigureAwait(false))
            {
                deleted++;
            }
        }

        logger.LogInformation($"Deleted {deleted} persisted run(s) from {options.StateDirectory}");
        return 0;
    }

    private static bool ShouldDeleteRun(WorkflowRunState run, RuntimeOptions options)
    {
        if (options.DeleteCompleted && run.Status == RunStatus.Completed)
        {
            return true;
        }

        if (options.DeleteFailed && run.Status == RunStatus.Failed)
        {
            return true;
        }

        if (options.DeleteAllOlderThan is not null)
        {
            var cutoff = DateTimeOffset.UtcNow - options.DeleteAllOlderThan.Value;
            return run.UpdatedAtUtc <= cutoff;
        }

        if (options.DeleteWaitingOlderThan is not null && run.Status == RunStatus.Waiting)
        {
            var cutoff = DateTimeOffset.UtcNow - options.DeleteWaitingOlderThan.Value;
            var referenceTimestamp = run.WaitingSinceUtc ?? run.UpdatedAtUtc;
            return referenceTimestamp <= cutoff;
        }

        return false;
    }
    private static async Task<int> DeleteRunAsync(ILogger logger, RuntimeOptions options)
    {
        var runStateStore = new FileRunStateStore(options.StateDirectory);
        var deleted = await runStateStore.DeleteRunAsync(options.DeleteRunId!).ConfigureAwait(false);

        if (!deleted)
        {
            logger.LogWarning($"Run id '{options.DeleteRunId}' was not found in {options.StateDirectory}");
            return 1;
        }

        logger.LogInformation($"Deleted run id '{options.DeleteRunId}' from {options.StateDirectory}");
        return 0;
    }

    private static IExecutionEventSink? BuildEventSink(string? eventJsonPath, bool emitConsoleEvents)
    {
        var sinks = new List<IExecutionEventSink>();

        if (emitConsoleEvents)
        {
            sinks.Add(new ConsoleExecutionEventSink());
        }

        if (!string.IsNullOrWhiteSpace(eventJsonPath))
        {
            sinks.Add(new JsonFileExecutionEventSink(Path.GetFullPath(eventJsonPath)));
        }

        return sinks.Count switch
        {
            0 => null,
            1 => sinks[0],
            _ => new CompositeExecutionEventSink(sinks)
        };
    }

    private static void LogValidationIssues(ValidationResult validation, ILogger logger)
    {
        foreach (var issue in validation.Issues)
        {
            var message = string.IsNullOrWhiteSpace(issue.SourcePath)
                ? $"{issue.Code} at {issue.Path}: {issue.Message}"
                : $"{issue.Code} at {issue.Path} ({issue.SourcePath}): {issue.Message}";
            if (issue.Severity == ValidationSeverity.Error)
            {
                logger.LogError(message);
            }
            else
            {
                logger.LogWarning(message);
            }
        }
    }

    private static RuntimeOptions ParseArguments(string[] args)
    {
        var options = CreateDefaults();

        string? configPath = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                configPath = args[++i];
                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(configPath))
        {
            ApplyJsonConfig(options, Path.GetFullPath(configPath));
        }
        else
        {
            var defaultConfig = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "procedo.runtime.json"));
            if (File.Exists(defaultConfig))
            {
                ApplyJsonConfig(options, defaultConfig);
            }
        }

        ApplyEnvironment(options);
        ApplyCli(options, args);

        options.WorkflowPath = string.IsNullOrWhiteSpace(options.WorkflowPath)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "examples", "hello_pipeline.yaml"))
            : Path.GetFullPath(options.WorkflowPath);

        options.StateDirectory = string.IsNullOrWhiteSpace(options.StateDirectory)
            ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".procedo", "runs"))
            : Path.GetFullPath(options.StateDirectory);

        ValidateRuntimeOptions(options);
        return options;
    }

    private static RuntimeOptions CreateDefaults()
        => new()
        {
            WorkflowPath = string.Empty,
            StateDirectory = string.Empty,
            Execution = WorkflowExecutionOptions.Default
        };

    private static void ApplyJsonConfig(RuntimeOptions options, string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        if (root.TryGetProperty("workflowPath", out var workflowPath))
        {
            options.WorkflowPath = workflowPath.GetString() ?? options.WorkflowPath;
        }

        if (root.TryGetProperty("resumeRunId", out var resumeRunId))
        {
            options.ResumeRunId = resumeRunId.GetString();
        }

        if (root.TryGetProperty("resumeSignalType", out var resumeSignalType))
        {
            options.ResumeSignalType = resumeSignalType.GetString();
        }

        if (root.TryGetProperty("resumePayload", out var resumePayload) && resumePayload.ValueKind == JsonValueKind.Object)
        {
            options.ResumePayload = JsonObjectToDictionary(resumePayload);
        }

        if (root.TryGetProperty("stateDirectory", out var stateDirectory))
        {
            options.StateDirectory = stateDirectory.GetString() ?? options.StateDirectory;
        }

        if (root.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
        {
            options.Parameters = JsonObjectToDictionary(parameters);
        }

        if (root.TryGetProperty("strictValidation", out var strictValidation) && strictValidation.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            options.StrictValidation = strictValidation.GetBoolean();
        }

        if (root.TryGetProperty("persist", out var persist) && persist.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            options.Persist = persist.GetBoolean();
        }

        if (root.TryGetProperty("eventsJsonPath", out var eventsJsonPath))
        {
            options.EventJsonPath = eventsJsonPath.GetString();
        }

        if (root.TryGetProperty("emitConsoleEvents", out var emitConsoleEvents) && emitConsoleEvents.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            options.EmitConsoleEvents = emitConsoleEvents.GetBoolean();
        }

        if (root.TryGetProperty("systemSecurity", out var systemSecurity) && systemSecurity.ValueKind == JsonValueKind.Object)
        {
            if (systemSecurity.TryGetProperty("allowHttpRequests", out var allowHttp) && allowHttp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                options.SystemSecurity.AllowHttpRequests = allowHttp.GetBoolean();
            }

            if (systemSecurity.TryGetProperty("allowFileSystemAccess", out var allowFileSystem) && allowFileSystem.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                options.SystemSecurity.AllowFileSystemAccess = allowFileSystem.GetBoolean();
            }

            if (systemSecurity.TryGetProperty("allowProcessExecution", out var allowProcess) && allowProcess.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                options.SystemSecurity.AllowProcessExecution = allowProcess.GetBoolean();
            }

            if (systemSecurity.TryGetProperty("allowUnsafeExecutables", out var allowUnsafe) && allowUnsafe.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                options.SystemSecurity.AllowUnsafeExecutables = allowUnsafe.GetBoolean();
            }

            if (systemSecurity.TryGetProperty("allowedPathRoots", out var allowedPathRoots) && allowedPathRoots.ValueKind == JsonValueKind.Array)
            {
                options.SystemSecurity.AllowedPathRoots.Clear();
                foreach (var entry in allowedPathRoots.EnumerateArray())
                {
                    var value = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        options.SystemSecurity.AllowedPathRoots.Add(value);
                    }
                }
            }

            if (systemSecurity.TryGetProperty("allowedHttpHosts", out var allowedHttpHosts) && allowedHttpHosts.ValueKind == JsonValueKind.Array)
            {
                options.SystemSecurity.AllowedHttpHosts.Clear();
                foreach (var entry in allowedHttpHosts.EnumerateArray())
                {
                    var value = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        options.SystemSecurity.AllowedHttpHosts.Add(value);
                    }
                }
            }

            if (systemSecurity.TryGetProperty("allowedExecutables", out var allowedExecutables) && allowedExecutables.ValueKind == JsonValueKind.Array)
            {
                options.SystemSecurity.AllowedExecutables.Clear();
                foreach (var entry in allowedExecutables.EnumerateArray())
                {
                    var value = entry.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        options.SystemSecurity.AllowedExecutables.Add(value);
                    }
                }
            }
        }

        if (!root.TryGetProperty("execution", out var execution) || execution.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (execution.TryGetProperty("defaultMaxParallelism", out var maxP) && maxP.TryGetInt32(out var maxPValue))
        {
            options.Execution.DefaultMaxParallelism = maxPValue;
        }

        if (execution.TryGetProperty("defaultStepRetries", out var retries) && retries.TryGetInt32(out var retriesValue))
        {
            options.Execution.DefaultStepRetries = retriesValue;
        }

        if (execution.TryGetProperty("defaultStepTimeoutMs", out var timeout) && timeout.TryGetInt32(out var timeoutValue))
        {
            options.Execution.DefaultStepTimeoutMs = timeoutValue;
        }

        if (execution.TryGetProperty("continueOnError", out var continueOnError) && continueOnError.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            options.Execution.ContinueOnError = continueOnError.GetBoolean();
        }

        if (execution.TryGetProperty("retryInitialBackoffMs", out var backoff) && backoff.TryGetInt32(out var backoffValue))
        {
            options.Execution.RetryInitialBackoffMs = backoffValue;
        }

        if (execution.TryGetProperty("retryBackoffMultiplier", out var multiplier) && multiplier.TryGetDouble(out var multiplierValue))
        {
            options.Execution.RetryBackoffMultiplier = multiplierValue;
        }

        if (execution.TryGetProperty("retryMaxBackoffMs", out var maxBackoff) && maxBackoff.TryGetInt32(out var maxBackoffValue))
        {
            options.Execution.RetryMaxBackoffMs = maxBackoffValue;
        }
    }

    private static void ApplyEnvironment(RuntimeOptions options)
    {
        options.WorkflowPath = GetEnvString("PROCEDO_WORKFLOW_PATH", options.WorkflowPath);
        options.ResumeRunId = GetEnvString("PROCEDO_RESUME_RUN_ID", options.ResumeRunId);
        options.ResumeSignalType = GetEnvString("PROCEDO_RESUME_SIGNAL", options.ResumeSignalType);
        options.StateDirectory = GetEnvString("PROCEDO_STATE_DIR", options.StateDirectory);
        options.EventJsonPath = GetEnvString("PROCEDO_EVENTS_JSON", options.EventJsonPath);

        var parameterJson = Environment.GetEnvironmentVariable("PROCEDO_PARAMETERS_JSON");
        if (!string.IsNullOrWhiteSpace(parameterJson))
        {
            options.Parameters = ParseResumePayload(parameterJson);
        }

        var resumePayloadJson = Environment.GetEnvironmentVariable("PROCEDO_RESUME_PAYLOAD_JSON");
        if (!string.IsNullOrWhiteSpace(resumePayloadJson))
        {
            options.ResumePayload = ParseResumePayload(resumePayloadJson);
        }

        options.StrictValidation = GetEnvBool("PROCEDO_STRICT_VALIDATION", options.StrictValidation);
        options.EmitConsoleEvents = GetEnvBool("PROCEDO_EVENTS_CONSOLE", options.EmitConsoleEvents);
        options.Persist = GetEnvBool("PROCEDO_PERSIST", options.Persist);

        options.SystemSecurity.AllowHttpRequests = GetEnvBool("PROCEDO_SYSTEM_ALLOW_HTTP", options.SystemSecurity.AllowHttpRequests);
        options.SystemSecurity.AllowFileSystemAccess = GetEnvBool("PROCEDO_SYSTEM_ALLOW_FILESYSTEM", options.SystemSecurity.AllowFileSystemAccess);
        options.SystemSecurity.AllowProcessExecution = GetEnvBool("PROCEDO_SYSTEM_ALLOW_PROCESS", options.SystemSecurity.AllowProcessExecution);
        options.SystemSecurity.AllowUnsafeExecutables = GetEnvBool("PROCEDO_SYSTEM_ALLOW_UNSAFE_EXECUTABLES", options.SystemSecurity.AllowUnsafeExecutables);
        ApplyListOverride(options.SystemSecurity.AllowedPathRoots, Environment.GetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_PATH_ROOTS"));
        ApplyListOverride(options.SystemSecurity.AllowedHttpHosts, Environment.GetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_HTTP_HOSTS"));
        ApplyListOverride(options.SystemSecurity.AllowedExecutables, Environment.GetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_EXECUTABLES"));

        options.Execution.DefaultMaxParallelism = GetEnvInt("PROCEDO_MAX_PARALLELISM", options.Execution.DefaultMaxParallelism);
        options.Execution.DefaultStepRetries = GetEnvInt("PROCEDO_DEFAULT_RETRIES", options.Execution.DefaultStepRetries);
        options.Execution.DefaultStepTimeoutMs = GetEnvNullableInt("PROCEDO_DEFAULT_TIMEOUT_MS", options.Execution.DefaultStepTimeoutMs);
        options.Execution.ContinueOnError = GetEnvBool("PROCEDO_CONTINUE_ON_ERROR", options.Execution.ContinueOnError);
        options.Execution.RetryInitialBackoffMs = GetEnvInt("PROCEDO_RETRY_INITIAL_BACKOFF_MS", options.Execution.RetryInitialBackoffMs);
        options.Execution.RetryBackoffMultiplier = GetEnvDouble("PROCEDO_RETRY_BACKOFF_MULTIPLIER", options.Execution.RetryBackoffMultiplier);
        options.Execution.RetryMaxBackoffMs = GetEnvInt("PROCEDO_RETRY_MAX_BACKOFF_MS", options.Execution.RetryMaxBackoffMs);
    }

    private static void ApplyCli(RuntimeOptions options, string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];

            if (string.Equals(token, "--resume", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ResumeRunId = args[++i];
                continue;
            }

            if (string.Equals(token, "--resume-signal", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ResumeSignalType = args[++i];
                continue;
            }

            if (string.Equals(token, "--resume-payload-json", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ResumePayload = ParseResumePayload(args[++i]);
                continue;
            }

            if (string.Equals(token, "--state-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.StateDirectory = args[++i];
                continue;
            }

            if (string.Equals(token, "--list-waiting", StringComparison.OrdinalIgnoreCase))
            {
                options.ListWaiting = true;
                continue;
            }

            if (string.Equals(token, "--delete-run", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.DeleteRunId = args[++i];
                continue;
            }

            if (string.Equals(token, "--show-run", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.ShowRunId = args[++i];
                continue;
            }

            if (string.Equals(token, "--delete-completed", StringComparison.OrdinalIgnoreCase))
            {
                options.DeleteCompleted = true;
                continue;
            }

            if (string.Equals(token, "--delete-failed", StringComparison.OrdinalIgnoreCase))
            {
                options.DeleteFailed = true;
                continue;
            }

            if (string.Equals(token, "--delete-all-older-than", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.DeleteAllOlderThan = ParseDuration(args[++i], "--delete-all-older-than");
                continue;
            }

            if (string.Equals(token, "--delete-waiting-older-than", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.DeleteWaitingOlderThan = ParseDuration(args[++i], "--delete-waiting-older-than");
                continue;
            }

            if (string.Equals(token, "--param", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var assignment = args[++i];
                var separator = assignment.IndexOf('=');
                if (separator <= 0 || separator == assignment.Length - 1)
                {
                    throw new InvalidOperationException("--param requires key=value format.");
                }

                options.Parameters[assignment[..separator]] = ParseCliParameterValue(assignment[(separator + 1)..]);
                continue;
            }

            if (string.Equals(token, "--events-json", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.EventJsonPath = args[++i];
                continue;
            }

            if (string.Equals(token, "--max-parallelism", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var maxP))
            {
                options.Execution.DefaultMaxParallelism = maxP;
                continue;
            }

            if (string.Equals(token, "--default-retries", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var retries))
            {
                options.Execution.DefaultStepRetries = retries;
                continue;
            }

            if (string.Equals(token, "--default-timeout-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var timeoutMs))
            {
                options.Execution.DefaultStepTimeoutMs = timeoutMs;
                continue;
            }

            if (string.Equals(token, "--retry-initial-backoff-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var backoffMs))
            {
                options.Execution.RetryInitialBackoffMs = backoffMs;
                continue;
            }

            if (string.Equals(token, "--retry-backoff-multiplier", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && double.TryParse(args[++i], out var backoffMultiplier))
            {
                options.Execution.RetryBackoffMultiplier = backoffMultiplier;
                continue;
            }

            if (string.Equals(token, "--retry-max-backoff-ms", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && int.TryParse(args[++i], out var maxBackoffMs))
            {
                options.Execution.RetryMaxBackoffMs = maxBackoffMs;
                continue;
            }

            if (string.Equals(token, "--events-console", StringComparison.OrdinalIgnoreCase))
            {
                options.EmitConsoleEvents = true;
                continue;
            }

            if (string.Equals(token, "--strict-validation", StringComparison.OrdinalIgnoreCase))
            {
                options.StrictValidation = true;
                continue;
            }

            if (string.Equals(token, "--persist", StringComparison.OrdinalIgnoreCase))
            {
                options.Persist = true;
                continue;
            }

            if (string.Equals(token, "--continue-on-error", StringComparison.OrdinalIgnoreCase))
            {
                options.Execution.ContinueOnError = true;
                continue;
            }

            if (string.Equals(token, "--config", StringComparison.OrdinalIgnoreCase))
            {
                i++;
                continue;
            }

            if (string.Equals(token, "--help", StringComparison.OrdinalIgnoreCase) || string.Equals(token, "-h", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!token.StartsWith("--", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(options.WorkflowPath))
            {
                options.WorkflowPath = token;
            }
        }
    }

    private static string GetEnvString(string key, string? fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
    }

    private static bool GetEnvBool(string key, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int GetEnvInt(string key, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static int? GetEnvNullableInt(string key, int? fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static double GetEnvDouble(string key, double fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return double.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static void ApplyListOverride(IList<string> target, string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        target.Clear();
        foreach (var value in rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                target.Add(value);
            }
        }
    }

    private static void ValidateRuntimeOptions(RuntimeOptions options)
    {
        NormalizePathRoots(options.SystemSecurity.AllowedPathRoots);
        NormalizeSimpleList(options.SystemSecurity.AllowedHttpHosts, value => value.ToLowerInvariant());
        NormalizeSimpleList(options.SystemSecurity.AllowedExecutables, value =>
        {
            if (value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar))
            {
                throw new InvalidOperationException($"Executable allowlist entry '{value}' must be a file name, not a path.");
            }

            return value;
        });

        if (!string.IsNullOrWhiteSpace(options.ResumeSignalType) && string.IsNullOrWhiteSpace(options.ResumeRunId))
        {
            throw new InvalidOperationException("Resume signal requires a resume run id.");
        }

        if (options.ListWaiting && !string.IsNullOrWhiteSpace(options.DeleteRunId))
        {
            throw new InvalidOperationException("--list-waiting and --delete-run cannot be used together.");
        }

        if (options.ListWaiting && !string.IsNullOrWhiteSpace(options.ShowRunId))
        {
            throw new InvalidOperationException("--list-waiting and --show-run cannot be used together.");
        }

        if (!string.IsNullOrWhiteSpace(options.DeleteRunId) && !string.IsNullOrWhiteSpace(options.ShowRunId))
        {
            throw new InvalidOperationException("--delete-run and --show-run cannot be used together.");
        }

        var bulkDeleteFlags = (options.DeleteCompleted ? 1 : 0)
            + (options.DeleteFailed ? 1 : 0)
            + (options.DeleteAllOlderThan is not null ? 1 : 0)
            + (options.DeleteWaitingOlderThan is not null ? 1 : 0);

        if (bulkDeleteFlags > 1)
        {
            throw new InvalidOperationException("Only one bulk delete filter may be used at a time.");
        }

        if (options.HasBulkDelete && (!string.IsNullOrWhiteSpace(options.DeleteRunId) || !string.IsNullOrWhiteSpace(options.ShowRunId) || options.ListWaiting))
        {
            throw new InvalidOperationException("Bulk delete options cannot be combined with --list-waiting, --show-run, or --delete-run.");
        }

        var usesPersistence = options.Persist || !string.IsNullOrWhiteSpace(options.ResumeRunId);
        if (usesPersistence && !options.SystemSecurity.AllowFileSystemAccess)
        {
            throw new InvalidOperationException("Persistence requires system file access to be enabled in runtime security settings.");
        }

        if (!string.IsNullOrWhiteSpace(options.EventJsonPath) && !options.SystemSecurity.AllowFileSystemAccess)
        {
            throw new InvalidOperationException("JSON event output requires system file access to be enabled in runtime security settings.");
        }

        if (usesPersistence && options.SystemSecurity.AllowedPathRoots.Count > 0
            && !IsPathWithinAllowedRoots(options.StateDirectory, options.SystemSecurity.AllowedPathRoots))
        {
            throw new InvalidOperationException($"State directory '{options.StateDirectory}' is outside the configured allowed system path roots.");
        }

        if (!string.IsNullOrWhiteSpace(options.EventJsonPath)
            && options.SystemSecurity.AllowedPathRoots.Count > 0
            && !IsPathWithinAllowedRoots(options.EventJsonPath, options.SystemSecurity.AllowedPathRoots))
        {
            throw new InvalidOperationException($"Events JSON path '{options.EventJsonPath}' is outside the configured allowed system path roots.");
        }
    }

    private static void NormalizePathRoots(IList<string> roots)
    {
        var normalized = roots
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        roots.Clear();
        foreach (var root in normalized)
        {
            roots.Add(root);
        }
    }

    private static void NormalizeSimpleList(IList<string> values, Func<string, string> normalize)
    {
        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => normalize(x.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        values.Clear();
        foreach (var value in normalized)
        {
            values.Add(value);
        }
    }

    private static StepRunState? FindWaitingStep(WorkflowRunState runState)
    {
        if (!string.IsNullOrWhiteSpace(runState.WaitingStepKey)
            && runState.Steps.TryGetValue(runState.WaitingStepKey, out var waitingStep))
        {
            return waitingStep;
        }

        return runState.Steps.Values.FirstOrDefault(step => step.Status == StepRunStatus.Waiting);
    }

    private static bool IsPathWithinAllowedRoots(string path, IEnumerable<string> allowedRoots)
    {
        var fullPath = Path.GetFullPath(path);
        foreach (var root in allowedRoots)
        {
            var fullRoot = Path.GetFullPath(root);
            if (IsSameOrChildPath(fullPath, fullRoot))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameOrChildPath(string fullPath, string fullRoot)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedPath, normalizedRoot, comparison))
        {
            return true;
        }

        var rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootWithSeparator, comparison)
            || normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);
    }

    private static TimeSpan ParseDuration(string raw, string optionName)
    {
        if (TimeSpan.TryParse(raw, out var parsed) && parsed > TimeSpan.Zero)
        {
            return parsed;
        }

        throw new InvalidOperationException($"{optionName} requires a positive timespan value such as '1.00:00:00' or '00:30:00'.");
    }
    private static object ParseCliParameterValue(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("@", StringComparison.Ordinal))
        {
            var path = trimmed[1..];
            var source = File.ReadAllText(path);
            using var document = JsonDocument.Parse(source);
            return ConvertJsonElement(document.RootElement);
        }

        if (LooksLikeJsonLiteral(trimmed))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                return ConvertJsonElement(document.RootElement);
            }
            catch (JsonException)
            {
                return raw;
            }
        }

        return raw;
    }

    private static bool LooksLikeJsonLiteral(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value[0] switch
        {
            '{' or '[' or '"' => true,
            't' or 'f' or 'n' => value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("false", StringComparison.OrdinalIgnoreCase)
                || value.Equals("null", StringComparison.OrdinalIgnoreCase),
            '-' => true,
            _ => char.IsDigit(value[0])
        };
    }
    private static Dictionary<string, object> ParseResumePayload(string raw)
    {
        var source = File.Exists(raw) ? File.ReadAllText(raw) : raw;
        using var document = JsonDocument.Parse(source);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Resume payload JSON must be an object.");
        }

        return JsonObjectToDictionary(document.RootElement);
    }

    private static Dictionary<string, object> JsonObjectToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElement(property.Value);
        }

        return result;
    }

    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetInt64(out var i64) => i64,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => JsonObjectToDictionary(element),
            _ => element.ToString()
        };
    }

    private sealed class RuntimeOptions
    {
        public string WorkflowPath { get; set; } = string.Empty;

        public string? ResumeRunId { get; set; }

        public string? ResumeSignalType { get; set; }

        public Dictionary<string, object> ResumePayload { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string StateDirectory { get; set; } = string.Empty;

        public bool StrictValidation { get; set; }

        public string? EventJsonPath { get; set; }

        public bool EmitConsoleEvents { get; set; }

        public bool Persist { get; set; }

        public bool ListWaiting { get; set; }

        public string? DeleteRunId { get; set; }

        public string? ShowRunId { get; set; }

        public bool DeleteCompleted { get; set; }

        public bool DeleteFailed { get; set; }

        public TimeSpan? DeleteAllOlderThan { get; set; }

        public TimeSpan? DeleteWaitingOlderThan { get; set; }

        public bool HasBulkDelete => DeleteCompleted || DeleteFailed || DeleteAllOlderThan is not null || DeleteWaitingOlderThan is not null;

        public Dictionary<string, object> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public SystemPluginSecurityOptions SystemSecurity { get; set; } = new();

        public WorkflowExecutionOptions Execution { get; set; } = WorkflowExecutionOptions.Default;
    }
}





















