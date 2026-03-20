
using System.Diagnostics;
using Procedo.Core.Abstractions;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine.Graph;
using Procedo.Engine.Scheduling;
using Procedo.Expressions;
using Procedo.Observability;
using Procedo.Plugin.SDK;

namespace Procedo.Engine;

public sealed class ProcedoWorkflowEngine
{
    private readonly ExecutionGraphBuilder _graphBuilder = new();
    private readonly WorkflowScheduler _scheduler = new();

    public Task<WorkflowRunResult> ExecuteAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        CancellationToken cancellationToken)
        => ExecuteAsync(workflow, pluginRegistry, logger, null, cancellationToken, null);

    public Task<WorkflowRunResult> ExecuteAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IExecutionEventSink? eventSink = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(workflow, pluginRegistry, logger, eventSink, cancellationToken, null);

    public async Task<WorkflowRunResult> ExecuteAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IExecutionEventSink? eventSink,
        CancellationToken cancellationToken,
        WorkflowExecutionOptions? executionOptions)
    {
        if (workflow is null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        executionOptions ??= WorkflowExecutionOptions.Default;

        var runId = Guid.NewGuid().ToString("N");
        var events = new ExecutionEventPublisher(eventSink);
        var runWatch = Stopwatch.StartNew();

        await events.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = runId,
            WorkflowName = workflow.Name,
            Resumed = false
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation($"Starting workflow '{workflow.Name}' (run: {runId})");

        foreach (var stage in workflow.Stages)
        {
            logger.LogInformation($"Stage: {stage.Stage}");
            foreach (var job in stage.Jobs)
            {
                logger.LogInformation($"Job: {job.Job}");
                var graph = _graphBuilder.Build(job);
                var initialVariables = WorkflowContextResolver.BuildInitialVariables(workflow);
                var success = await _scheduler.ExecuteJobAsync(
                    runId,
                    workflow.Name,
                    stage.Stage,
                    job.Job,
                    workflow.StageSourcePath ?? workflow.SourcePath,
                    graph,
                    pluginRegistry,
                    logger,
                    events,
                    cancellationToken,
                    executionOptions,
                    ResolveMaxParallelism(workflow, job, executionOptions),
                    ResolveContinueOnError(workflow, job, null, executionOptions),
                    initialVariables).ConfigureAwait(false);

                if (!success)
                {
                    var failure = CreateJobFailureResult(stage.Stage, job.Job, runId, graph, workflow.StageSourcePath ?? workflow.SourcePath);

                    await events.PublishAsync(new ExecutionEvent
                    {
                        EventType = ExecutionEventType.RunFailed,
                        RunId = runId,
                        WorkflowName = workflow.Name,
                        Success = false,
                        DurationMs = runWatch.ElapsedMilliseconds,
                        Error = failure.Error,
                        SourcePath = failure.SourcePath
                    }, cancellationToken).ConfigureAwait(false);

                    return failure;
                }
            }
        }

        logger.LogInformation($"Workflow '{workflow.Name}' completed successfully.");
        await events.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunCompleted,
            RunId = runId,
            WorkflowName = workflow.Name,
            Success = true,
            DurationMs = runWatch.ElapsedMilliseconds
        }, cancellationToken).ConfigureAwait(false);

        return new WorkflowRunResult { Success = true, ErrorCode = RuntimeErrorCodes.None, RunId = runId };
    }

    public Task<WorkflowRunResult> ExecuteWithPersistenceAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        string? runId = null,
        CancellationToken cancellationToken = default)
        => ExecuteWithPersistenceAsync(
            workflow,
            pluginRegistry,
            logger,
            runStateStore,
            runId,
            null,
            cancellationToken,
            null);

    public Task<WorkflowRunResult> ExecuteWithPersistenceAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        string? runId,
        IExecutionEventSink? eventSink,
        CancellationToken cancellationToken = default)
        => ExecuteWithPersistenceAsync(
            workflow,
            pluginRegistry,
            logger,
            runStateStore,
            runId,
            eventSink,
            cancellationToken,
            null);

    public Task<WorkflowRunResult> ExecuteWithPersistenceAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        string? runId,
        IExecutionEventSink? eventSink,
        CancellationToken cancellationToken,
        WorkflowExecutionOptions? executionOptions)
        => ExecuteWithPersistenceCoreAsync(
            workflow,
            pluginRegistry,
            logger,
            runStateStore,
            runId,
            eventSink,
            cancellationToken,
            executionOptions,
            null,
            false);

    public Task<WorkflowRunResult> ResumeAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        string runId,
        ResumeRequest request,
        IExecutionEventSink? eventSink = null,
        CancellationToken cancellationToken = default,
        WorkflowExecutionOptions? executionOptions = null)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return ExecuteWithPersistenceCoreAsync(
            workflow,
            pluginRegistry,
            logger,
            runStateStore,
            runId,
            eventSink,
            cancellationToken,
            executionOptions,
            request,
            true);
    }

    private async Task<WorkflowRunResult?> ExecutePersistedJobAsync(
        WorkflowDefinition workflow,
        StageDefinition stage,
        JobDefinition job,
        string runId,
        WorkflowRunState runState,
        IRunStateStore runStateStore,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        ExecutionEventPublisher events,
        Stopwatch runWatch,
        WorkflowExecutionOptions executionOptions,
        ResumeRequest? resumeRequest,
        PersistedResumeState resumeState,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"Job: {job.Job}");
        var graph = _graphBuilder.Build(job);
        var variables = WorkflowContextResolver.BuildInitialVariables(workflow);
        var effectiveParallelism = ResolveMaxParallelism(workflow, job, executionOptions);
        var effectiveContinueOnError = ResolveContinueOnError(workflow, job, null, executionOptions);
        PersistedNodeExecutionOutcome? firstFailure = null;

        foreach (var node in graph.Values)
        {
            var stepKey = GetStepKey(stage.Stage, job.Job, node.Step.Step);
            if (!runState.Steps.TryGetValue(stepKey, out var existing))
            {
                continue;
            }

            if (existing.Status == StepRunStatus.Completed)
            {
                node.State = NodeState.Completed;
                foreach (var (k, v) in existing.Outputs)
                {
                    node.Outputs[k] = v;
                    WorkflowScheduler.AddStepOutputVariable(variables, node.Step.Step, k, v);
                }

                await events.PublishAsync(new ExecutionEvent
                {
                    EventType = ExecutionEventType.StepCompleted,
                    RunId = runId,
                    WorkflowName = workflow.Name,
                    Stage = stage.Stage,
                    Job = job.Job,
                    StepId = node.Step.Step,
                    StepType = node.Step.Type,
                    Success = true,
                    Resumed = true,
                    Outputs = node.Outputs.Count > 0 ? new Dictionary<string, object>(node.Outputs, StringComparer.OrdinalIgnoreCase) : null,
                    SourcePath = node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath
                }, cancellationToken).ConfigureAwait(false);
            }
            else if (existing.Status == StepRunStatus.Skipped)
            {
                node.State = NodeState.Completed;
                await events.PublishAsync(new ExecutionEvent
                {
                    EventType = ExecutionEventType.StepSkipped,
                    RunId = runId,
                    WorkflowName = workflow.Name,
                    Stage = stage.Stage,
                    Job = job.Job,
                    StepId = node.Step.Step,
                    StepType = node.Step.Type,
                    Success = true,
                    Resumed = true,
                    SourcePath = node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blocked = await MarkPersistedBlockedByFailedDependenciesAsync(
                graph,
                runState,
                runStateStore,
                runId,
                workflow.Name,
                stage.Stage,
                job.Job,
                workflow.StageSourcePath ?? workflow.SourcePath,
                events,
                cancellationToken).ConfigureAwait(false);

            if (blocked.Count > 0)
            {
                firstFailure ??= blocked[0];
                if (!effectiveContinueOnError)
                {
                    return await PublishRunFailureAndReturnAsync(
                        events,
                        runWatch,
                        workflow.Name,
                        stage.Stage,
                        job.Job,
                        runId,
                        blocked[0].SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath,
                        blocked[0].ErrorCode ?? RuntimeErrorCodes.DependencyBlocked,
                        blocked[0].Error,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            WorkflowScheduler.PromotePendingNodesToReady(graph);
            var readyNodes = graph.Values
                .Where(static n => n.State == NodeState.Ready)
                .Where(node => !resumeState.Pending || resumeState.WaitingStepKey is null || string.Equals(GetStepKey(stage.Stage, job.Job, node.Step.Step), resumeState.WaitingStepKey, StringComparison.OrdinalIgnoreCase))
                .OrderBy(static n => n.Step.Step, StringComparer.OrdinalIgnoreCase)
                .Take(effectiveParallelism)
                .ToList();

            if (readyNodes.Count == 0)
            {
                if (WorkflowScheduler.AllNodesTerminal(graph))
                {
                    break;
                }

                var error = resumeState.Pending && resumeState.WaitingStepKey is not null
                    ? $"Waiting step '{resumeState.WaitingStepKey}' could not be resumed because its dependencies are unresolved or the step no longer exists."
                    : "Scheduler detected a deadlock or unresolved dependency chain.";
                logger.LogError(error);
                await MarkRunFailedAsync(runState, runStateStore, error, cancellationToken).ConfigureAwait(false);

                await events.PublishAsync(new ExecutionEvent
                {
                    EventType = ExecutionEventType.RunFailed,
                    RunId = runId,
                    WorkflowName = workflow.Name,
                    Success = false,
                    DurationMs = runWatch.ElapsedMilliseconds,
                    Error = error,
                    SourcePath = workflow.StageSourcePath ?? workflow.SourcePath
                }, cancellationToken).ConfigureAwait(false);

                return Failure(stage.Stage, job.Job, runId, resumeState.Pending ? RuntimeErrorCodes.InvalidResume : RuntimeErrorCodes.SchedulerDeadlock, error, workflow.StageSourcePath ?? workflow.SourcePath);
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var node in readyNodes)
            {
                node.State = NodeState.Running;
                var stepKey = GetStepKey(stage.Stage, job.Job, node.Step.Step);
                var stepState = GetOrCreateStepState(runState, stepKey, stage.Stage, job.Job, node.Step.Step);
                stepState.Status = StepRunStatus.Running;
                stepState.StartedAtUtc ??= now;
                stepState.CompletedAtUtc = null;
                stepState.Error = null;
                stepState.Wait = null;
            }

            await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

            foreach (var node in readyNodes)
            {
                await events.PublishAsync(WorkflowScheduler.CreateStepEvent(
                    ExecutionEventType.StepStarted,
                    runId,
                    workflow.Name,
                    stage.Stage,
                    job.Job,
                    node.Step.Step,
                    node.Step.Type,
                    null,
                    null,
                    null), cancellationToken).ConfigureAwait(false);
            }

            var executionTasks = readyNodes.Select(node => ExecutePersistedNodeAsync(
                node,
                runId,
                workflow.Name,
                stage.Stage,
                job.Job,
                workflow.StageSourcePath ?? workflow.SourcePath,
                pluginRegistry,
                logger,
                new Dictionary<string, object>(variables, StringComparer.OrdinalIgnoreCase),
                executionOptions,
                effectiveContinueOnError,
                resumeState.Pending && resumeState.WaitingStepKey is not null && string.Equals(GetStepKey(stage.Stage, job.Job, node.Step.Step), resumeState.WaitingStepKey, StringComparison.OrdinalIgnoreCase)
                    ? resumeRequest
                    : null,
                cancellationToken)).ToArray();

            var outcomes = await Task.WhenAll(executionTasks).ConfigureAwait(false);
            foreach (var outcome in outcomes)
            {
                var stepKey = GetStepKey(stage.Stage, job.Job, outcome.StepId);
                var stepState = GetOrCreateStepState(runState, stepKey, stage.Stage, job.Job, outcome.StepId);
                switch (outcome.Kind)
                {
                    case PersistedNodeOutcomeKind.Skipped:
                        outcome.Node.State = NodeState.Completed;
                        stepState.Status = StepRunStatus.Skipped;
                        stepState.Error = null;
                        stepState.Wait = null;
                        stepState.Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
                        logger.LogInformation($"Skipping [{stage.Stage}/{job.Job}/{outcome.StepId}] because condition '{outcome.ConditionText}' evaluated to false.");
                        await events.PublishAsync(WorkflowScheduler.CreateStepEvent(
                            ExecutionEventType.StepSkipped,
                            runId,
                            workflow.Name,
                            stage.Stage,
                            job.Job,
                            outcome.StepId,
                            outcome.StepType,
                            true,
                            null,
                            null,
                            sourcePath: outcome.SourcePath), cancellationToken).ConfigureAwait(false);
                        break;

                    case PersistedNodeOutcomeKind.Waiting:
                        outcome.Node.State = NodeState.Ready;
                        outcome.Node.Error = null;
                        outcome.Node.ErrorCode = null;
                        stepState.Status = StepRunStatus.Waiting;
                        stepState.Error = null;
                        stepState.Wait = outcome.WaitDescriptor;
                        stepState.Outputs = new Dictionary<string, object>(outcome.OutputValues, StringComparer.OrdinalIgnoreCase);
                        runState.Status = RunStatus.Waiting;
                        runState.Error = null;
                        runState.WaitingStepKey = stepKey;
                        runState.WaitingSinceUtc = DateTimeOffset.UtcNow;
                        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

                        await events.PublishAsync(new ExecutionEvent
                        {
                            EventType = ExecutionEventType.StepWaiting,
                            RunId = runId,
                            WorkflowName = workflow.Name,
                            Stage = stage.Stage,
                            Job = job.Job,
                            StepId = outcome.StepId,
                            StepType = outcome.StepType,
                            DurationMs = outcome.DurationMs,
                            Error = outcome.Error,
                            SourcePath = outcome.SourcePath,
                            WaitType = outcome.WaitDescriptor?.Type,
                            WaitKey = outcome.WaitDescriptor?.Key
                        }, cancellationToken).ConfigureAwait(false);

                        await events.PublishAsync(new ExecutionEvent
                        {
                            EventType = ExecutionEventType.RunWaiting,
                            RunId = runId,
                            WorkflowName = workflow.Name,
                            Resumed = resumeRequest is not null,
                            WaitType = outcome.WaitDescriptor?.Type,
                            WaitKey = outcome.WaitDescriptor?.Key,
                            SignalType = resumeRequest?.SignalType,
                            DurationMs = runWatch.ElapsedMilliseconds,
                            Error = outcome.Error
                        }, cancellationToken).ConfigureAwait(false);

                        logger.LogInformation($"Workflow '{workflow.Name}' is waiting at step '{outcome.StepId}' (run: {runId})");
                        return Waiting(runState, workflow.Name);

                    case PersistedNodeOutcomeKind.Completed:
                        outcome.Node.State = NodeState.Completed;
                        outcome.Node.Error = null;
                        outcome.Node.ErrorCode = null;
                        stepState.Status = StepRunStatus.Completed;
                        stepState.Error = null;
                        stepState.Wait = null;
                        stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                        stepState.Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        foreach (var (key, value) in outcome.OutputValues)
                        {
                            outcome.Node.Outputs[key] = value;
                            stepState.Outputs[key] = value;
                            WorkflowScheduler.AddStepOutputVariable(variables, outcome.StepId, key, value);
                        }

                        if (resumeState.Pending && resumeState.WaitingStepKey is not null && string.Equals(stepKey, resumeState.WaitingStepKey, StringComparison.OrdinalIgnoreCase))
                        {
                            runState.WaitingStepKey = null;
                            runState.WaitingSinceUtc = null;
                            resumeState.Pending = false;
                        }

                        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
                        await events.PublishAsync(WorkflowScheduler.CreateStepEvent(
                            ExecutionEventType.StepCompleted,
                            runId,
                            workflow.Name,
                            stage.Stage,
                            job.Job,
                            outcome.StepId,
                            outcome.StepType,
                            true,
                            outcome.DurationMs,
                            null,
                            outcome.OutputValues.Count > 0 ? new Dictionary<string, object>(outcome.OutputValues, StringComparer.OrdinalIgnoreCase) : null,
                            sourcePath: outcome.SourcePath), cancellationToken).ConfigureAwait(false);
                        break;

                    case PersistedNodeOutcomeKind.Failed:
                        outcome.Node.State = NodeState.Failed;
                        outcome.Node.Error = outcome.Error;
                        outcome.Node.ErrorCode = outcome.ErrorCode;
                        stepState.Status = StepRunStatus.Failed;
                        stepState.Error = outcome.Error;
                        stepState.Wait = null;
                        stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                        await MarkRunFailedAsync(runState, runStateStore, outcome.Error, cancellationToken).ConfigureAwait(false);
                        logger.LogError(WorkflowScheduler.FormatStepFailureMessage(outcome.StepId, outcome.Error, outcome.SourcePath, outcome.FailureVerb));
                        await events.PublishAsync(WorkflowScheduler.CreateStepEvent(
                            ExecutionEventType.StepFailed,
                            runId,
                            workflow.Name,
                            stage.Stage,
                            job.Job,
                            outcome.StepId,
                            outcome.StepType,
                            false,
                            outcome.DurationMs,
                            outcome.Error,
                            sourcePath: outcome.SourcePath), cancellationToken).ConfigureAwait(false);
                        firstFailure ??= outcome;
                        if (!outcome.ContinueOnError)
                        {
                            return await PublishRunFailureAndReturnAsync(
                                events,
                                runWatch,
                                workflow.Name,
                                stage.Stage,
                                job.Job,
                                runId,
                                outcome.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath,
                                outcome.ErrorCode ?? RuntimeErrorCodes.StepException,
                                outcome.Error,
                                cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }
            }
        }

        if (firstFailure is not null)
        {
            return await PublishRunFailureAndReturnAsync(
                events,
                runWatch,
                workflow.Name,
                stage.Stage,
                job.Job,
                runId,
                firstFailure.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath,
                firstFailure.ErrorCode ?? RuntimeErrorCodes.StepException,
                firstFailure.Error,
                cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private async Task<PersistedNodeExecutionOutcome> ExecutePersistedNodeAsync(
        ExecutionNode node,
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string? sourcePath,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IDictionary<string, object> variables,
        WorkflowExecutionOptions options,
        bool defaultContinueOnError,
        ResumeRequest? resumeRequest,
        CancellationToken cancellationToken)
    {
        var nodeContinueOnError = node.Step.ContinueOnError ?? defaultContinueOnError;
        if (!string.IsNullOrWhiteSpace(node.Step.Condition))
        {
            try
            {
                if (!ExpressionResolver.EvaluateCondition(node.Step.Condition, variables))
                {
                    return PersistedNodeExecutionOutcome.Skipped(node, node.Step.Step, node.Step.Type, node.Step.Condition, node.Step.SourcePath ?? sourcePath);
                }
            }
            catch (ExpressionResolutionException ex)
            {
                return PersistedNodeExecutionOutcome.Failed(
                    node,
                    node.Step.Step,
                    node.Step.Type,
                    RuntimeErrorCodes.StepException,
                    ex.Message,
                    nodeContinueOnError,
                    node.Step.SourcePath ?? sourcePath,
                    "has invalid condition");
            }
        }

        if (!pluginRegistry.TryResolve(node.Step.Type, out var stepPlugin) || stepPlugin is null)
        {
            return PersistedNodeExecutionOutcome.Failed(
                node,
                node.Step.Step,
                node.Step.Type,
                RuntimeErrorCodes.PluginNotFound,
                $"No plugin registered for type '{node.Step.Type}'.",
                nodeContinueOnError,
                node.Step.SourcePath ?? sourcePath);
        }

        var stepWatch = Stopwatch.StartNew();
        var retries = node.Step.Retries is >= 0 ? node.Step.Retries.Value : options.GetSafeDefaultRetries();
        var maxAttempts = retries + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timeoutMs = WorkflowScheduler.ResolveTimeoutMs(node.Step.TimeoutMs, options);
            try
            {
                logger.LogInformation($"Running [{stageName}/{jobName}/{node.Step.Step}] ({node.Step.Type}) attempt {attempt}/{maxAttempts}");
                var resolvedInputs = ExpressionResolver.ResolveInputs(node.Step.With, variables);
                using var timeoutCts = WorkflowScheduler.BuildTimeoutTokenSource(node.Step.TimeoutMs, options, cancellationToken);
                var context = new StepContext
                {
                    RunId = runId,
                    StepId = node.Step.Step,
                    Inputs = resolvedInputs,
                    Variables = variables,
                    Logger = logger,
                    CancellationToken = timeoutCts?.Token ?? cancellationToken,
                    Resume = resumeRequest
                };

                var result = await WorkflowScheduler.ExecuteWithTimeoutAsync(stepPlugin, context, timeoutMs, cancellationToken).ConfigureAwait(false);
                if (result.Waiting)
                {
                    return PersistedNodeExecutionOutcome.Waiting(
                        node,
                        node.Step.Step,
                        node.Step.Type,
                        result.Wait,
                        result.Outputs ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                        result.Wait?.Reason,
                        stepWatch.ElapsedMilliseconds,
                        node.Step.SourcePath ?? sourcePath);
                }

                if (result.Success)
                {
                    return PersistedNodeExecutionOutcome.Completed(
                        node,
                        node.Step.Step,
                        node.Step.Type,
                        result.Outputs ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                        stepWatch.ElapsedMilliseconds,
                        node.Step.SourcePath ?? sourcePath);
                }

                var failureMessage = string.IsNullOrWhiteSpace(result.Error)
                    ? $"Step '{node.Step.Step}' failed."
                    : result.Error;
                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' failed on attempt {attempt}/{maxAttempts}: {failureMessage}. Retrying.");
                    await WorkflowScheduler.DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return PersistedNodeExecutionOutcome.Failed(
                    node,
                    node.Step.Step,
                    node.Step.Type,
                    RuntimeErrorCodes.StepResultFailed,
                    failureMessage,
                    nodeContinueOnError,
                    node.Step.SourcePath ?? sourcePath,
                    null,
                    stepWatch.ElapsedMilliseconds);
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
                    await WorkflowScheduler.DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return PersistedNodeExecutionOutcome.Failed(
                    node,
                    node.Step.Step,
                    node.Step.Type,
                    RuntimeErrorCodes.StepTimeout,
                    timeoutMessage,
                    nodeContinueOnError,
                    node.Step.SourcePath ?? sourcePath,
                    "timed out",
                    stepWatch.ElapsedMilliseconds);
            }
            catch (TimeoutException ex)
            {
                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' timed out on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying.");
                    await WorkflowScheduler.DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return PersistedNodeExecutionOutcome.Failed(
                    node,
                    node.Step.Step,
                    node.Step.Type,
                    RuntimeErrorCodes.StepTimeout,
                    ex.Message,
                    nodeContinueOnError,
                    node.Step.SourcePath ?? sourcePath,
                    "timed out",
                    stepWatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts)
                {
                    logger.LogWarning($"Step '{node.Step.Step}' threw on attempt {attempt}/{maxAttempts}: {ex.Message}. Retrying.");
                    await WorkflowScheduler.DelayForRetryAsync(attempt, options, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return PersistedNodeExecutionOutcome.Failed(
                    node,
                    node.Step.Step,
                    node.Step.Type,
                    RuntimeErrorCodes.StepException,
                    ex.Message,
                    nodeContinueOnError,
                    node.Step.SourcePath ?? sourcePath,
                    "threw exception",
                    stepWatch.ElapsedMilliseconds);
            }
        }

        return PersistedNodeExecutionOutcome.Failed(
            node,
            node.Step.Step,
            node.Step.Type,
            RuntimeErrorCodes.StepResultFailed,
            $"Step '{node.Step.Step}' failed after retry exhaustion.",
            nodeContinueOnError,
            node.Step.SourcePath ?? sourcePath);
    }

    private async Task<List<PersistedNodeExecutionOutcome>> MarkPersistedBlockedByFailedDependenciesAsync(
        IReadOnlyDictionary<string, ExecutionNode> graph,
        WorkflowRunState runState,
        IRunStateStore runStateStore,
        string runId,
        string workflowName,
        string stageName,
        string jobName,
        string? sourcePath,
        ExecutionEventPublisher events,
        CancellationToken cancellationToken)
    {
        var blocked = new List<PersistedNodeExecutionOutcome>();
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

            node.State = NodeState.Failed;
            node.Error = $"Step '{node.Step.Step}' cannot run because dependency '{failedDependency.Step.Step}' failed.";
            node.ErrorCode = RuntimeErrorCodes.DependencyBlocked;

            var stepKey = GetStepKey(stageName, jobName, node.Step.Step);
            var stepState = GetOrCreateStepState(runState, stepKey, stageName, jobName, node.Step.Step);
            stepState.Status = StepRunStatus.Failed;
            stepState.Error = node.Error;
            stepState.Wait = null;
            stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
            await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

            await events.PublishAsync(WorkflowScheduler.CreateStepEvent(
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

            blocked.Add(PersistedNodeExecutionOutcome.Failed(
                node,
                node.Step.Step,
                node.Step.Type,
                RuntimeErrorCodes.DependencyBlocked,
                node.Error,
                continueOnError: true,
                node.Step.SourcePath ?? sourcePath));
        }

        return blocked;
    }

    private sealed class PersistedResumeState(string? waitingStepKey, bool pending)
    {
        public string? WaitingStepKey { get; } = waitingStepKey;

        public bool Pending { get; set; } = pending;
    }

    private enum PersistedNodeOutcomeKind
    {
        Completed,
        Skipped,
        Waiting,
        Failed
    }

    private sealed record PersistedNodeExecutionOutcome(
        PersistedNodeOutcomeKind Kind,
        ExecutionNode Node,
        string StepId,
        string StepType,
        string? ErrorCode,
        string? Error,
        bool ContinueOnError,
        string? SourcePath,
        long? DurationMs = null,
        IDictionary<string, object>? Outputs = null,
        WaitDescriptor? WaitDescriptor = null,
        string? ConditionText = null,
        string? FailureVerb = null)
    {
        public IDictionary<string, object> OutputValues { get; } = Outputs ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public static PersistedNodeExecutionOutcome Completed(
            ExecutionNode node,
            string stepId,
            string stepType,
            IDictionary<string, object> outputs,
            long? durationMs,
            string? sourcePath)
            => new(PersistedNodeOutcomeKind.Completed, node, stepId, stepType, null, null, false, sourcePath, durationMs, new Dictionary<string, object>(outputs, StringComparer.OrdinalIgnoreCase));

        public static PersistedNodeExecutionOutcome Waiting(
            ExecutionNode node,
            string stepId,
            string stepType,
            WaitDescriptor? waitDescriptor,
            IDictionary<string, object> outputs,
            string? error,
            long? durationMs,
            string? sourcePath)
            => new(PersistedNodeOutcomeKind.Waiting, node, stepId, stepType, null, error, false, sourcePath, durationMs, new Dictionary<string, object>(outputs, StringComparer.OrdinalIgnoreCase), waitDescriptor);

        public static PersistedNodeExecutionOutcome Skipped(
            ExecutionNode node,
            string stepId,
            string stepType,
            string? conditionText,
            string? sourcePath)
            => new(PersistedNodeOutcomeKind.Skipped, node, stepId, stepType, null, null, false, sourcePath, null, null, null, conditionText);

        public static PersistedNodeExecutionOutcome Failed(
            ExecutionNode node,
            string stepId,
            string stepType,
            string errorCode,
            string? error,
            bool continueOnError,
            string? sourcePath,
            string? failureVerb = null,
            long? durationMs = null)
            => new(PersistedNodeOutcomeKind.Failed, node, stepId, stepType, errorCode, error, continueOnError, sourcePath, durationMs, null, null, null, failureVerb);
    }

    public async Task<WorkflowRunResult> ResumeWaitingRunAsync(
        IWorkflowDefinitionResolver workflowResolver,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        ResumeWaitingRunRequest request,
        IExecutionEventSink? eventSink = null,
        CancellationToken cancellationToken = default,
        WorkflowExecutionOptions? executionOptions = null)
    {
        if (workflowResolver is null)
        {
            throw new ArgumentNullException(nameof(workflowResolver));
        }

        if (pluginRegistry is null)
        {
            throw new ArgumentNullException(nameof(pluginRegistry));
        }

        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (runStateStore is null)
        {
            throw new ArgumentNullException(nameof(runStateStore));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.WaitType))
        {
            throw new ArgumentException("Wait type is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SignalType))
        {
            throw new ArgumentException("Signal type is required.", nameof(request));
        }

        if (!runStateStore.SupportsConditionalSave())
        {
            return new WorkflowRunResult
            {
                Success = false,
                ErrorCode = RuntimeErrorCodes.ConfigurationInvalid,
                Error = "Callback-driven resume requires a run state store that supports conditional save semantics.",
                RunId = string.Empty
            };
        }

        var matches = await runStateStore.FindWaitingRunsCompatAsync(
            new WaitingRunQuery
            {
                WorkflowName = request.WorkflowName,
                WaitType = request.WaitType,
                WaitKey = request.WaitKey,
                StepId = request.StepId,
                ExpectedSignalType = request.ExpectedSignalType,
                IncludeMetadata = true
            },
            cancellationToken).ConfigureAwait(false);

        if (matches.Count == 0)
        {
            return InvalidResume(string.Empty, $"No waiting run matched wait type '{request.WaitType}' and the supplied identity criteria.");
        }

        var selected = SelectWaitingRun(matches, request);
        if (selected is null)
        {
            return InvalidResume(
                string.Empty,
                $"Multiple waiting runs matched wait type '{request.WaitType}'. Specify stricter criteria or choose a non-default match behavior.");
        }

        if (!string.IsNullOrWhiteSpace(selected.ExpectedSignalType)
            && !string.Equals(selected.ExpectedSignalType, request.SignalType, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidResume(
                selected.RunId,
                $"Waiting run '{selected.RunId}' expects signal type '{selected.ExpectedSignalType}', but '{request.SignalType}' was supplied.");
        }

        var runState = await runStateStore.GetRunAsync(selected.RunId, cancellationToken).ConfigureAwait(false);
        if (runState is null)
        {
            return InvalidResume(selected.RunId, $"Waiting run '{selected.RunId}' no longer exists.");
        }

        WorkflowDefinition workflow;
        try
        {
            workflow = await workflowResolver.ResolveAsync(ToPersistedWorkflowReference(runState), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return InvalidResume(
                selected.RunId,
                $"Unable to resolve workflow definition for run '{selected.RunId}': {ex.Message}",
                runState.WorkflowSourcePath);
        }

        return await ResumeAsync(
            workflow,
            pluginRegistry,
            logger,
            runStateStore,
            selected.RunId,
            new ResumeRequest
            {
                SignalType = request.SignalType,
                Payload = new Dictionary<string, object>(request.Payload, StringComparer.OrdinalIgnoreCase)
            },
            eventSink,
            cancellationToken,
            executionOptions).ConfigureAwait(false);
    }

    private async Task<WorkflowRunResult> ExecuteWithPersistenceCoreAsync(
        WorkflowDefinition workflow,
        IPluginRegistry pluginRegistry,
        ILogger logger,
        IRunStateStore runStateStore,
        string? runId,
        IExecutionEventSink? eventSink,
        CancellationToken cancellationToken,
        WorkflowExecutionOptions? executionOptions,
        ResumeRequest? resumeRequest,
        bool requireWaitingRun)
    {
        if (workflow is null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        if (runStateStore is null)
        {
            throw new ArgumentNullException(nameof(runStateStore));
        }

        executionOptions ??= WorkflowExecutionOptions.Default;

        if (string.IsNullOrWhiteSpace(runId))
        {
            runId = Guid.NewGuid().ToString("N");
        }
        var events = new ExecutionEventPublisher(eventSink);
        var runWatch = Stopwatch.StartNew();

        var existingRun = await runStateStore.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
        var isResumedRun = existingRun is not null;

        if (requireWaitingRun && existingRun is null)
        {
            return InvalidResume(runId, "No persisted run was found for the supplied run id.");
        }

        if (existingRun?.Status == RunStatus.Waiting && resumeRequest is null)
        {
            return Waiting(existingRun, workflow.Name);
        }

        if (requireWaitingRun && existingRun?.Status != RunStatus.Waiting)
        {
            return InvalidResume(runId, $"Run '{runId}' is not waiting and cannot be resumed.");
        }

        var runState = existingRun
            ?? new WorkflowRunState
            {
                RunId = runId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

        runState.WorkflowName = workflow.Name;
        runState.WorkflowVersion = workflow.Version;
        runState.WorkflowSourcePath = workflow.SourcePath;
        runState.WorkflowDefinitionSnapshot = WorkflowDefinitionSnapshotCodec.Serialize(workflow);
        runState.WorkflowDefinitionFingerprint = WorkflowDefinitionSnapshotCodec.ComputeFingerprint(runState.WorkflowDefinitionSnapshot);
        runState.WorkflowParameters = new Dictionary<string, object>(workflow.ParameterValues, StringComparer.OrdinalIgnoreCase);
        runState.Status = RunStatus.Running;
        runState.Error = null;
        if (resumeRequest is not null && existingRun is not null)
        {
            if (!runStateStore.SupportsConditionalSave())
            {
                return new WorkflowRunResult
                {
                    Success = false,
                    ErrorCode = RuntimeErrorCodes.ConfigurationInvalid,
                    Error = "Persisted resume requires a run state store that supports conditional save semantics.",
                    RunId = runId
                };
            }

            var claimed = await runStateStore.TrySaveRunCompatAsync(runState, existingRun.ConcurrencyVersion, cancellationToken).ConfigureAwait(false);
            if (!claimed)
            {
                return InvalidResume(runId, $"Run '{runId}' is no longer waiting and cannot be resumed.");
            }
        }
        else
        {
            await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
        }

        if (resumeRequest is not null)
        {
            await events.PublishAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunResumed,
                RunId = runId,
                WorkflowName = workflow.Name,
                Resumed = true,
                SignalType = resumeRequest.SignalType
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation($"Resuming workflow '{workflow.Name}' (run: {runId})");
        }
        else
        {
            await events.PublishAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunStarted,
                RunId = runId,
                WorkflowName = workflow.Name,
                Resumed = isResumedRun
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation($"Starting/resuming workflow '{workflow.Name}' (run: {runId})");
        }

        var resumeState = new PersistedResumeState(existingRun?.WaitingStepKey, resumeRequest is not null);

        foreach (var stage in workflow.Stages)
        {
            logger.LogInformation($"Stage: {stage.Stage}");
            foreach (var job in stage.Jobs)
            {
                var persistedResult = await ExecutePersistedJobAsync(
                    workflow,
                    stage,
                    job,
                    runId,
                    runState,
                    runStateStore,
                    pluginRegistry,
                    logger,
                    events,
                    runWatch,
                    executionOptions,
                    resumeRequest,
                    resumeState,
                    cancellationToken).ConfigureAwait(false);

                if (persistedResult is not null)
                {
                    return persistedResult;
                }
            }
        }

        if (resumeState.Pending)
        {
            var error = $"Waiting step '{resumeState.WaitingStepKey}' was not found in workflow '{workflow.Name}'.";
            logger.LogError(error);
            await MarkRunFailedAsync(runState, runStateStore, error, cancellationToken).ConfigureAwait(false);
            return InvalidResume(runId, error, workflow.SourcePath);
        }

        runState.Status = RunStatus.Completed;
        runState.Error = null;
        runState.WaitingStepKey = null;
        runState.WaitingSinceUtc = null;
        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

        logger.LogInformation($"Workflow '{workflow.Name}' completed successfully.");
        await events.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunCompleted,
            RunId = runId,
            WorkflowName = workflow.Name,
            Success = true,
            DurationMs = runWatch.ElapsedMilliseconds,
            Resumed = isResumedRun
        }, cancellationToken).ConfigureAwait(false);

        return new WorkflowRunResult { Success = true, ErrorCode = RuntimeErrorCodes.None, RunId = runId };
    }

    private static async Task<WorkflowRunResult> PublishRunFailureAndReturnAsync(
        ExecutionEventPublisher events,
        Stopwatch runWatch,
        string workflowName,
        string stage,
        string job,
        string runId,
        string? sourcePath,
        string errorCode,
        string? error,
        CancellationToken cancellationToken)
    {
        var result = Failure(stage, job, runId, errorCode, error, sourcePath);

        await events.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunFailed,
            RunId = runId,
            WorkflowName = workflowName,
            Success = false,
            DurationMs = runWatch.ElapsedMilliseconds,
            Error = result.Error,
            SourcePath = result.SourcePath
        }, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private static ExecutionEvent CreateStepEvent(
        ExecutionEventType type,
        string runId,
        string workflowName,
        string stage,
        string job,
        string step,
        string stepType,
        bool? success,
        long? durationMs,
        string? error,
        Dictionary<string, object>? outputs = null,
        string? sourcePath = null,
        string? waitType = null,
        string? waitKey = null,
        string? signalType = null)
        => new()
        {
            EventType = type,
            RunId = runId,
            WorkflowName = workflowName,
            Stage = stage,
            Job = job,
            StepId = step,
            StepType = stepType,
            Success = success,
            WaitType = waitType,
            WaitKey = waitKey,
            SignalType = signalType,
            DurationMs = durationMs,
            Error = error,
            SourcePath = sourcePath,
            Outputs = outputs
        };

    private static void AddStepOutputVariable(IDictionary<string, object> variables, string stepId, string outputKey, object value)
    {
        variables[$"{stepId}.{outputKey}"] = value;
        variables[$"steps.{stepId}.outputs.{outputKey}"] = value;
    }

    private static string FormatStepFailureMessage(string stepId, string? error, string? sourcePath, string? verb = null)
    {
        var action = string.IsNullOrWhiteSpace(verb) ? "failed" : verb;
        var baseMessage = $"Step '{stepId}' {action}: {error}";
        return string.IsNullOrWhiteSpace(sourcePath) ? baseMessage : $"{baseMessage} (Source: {sourcePath})";
    }

    private static StepRunState GetOrCreateStepState(
        WorkflowRunState runState,
        string stepKey,
        string stage,
        string job,
        string stepId)
    {
        if (!runState.Steps.TryGetValue(stepKey, out var state))
        {
            state = new StepRunState
            {
                Stage = stage,
                Job = job,
                StepId = stepId
            };
            runState.Steps[stepKey] = state;
        }

        return state;
    }

    private static bool ResolveContinueOnError(
        WorkflowDefinition workflow,
        JobDefinition job,
        StepDefinition? step,
        WorkflowExecutionOptions options)
        => step?.ContinueOnError ?? job.ContinueOnError ?? workflow.ContinueOnError ?? options.ContinueOnError;

    private static int ResolveMaxParallelism(
        WorkflowDefinition workflow,
        JobDefinition job,
        WorkflowExecutionOptions options)
    {
        var value = job.MaxParallelism ?? workflow.MaxParallelism ?? options.GetSafeDefaultMaxParallelism();
        return value > 0 ? value : 1;
    }

    private static string GetStepKey(string stage, string job, string stepId)
        => $"{stage}/{job}/{stepId}";

    private static PersistedWorkflowReference ToPersistedWorkflowReference(WorkflowRunState runState)
        => new()
        {
            RunId = runState.RunId,
            WorkflowName = runState.WorkflowName,
            WorkflowVersion = runState.WorkflowVersion,
            WorkflowSourcePath = runState.WorkflowSourcePath,
            WorkflowDefinitionSnapshot = runState.WorkflowDefinitionSnapshot,
            WorkflowDefinitionFingerprint = runState.WorkflowDefinitionFingerprint,
            Parameters = new Dictionary<string, object>(runState.WorkflowParameters, StringComparer.OrdinalIgnoreCase)
        };

    private static ActiveWaitState? SelectWaitingRun(IReadOnlyList<ActiveWaitState> matches, ResumeWaitingRunRequest request)
    {
        if (matches.Count == 0)
        {
            return null;
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        return request.MatchBehavior switch
        {
            WaitingRunMatchBehavior.ResumeNewest => matches
                .OrderByDescending(static wait => wait.WaitingSinceUtc)
                .ThenBy(static wait => wait.RunId, StringComparer.OrdinalIgnoreCase)
                .First(),
            WaitingRunMatchBehavior.ResumeOldest => matches
                .OrderBy(static wait => wait.WaitingSinceUtc)
                .ThenBy(static wait => wait.RunId, StringComparer.OrdinalIgnoreCase)
                .First(),
            _ => null
        };
    }

    private static async Task MarkRunFailedAsync(
        WorkflowRunState runState,
        IRunStateStore store,
        string? error,
        CancellationToken cancellationToken)
    {
        runState.Status = RunStatus.Failed;
        runState.Error = error;
        runState.WaitingStepKey = null;
        runState.WaitingSinceUtc = null;
        await store.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
    }

    private static WorkflowRunResult Failure(
        string stage,
        string job,
        string runId,
        string errorCode = RuntimeErrorCodes.JobFailed,
        string? error = null,
        string? sourcePath = null)
        => new()
        {
            Success = false,
            Error = string.IsNullOrWhiteSpace(error) ? $"Execution failed at stage '{stage}', job '{job}'." : error,
            ErrorCode = errorCode,
            RunId = runId,
            SourcePath = sourcePath
        };

    private static WorkflowRunResult CreateJobFailureResult(
        string stage,
        string job,
        string runId,
        IReadOnlyDictionary<string, ExecutionNode> graph,
        string? sourcePath)
    {
        var failedNode = graph.Values
            .Where(static node => node.State == NodeState.Failed)
            .OrderBy(static node => node.Step.Step, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (failedNode is null)
        {
            return Failure(
                stage,
                job,
                runId,
                RuntimeErrorCodes.SchedulerDeadlock,
                "Scheduler detected a deadlock or unresolved dependency chain.",
                sourcePath);
        }

        return Failure(
            stage,
            job,
            runId,
            failedNode.ErrorCode ?? RuntimeErrorCodes.JobFailed,
            failedNode.Error ?? $"Execution failed at stage '{stage}', job '{job}'.",
            failedNode.Step.SourcePath ?? sourcePath);
    }

    private static WorkflowRunResult Waiting(WorkflowRunState runState, string workflowName)
    {
        var waitingStep = FindWaitingStep(runState);
        return new WorkflowRunResult
        {
            Success = false,
            Waiting = true,
            Error = waitingStep?.Wait?.Reason ?? $"Workflow '{workflowName}' is waiting for an external signal.",
            ErrorCode = RuntimeErrorCodes.Waiting,
            RunId = runState.RunId,
            WaitingStepId = waitingStep?.StepId,
            WaitingType = waitingStep?.Wait?.Type
        };
    }

    private static WorkflowRunResult InvalidResume(string runId, string error, string? sourcePath = null)
        => new()
        {
            Success = false,
            Error = error,
            ErrorCode = RuntimeErrorCodes.InvalidResume,
            RunId = runId,
            SourcePath = sourcePath
        };

    private static StepRunState? FindWaitingStep(WorkflowRunState runState)
    {
        if (!string.IsNullOrWhiteSpace(runState.WaitingStepKey)
            && runState.Steps.TryGetValue(runState.WaitingStepKey, out var waitingStep))
        {
            return waitingStep;
        }

        return runState.Steps.Values.FirstOrDefault(step => step.Status == StepRunStatus.Waiting);
    }
}

