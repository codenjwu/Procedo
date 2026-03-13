
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
        runState.Status = RunStatus.Running;
        runState.Error = null;
        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

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

        var waitingStepKey = existingRun?.WaitingStepKey;
        var resumePending = resumeRequest is not null;

        foreach (var stage in workflow.Stages)
        {
            logger.LogInformation($"Stage: {stage.Stage}");
            foreach (var job in stage.Jobs)
            {
                logger.LogInformation($"Job: {job.Job}");
                var graph = _graphBuilder.Build(job);
                var variables = WorkflowContextResolver.BuildInitialVariables(workflow);

                foreach (var node in graph.Values)
                {
                    var stepKey = GetStepKey(stage.Stage, job.Job, node.Step.Step);
                    if (!runState.Steps.TryGetValue(stepKey, out var existing)
                        || existing.Status is not (StepRunStatus.Completed or StepRunStatus.Skipped))
                    {
                        continue;
                    }

                    node.State = NodeState.Completed;
                    foreach (var (k, v) in existing.Outputs)
                    {
                        node.Outputs[k] = v;
                        AddStepOutputVariable(variables, node.Step.Step, k, v);
                    }

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

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var progress = false;

                    foreach (var node in graph.Values)
                    {
                        if (node.State is NodeState.Completed or NodeState.Failed)
                        {
                            continue;
                        }

                        var stepKey = GetStepKey(stage.Stage, job.Job, node.Step.Step);
                        if (resumePending && waitingStepKey is not null && !string.Equals(stepKey, waitingStepKey, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (node.State == NodeState.Pending)
                        {
                            var depsComplete = node.Dependencies.All(d => d.State == NodeState.Completed);
                            node.State = depsComplete ? NodeState.Ready : NodeState.Pending;
                        }

                        if (node.State != NodeState.Ready)
                        {
                            continue;
                        }

                        progress = true;
                        var stepState = GetOrCreateStepState(runState, stepKey, stage.Stage, job.Job, node.Step.Step);

                        if (!string.IsNullOrWhiteSpace(node.Step.Condition))
                        {
                            try
                            {
                                if (!ExpressionResolver.EvaluateCondition(node.Step.Condition, variables))
                                {
                                    node.State = NodeState.Completed;
                                    stepState.Status = StepRunStatus.Skipped;
                                    stepState.Error = null;
                                    stepState.Wait = null;
                                    stepState.Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                    stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                                    await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

                                    logger.LogInformation($"Skipping [{stage.Stage}/{job.Job}/{node.Step.Step}] because condition '{node.Step.Condition}' evaluated to false.");
                                    await events.PublishAsync(CreateStepEvent(
                                        ExecutionEventType.StepSkipped,
                                        runId,
                                        workflow.Name,
                                        stage.Stage,
                                        job.Job,
                                        node.Step.Step,
                                        node.Step.Type,
                                        true,
                                        null,
                                        null,
                                        sourcePath: node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath), cancellationToken).ConfigureAwait(false);
                                    continue;
                                }
                            }
                            catch (ExpressionResolutionException ex)
                            {
                                node.State = NodeState.Failed;
                                node.Error = ex.Message;
                                node.ErrorCode = RuntimeErrorCodes.StepException;
                                stepState.Status = StepRunStatus.Failed;
                                stepState.Error = ex.Message;
                                stepState.Wait = null;
                                stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                                await MarkRunFailedAsync(runState, runStateStore, ex.Message, cancellationToken).ConfigureAwait(false);
                                logger.LogError(FormatStepFailureMessage(node.Step.Step, ex.Message, node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath, "has invalid condition"));

                                await events.PublishAsync(CreateStepEvent(
                                    ExecutionEventType.StepFailed,
                                    runId,
                                    workflow.Name,
                                    stage.Stage,
                                    job.Job,
                                    node.Step.Step,
                                    node.Step.Type,
                                    false,
                                    null,
                                    ex.Message,
                                    sourcePath: node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath), cancellationToken).ConfigureAwait(false);

                                return await PublishRunFailureAndReturnAsync(events, runWatch, workflow.Name, stage.Stage, job.Job, runId, workflow.StageSourcePath ?? workflow.SourcePath, RuntimeErrorCodes.StepException, ex.Message, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        node.State = NodeState.Running;
                        stepState.Status = StepRunStatus.Running;
                        stepState.StartedAtUtc ??= DateTimeOffset.UtcNow;
                        stepState.CompletedAtUtc = null;
                        stepState.Error = null;
                        stepState.Wait = null;
                        await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

                        if (!pluginRegistry.TryResolve(node.Step.Type, out var stepPlugin) || stepPlugin is null)
                        {
                            node.State = NodeState.Failed;
                            node.Error = $"No plugin registered for type '{node.Step.Type}'.";
                            node.ErrorCode = RuntimeErrorCodes.PluginNotFound;
                            stepState.Status = StepRunStatus.Failed;
                            stepState.Error = node.Error;
                            stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                            await MarkRunFailedAsync(runState, runStateStore, stepState.Error, cancellationToken).ConfigureAwait(false);
                            logger.LogError(FormatStepFailureMessage(node.Step.Step, stepState.Error, node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath));

                            await events.PublishAsync(CreateStepEvent(
                                ExecutionEventType.StepFailed,
                                runId,
                                workflow.Name,
                                stage.Stage,
                                job.Job,
                                node.Step.Step,
                                node.Step.Type,
                                false,
                                stepState.StartedAtUtc is null ? null : (long?)(DateTimeOffset.UtcNow - stepState.StartedAtUtc.Value).TotalMilliseconds,
                                stepState.Error,
                                sourcePath: node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath), cancellationToken).ConfigureAwait(false);

                            return await PublishRunFailureAndReturnAsync(events, runWatch, workflow.Name, stage.Stage, job.Job, runId, workflow.StageSourcePath ?? workflow.SourcePath, RuntimeErrorCodes.PluginNotFound, stepState.Error, cancellationToken).ConfigureAwait(false);
                        }

                        var stepWatch = Stopwatch.StartNew();
                        await events.PublishAsync(CreateStepEvent(
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

                        logger.LogInformation($"Running [{stage.Stage}/{job.Job}/{node.Step.Step}] ({node.Step.Type})");

                        try
                        {
                            var resolvedInputs = ExpressionResolver.ResolveInputs(node.Step.With, variables);
                            var context = new StepContext
                            {
                                RunId = runId,
                                StepId = node.Step.Step,
                                Inputs = resolvedInputs,
                                Variables = variables,
                                Logger = logger,
                                CancellationToken = cancellationToken,
                                Resume = resumePending && waitingStepKey is not null && string.Equals(stepKey, waitingStepKey, StringComparison.OrdinalIgnoreCase)
                                    ? resumeRequest
                                    : null
                            };

                            var result = await stepPlugin.ExecuteAsync(context).ConfigureAwait(false);
                            if (result.Waiting)
                            {
                                node.State = NodeState.Ready;
                                node.Error = null;
                                stepState.Status = StepRunStatus.Waiting;
                                stepState.Error = null;
                                stepState.Wait = result.Wait;
                                stepState.Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                if (result.Outputs is not null)
                                {
                                    foreach (var (k, v) in result.Outputs)
                                    {
                                        stepState.Outputs[k] = v;
                                    }
                                }

                                runState.Status = RunStatus.Waiting;
                                runState.Error = null;
                                runState.WaitingStepKey = stepKey;
                                runState.WaitingSinceUtc = DateTimeOffset.UtcNow;
                                await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);

                                await events.PublishAsync(CreateStepEvent(
                                    ExecutionEventType.StepWaiting,
                                    runId,
                                    workflow.Name,
                                    stage.Stage,
                                    job.Job,
                                    node.Step.Step,
                                    node.Step.Type,
                                    null,
                                    stepWatch.ElapsedMilliseconds,
                                    result.Wait?.Reason,
                                    null,
                                    waitType: result.Wait?.Type,
                                    waitKey: result.Wait?.Key), cancellationToken).ConfigureAwait(false);

                                await events.PublishAsync(new ExecutionEvent
                                {
                                    EventType = ExecutionEventType.RunWaiting,
                                    RunId = runId,
                                    WorkflowName = workflow.Name,
                                    Resumed = resumeRequest is not null,
                                    WaitType = result.Wait?.Type,
                                    WaitKey = result.Wait?.Key,
                                    SignalType = resumeRequest?.SignalType,
                                    DurationMs = runWatch.ElapsedMilliseconds,
                                    Error = result.Wait?.Reason
                                }, cancellationToken).ConfigureAwait(false);

                                logger.LogInformation($"Workflow '{workflow.Name}' is waiting at step '{node.Step.Step}' (run: {runId})");
                                return Waiting(runState, workflow.Name);
                            }

                            if (!result.Success)
                            {
                                node.State = NodeState.Failed;
                                node.Error = string.IsNullOrWhiteSpace(result.Error)
                                    ? $"Step '{node.Step.Step}' failed."
                                    : result.Error;
                                node.ErrorCode = RuntimeErrorCodes.StepResultFailed;

                                stepState.Status = StepRunStatus.Failed;
                                stepState.Error = node.Error;
                                stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                                await MarkRunFailedAsync(runState, runStateStore, node.Error, cancellationToken).ConfigureAwait(false);
                                logger.LogError(FormatStepFailureMessage(node.Step.Step, node.Error, node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath));

                                await events.PublishAsync(CreateStepEvent(
                                    ExecutionEventType.StepFailed,
                                    runId,
                                    workflow.Name,
                                    stage.Stage,
                                    job.Job,
                                    node.Step.Step,
                                    node.Step.Type,
                                    false,
                                    stepWatch.ElapsedMilliseconds,
                                    node.Error,
                                    sourcePath: node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath), cancellationToken).ConfigureAwait(false);

                                return await PublishRunFailureAndReturnAsync(events, runWatch, workflow.Name, stage.Stage, job.Job, runId, workflow.StageSourcePath ?? workflow.SourcePath, RuntimeErrorCodes.StepResultFailed, node.Error, cancellationToken).ConfigureAwait(false);
                            }

                            node.State = NodeState.Completed;
                            stepState.Status = StepRunStatus.Completed;
                            stepState.Error = null;
                            stepState.Wait = null;
                            stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                            stepState.Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                            if (result.Outputs is not null)
                            {
                                foreach (var (k, v) in result.Outputs)
                                {
                                    node.Outputs[k] = v;
                                    stepState.Outputs[k] = v;
                                    AddStepOutputVariable(variables, node.Step.Step, k, v);
                                }
                            }

                            if (resumePending && waitingStepKey is not null && string.Equals(stepKey, waitingStepKey, StringComparison.OrdinalIgnoreCase))
                            {
                                runState.WaitingStepKey = null;
                                runState.WaitingSinceUtc = null;
                                resumePending = false;
                            }

                            await runStateStore.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
                            await events.PublishAsync(CreateStepEvent(
                                ExecutionEventType.StepCompleted,
                                runId,
                                workflow.Name,
                                stage.Stage,
                                job.Job,
                                node.Step.Step,
                                node.Step.Type,
                                true,
                                stepWatch.ElapsedMilliseconds,
                                null,
                                node.Outputs.Count > 0 ? new Dictionary<string, object>(node.Outputs, StringComparer.OrdinalIgnoreCase) : null), cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            node.State = NodeState.Failed;
                            node.Error = ex.Message;
                            node.ErrorCode = RuntimeErrorCodes.StepException;
                            stepState.Status = StepRunStatus.Failed;
                            stepState.Error = ex.Message;
                            stepState.Wait = null;
                            stepState.CompletedAtUtc = DateTimeOffset.UtcNow;
                            await MarkRunFailedAsync(runState, runStateStore, ex.Message, cancellationToken).ConfigureAwait(false);
                            logger.LogError(FormatStepFailureMessage(node.Step.Step, ex.Message, node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath, "threw exception"));

                            await events.PublishAsync(CreateStepEvent(
                                ExecutionEventType.StepFailed,
                                runId,
                                workflow.Name,
                                stage.Stage,
                                job.Job,
                                node.Step.Step,
                                node.Step.Type,
                                false,
                                stepWatch.ElapsedMilliseconds,
                                ex.Message,
                                sourcePath: node.Step.SourcePath ?? workflow.StageSourcePath ?? workflow.SourcePath), cancellationToken).ConfigureAwait(false);

                            return await PublishRunFailureAndReturnAsync(events, runWatch, workflow.Name, stage.Stage, job.Job, runId, workflow.StageSourcePath ?? workflow.SourcePath, RuntimeErrorCodes.StepException, ex.Message, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (graph.Values.All(n => n.State == NodeState.Completed))
                    {
                        break;
                    }

                    if (resumePending && waitingStepKey is not null)
                    {
                        var jobContainsWaitingStep = graph.Values.Any(node => string.Equals(
                            GetStepKey(stage.Stage, job.Job, node.Step.Step),
                            waitingStepKey,
                            StringComparison.OrdinalIgnoreCase));

                        if (!jobContainsWaitingStep)
                        {
                            break;
                        }
                    }

                    if (!progress)
                    {
                        var error = resumePending && waitingStepKey is not null
                            ? $"Waiting step '{waitingStepKey}' could not be resumed because its dependencies are unresolved or the step no longer exists."
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

                        return Failure(stage.Stage, job.Job, runId, resumePending ? RuntimeErrorCodes.InvalidResume : RuntimeErrorCodes.SchedulerDeadlock, error, workflow.StageSourcePath ?? workflow.SourcePath);
                    }
                }
            }
        }

        if (resumePending)
        {
            var error = $"Waiting step '{waitingStepKey}' was not found in workflow '{workflow.Name}'.";
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

