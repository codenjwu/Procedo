using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Engine.Graph;
using Procedo.Expressions;
using Procedo.Observability;
using Procedo.Plugin.SDK;

namespace Procedo.Engine.Scheduling;

public sealed class WorkflowScheduler
{ 
    public async Task<bool> ExecuteJobAsync(
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string? sourcePath,
        IReadOnlyDictionary<string, ExecutionNode> graph,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        ExecutionEventPublisher? events = null,
        CancellationToken cancellationToken = default,
        WorkflowExecutionOptions? options = null,
        int? maxParallelism = null,
        bool? continueOnError = null,
        IDictionary<string, object>? initialVariables = null)
    {
        events ??= new ExecutionEventPublisher();
        options ??= WorkflowExecutionOptions.Default;

        var variables = initialVariables is null
            ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(initialVariables, StringComparer.OrdinalIgnoreCase);
        var anyFailures = false;
        var effectiveParallelism = maxParallelism is > 0 ? maxParallelism.Value : options.GetSafeDefaultMaxParallelism();
        var effectiveContinueOnError = continueOnError ?? options.ContinueOnError;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested && AllNodesTerminal(graph))
            {
                return !anyFailures;
            }

            cancellationToken.ThrowIfCancellationRequested();

            PromotePendingNodesToReady(graph);
            var blocked = await MarkBlockedByFailedDependenciesAsync(
                graph,
                runId,
                workflowName,
                stageName,
                jobName,
                sourcePath,
                events,
                cancellationToken).ConfigureAwait(false);

            if (blocked > 0)
            {
                anyFailures = true;
                if (!effectiveContinueOnError)
                {
                    return false;
                }
            }

            var readyNodes = graph.Values
                .Where(static n => n.State == NodeState.Ready)
                .OrderBy(static n => n.Step.Step, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (readyNodes.Count == 0)
            {
                if (AllNodesTerminal(graph))
                {
                    return !anyFailures;
                }

                logger.LogError("Scheduler detected a deadlock or unresolved dependency chain.");
                return false;
            }

            var batch = readyNodes.Take(effectiveParallelism).ToList();
            foreach (var node in batch)
            {
                node.State = NodeState.Running;
            }

            var outcomes = await Task.WhenAll(batch.Select(node => ExecuteNodeAsync(
                node,
                runId,
                workflowName,
                stageName,
                jobName,
                sourcePath,
                pluginRegistry,
                logger,
                variables,
                events,
                options,
                effectiveContinueOnError,
                cancellationToken))).ConfigureAwait(false);

            foreach (var outcome in outcomes)
            {
                if (outcome.Success)
                {
                    continue;
                }

                anyFailures = true;
                if (!outcome.ContinueOnError)
                {
                    return false;
                }
            }
        }
    }

    private static async Task<NodeExecutionOutcome> ExecuteNodeAsync(
        ExecutionNode node,
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string? sourcePath,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IDictionary<string, object> variables,
        ExecutionEventPublisher events,
        WorkflowExecutionOptions options,
        bool defaultContinueOnError,
        CancellationToken cancellationToken)
    {
        var nodeContinueOnError = node.Step.ContinueOnError ?? defaultContinueOnError;

        if (!string.IsNullOrWhiteSpace(node.Step.Condition))
        {
            try
            {
                if (!ExpressionResolver.EvaluateCondition(node.Step.Condition, variables))
                {
                    node.State = NodeState.Completed;
                    logger.LogInformation($"Skipping [{stageName}/{jobName}/{node.Step.Step}] because condition '{node.Step.Condition}' evaluated to false.");
                    await events.PublishAsync(CreateStepEvent(
                        ExecutionEventType.StepSkipped,
                        runId,
                        workflowName,
                        stageName,
                        jobName,
                        node.Step.Step,
                        node.Step.Type,
                        true,
                        null,
                        null,
                        sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                    return NodeExecutionOutcome.SuccessResult();
                }
            }
            catch (ExpressionResolutionException ex)
            {
                node.State = NodeState.Failed;
                node.Error = ex.Message;
                node.ErrorCode = RuntimeErrorCodes.StepException;
                logger.LogError(FormatStepFailureMessage(node.Step.Step, node.Error, node.Step.SourcePath ?? sourcePath, "has invalid condition"));
                await events.PublishAsync(CreateStepEvent(
                    ExecutionEventType.StepFailed,
                    runId,
                    workflowName,
                    stageName,
                    jobName,
                    node.Step.Step,
                    node.Step.Type,
                    false,
                    null,
                    node.Error,
                    sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepException, node.Error);
            }
        }

        if (!pluginRegistry.TryResolve(node.Step.Type, out var stepPlugin) || stepPlugin is null)
        {
            node.State = NodeState.Failed;
            node.Error = $"No plugin registered for type '{node.Step.Type}'.";
            node.ErrorCode = RuntimeErrorCodes.PluginNotFound;
            logger.LogError(FormatStepFailureMessage(node.Step.Step, node.Error, node.Step.SourcePath ?? sourcePath));
            await events.PublishAsync(CreateStepEvent(
                ExecutionEventType.StepFailed,
                runId,
                workflowName,
                stageName,
                jobName,
                node.Step.Step,
                node.Step.Type,
                false,
                null,
                node.Error,
                sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

            return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.PluginNotFound, node.Error);
        }

        await events.PublishAsync(CreateStepEvent(
            ExecutionEventType.StepStarted,
            runId,
            workflowName,
            stageName,
            jobName,
            node.Step.Step,
            node.Step.Type,
            null,
            null,
            null), cancellationToken).ConfigureAwait(false);

        var stepWatch = Stopwatch.StartNew();
        var retries = node.Step.Retries is >= 0 ? node.Step.Retries.Value : options.GetSafeDefaultRetries();
        var maxAttempts = retries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timeoutMs = ResolveTimeoutMs(node.Step.TimeoutMs, options);

            try
            {
                logger.LogInformation($"Running [{stageName}/{jobName}/{node.Step.Step}] ({node.Step.Type}) attempt {attempt}/{maxAttempts}");

                var resolvedInputs = ExpressionResolver.ResolveInputs(node.Step.With, variables);
                using var timeoutCts = BuildTimeoutTokenSource(node.Step.TimeoutMs, options, cancellationToken);

                var context = new StepContext
                {
                    RunId = runId,
                    StepId = node.Step.Step,
                    Inputs = resolvedInputs,
                    Variables = variables,
                    Logger = logger,
                    CancellationToken = timeoutCts?.Token ?? cancellationToken
                };

                var result = await ExecuteWithTimeoutAsync(stepPlugin, context, timeoutMs, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    node.State = NodeState.Completed;
                    if (result.Outputs is not null)
                    {
                        foreach (var (key, value) in result.Outputs)
                        {
                            node.Outputs[key] = value;
                            AddStepOutputVariable(variables, node.Step.Step, key, value);
                        }
                    }

                    await events.PublishAsync(CreateStepEvent(
                        ExecutionEventType.StepCompleted,
                        runId,
                        workflowName,
                        stageName,
                        jobName,
                        node.Step.Step,
                        node.Step.Type,
                        true,
                        stepWatch.ElapsedMilliseconds,
                        null,
                        node.Outputs.Count > 0 ? new Dictionary<string, object>(node.Outputs, StringComparer.OrdinalIgnoreCase) : null), cancellationToken).ConfigureAwait(false);

                    return NodeExecutionOutcome.SuccessResult();
                }

                var failureMessage = string.IsNullOrWhiteSpace(result.Error)
                    ? $"Step '{node.Step.Step}' failed."
                    : result.Error;

                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' failed on attempt {attempt}/{maxAttempts}: {failureMessage}. Retrying.");
                    await DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                node.State = NodeState.Failed;
                node.Error = failureMessage;
                node.ErrorCode = RuntimeErrorCodes.StepResultFailed;
                logger.LogError(FormatStepFailureMessage(node.Step.Step, node.Error, node.Step.SourcePath ?? sourcePath));
                await events.PublishAsync(CreateStepEvent(
                    ExecutionEventType.StepFailed,
                    runId,
                    workflowName,
                    stageName,
                    jobName,
                    node.Step.Step,
                    node.Step.Type,
                    false,
                    stepWatch.ElapsedMilliseconds,
                    node.Error,
                    sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepResultFailed, node.Error);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (timeoutMs is > 0)
            {
                var timeoutMessage = $"Step '{node.Step.Step}' exceeded timeout of {timeoutMs.Value} ms.";

                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' timed out on attempt {attempt}/{maxAttempts}: {timeoutMessage}. Retrying.");
                    await DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                node.State = NodeState.Failed;
                node.Error = timeoutMessage;
                node.ErrorCode = RuntimeErrorCodes.StepTimeout;
                logger.LogError(FormatStepFailureMessage(node.Step.Step, timeoutMessage, node.Step.SourcePath ?? sourcePath, "timed out"));
                await events.PublishAsync(CreateStepEvent(
                    ExecutionEventType.StepFailed,
                    runId,
                    workflowName,
                    stageName,
                    jobName,
                    node.Step.Step,
                    node.Step.Type,
                    false,
                    stepWatch.ElapsedMilliseconds,
                    timeoutMessage,
                    sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepTimeout, timeoutMessage);
            }
            catch (TimeoutException ex)
            {
                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' timed out on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying.");
                    await DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                node.State = NodeState.Failed;
                node.Error = ex.Message;
                node.ErrorCode = RuntimeErrorCodes.StepTimeout;
                logger.LogError(FormatStepFailureMessage(node.Step.Step, ex.Message, node.Step.SourcePath ?? sourcePath, "timed out"));
                await events.PublishAsync(CreateStepEvent(
                    ExecutionEventType.StepFailed,
                    runId,
                    workflowName,
                    stageName,
                    jobName,
                    node.Step.Step,
                    node.Step.Type,
                    false,
                    stepWatch.ElapsedMilliseconds,
                    ex.Message,
                    sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepTimeout, ex.Message);
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' threw on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying.");
                    await DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                node.State = NodeState.Failed;
                node.Error = ex.Message;
                node.ErrorCode = RuntimeErrorCodes.StepException;
                logger.LogError(FormatStepFailureMessage(node.Step.Step, ex.Message, node.Step.SourcePath ?? sourcePath, "threw exception"));
                await events.PublishAsync(CreateStepEvent(
                    ExecutionEventType.StepFailed,
                    runId,
                    workflowName,
                    stageName,
                    jobName,
                    node.Step.Step,
                    node.Step.Type,
                    false,
                    stepWatch.ElapsedMilliseconds,
                    ex.Message,
                    sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

                return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepException, ex.Message);
            }
        }

        node.State = NodeState.Failed;
        node.Error = $"Step '{node.Step.Step}' failed after retry exhaustion.";
        node.ErrorCode = RuntimeErrorCodes.StepResultFailed;
        logger.LogError(FormatStepFailureMessage(node.Step.Step, node.Error, node.Step.SourcePath ?? sourcePath));
        await events.PublishAsync(CreateStepEvent(
            ExecutionEventType.StepFailed,
            runId,
            workflowName,
            stageName,
            jobName,
            node.Step.Step,
            node.Step.Type,
            false,
            stepWatch.ElapsedMilliseconds,
            node.Error,
            sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);

        return NodeExecutionOutcome.Failure(nodeContinueOnError, RuntimeErrorCodes.StepResultFailed, node.Error);
    }

    internal static void PromotePendingNodesToReady(IReadOnlyDictionary<string, ExecutionNode> graph)
    {
        foreach (var node in graph.Values)
        {
            if (node.State != NodeState.Pending)
            {
                continue;
            }

            node.State = AllDependenciesCompleted(node) ? NodeState.Ready : NodeState.Pending;
        }
    }

    internal static async Task<int> MarkBlockedByFailedDependenciesAsync(
        IReadOnlyDictionary<string, ExecutionNode> graph,
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string? sourcePath,
        ExecutionEventPublisher events,
        CancellationToken cancellationToken)
    {
        var blockedCount = 0;

        foreach (var node in graph.Values)
        {
            if (node.State is NodeState.Completed or NodeState.Failed or NodeState.Running)
            {
                continue;
            }

            var failedDependency = node.Dependencies.FirstOrDefault(static d => d.State == NodeState.Failed);
            if (failedDependency is null)
            {
                continue;
            }

            blockedCount++;
            node.State = NodeState.Failed;
            node.Error = $"Step '{node.Step.Step}' cannot run because dependency '{failedDependency.Step.Step}' failed.";
            node.ErrorCode = RuntimeErrorCodes.DependencyBlocked;

            await events.PublishAsync(CreateStepEvent(
                ExecutionEventType.StepFailed,
                runId,
                workflowName,
                stageName,
                jobName,
                node.Step.Step,
                node.Step.Type,
                false,
                null,
                node.Error,
                sourcePath: node.Step.SourcePath ?? sourcePath), cancellationToken).ConfigureAwait(false);
        }

        return blockedCount;
    }

    internal static async Task DelayForRetryAsync(int attempt, WorkflowExecutionOptions options, CancellationToken cancellationToken)
    {
        var initial = options.GetSafeRetryInitialBackoffMs();
        var max = options.GetSafeRetryMaxBackoffMs();
        var multiplier = options.GetSafeRetryBackoffMultiplier();
        var delayMs = (int)Math.Min(max, initial * Math.Pow(multiplier, Math.Max(0, attempt - 1)));
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<StepResult> ExecuteWithTimeoutAsync(
        IProcedoStep step,
        StepContext context,
        int? timeoutMs,
        CancellationToken cancellationToken)
    {
        var executeTask = step.ExecuteAsync(context);

        if (timeoutMs is not > 0)
        {
            return await executeTask.ConfigureAwait(false);
        }

        var timeoutTask = Task.Delay(timeoutMs.Value, cancellationToken);
        var completed = await Task.WhenAny(executeTask, timeoutTask).ConfigureAwait(false);

        if (completed == executeTask)
        {
            return await executeTask.ConfigureAwait(false);
        }

        throw new TimeoutException($"Step '{context.StepId}' exceeded timeout of {timeoutMs.Value} ms.");
    }

    internal static CancellationTokenSource? BuildTimeoutTokenSource(int? stepTimeoutMs, WorkflowExecutionOptions options, CancellationToken cancellationToken)
    {
        var timeoutMs = ResolveTimeoutMs(stepTimeoutMs, options);
        if (timeoutMs is not > 0)
        {
            return null;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs.Value);
        return cts;
    }

    internal static int? ResolveTimeoutMs(int? stepTimeoutMs, WorkflowExecutionOptions options)
    {
        if (stepTimeoutMs is > 0)
        {
            return stepTimeoutMs;
        }

        return options.GetSafeDefaultTimeoutMs();
    }

    internal static void AddStepOutputVariable(IDictionary<string, object> variables, string stepId, string outputKey, object value)
    {
        variables[$"{stepId}.{outputKey}"] = value;
        variables[$"steps.{stepId}.outputs.{outputKey}"] = value;
    }

    internal static string FormatStepFailureMessage(string stepId, string? error, string? sourcePath, string? verb = null)
    {
        var action = string.IsNullOrWhiteSpace(verb) ? "failed" : verb;
        var baseMessage = $"Step '{stepId}' {action}: {error}";
        return string.IsNullOrWhiteSpace(sourcePath) ? baseMessage : $"{baseMessage} (Source: {sourcePath})";
    }

    internal static ExecutionEvent CreateStepEvent(
        ExecutionEventType eventType,
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string stepId,
        string stepType,
        bool? success,
        long? durationMs,
        string? error,
        Dictionary<string, object>? outputs = null,
        string? sourcePath = null)
        => new()
        {
            EventType = eventType,
            RunId = runId,
            WorkflowName = workflowName,
            Stage = stageName,
            Job = jobName,
            StepId = stepId,
            StepType = stepType,
            Success = success,
            DurationMs = durationMs,
            Error = error,
            SourcePath = sourcePath,
            Outputs = outputs
        };

    private static bool AllDependenciesCompleted(ExecutionNode node)
    {
        foreach (var dependency in node.Dependencies)
        {
            if (dependency.State != NodeState.Completed)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool AllNodesTerminal(IReadOnlyDictionary<string, ExecutionNode> graph)
    {
        foreach (var node in graph.Values)
        {
            if (node.State is not (NodeState.Completed or NodeState.Failed))
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct NodeExecutionOutcome(bool Success, bool ContinueOnError, string? ErrorCode, string? Error)
    {
        public static NodeExecutionOutcome SuccessResult() => new(true, false, null, null);

        public static NodeExecutionOutcome Failure(bool continueOnError, string errorCode, string? error)
            => new(false, continueOnError, errorCode, error);
    }
}




