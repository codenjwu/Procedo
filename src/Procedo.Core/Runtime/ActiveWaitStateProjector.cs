namespace Procedo.Core.Runtime;

public static class ActiveWaitStateProjector
{
    public static ActiveWaitState? TryProject(WorkflowRunState run, bool includeMetadata)
    {
        if (run is null)
        {
            throw new ArgumentNullException(nameof(run));
        }

        if (!string.IsNullOrWhiteSpace(run.WaitingStepKey)
            && run.Steps.TryGetValue(run.WaitingStepKey, out var waitingStep)
            && waitingStep.Wait is not null)
        {
            return Create(run, run.WaitingStepKey, waitingStep, includeMetadata);
        }

        foreach (var (stepPath, stepState) in run.Steps)
        {
            if (stepState.Status == StepRunStatus.Waiting && stepState.Wait is not null)
            {
                return Create(run, stepPath, stepState, includeMetadata);
            }
        }

        return null;
    }

    private static ActiveWaitState Create(WorkflowRunState run, string stepPath, StepRunState stepState, bool includeMetadata)
    {
        var metadata = includeMetadata
            ? CloneDictionary(stepState.Wait!.Metadata)
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        return new ActiveWaitState
        {
            RunId = run.RunId,
            WorkflowName = run.WorkflowName,
            RunStatus = run.Status,
            WaitingSinceUtc = run.WaitingSinceUtc,
            Stage = stepState.Stage,
            Job = stepState.Job,
            StepId = stepState.StepId,
            StepPath = stepPath,
            WaitType = stepState.Wait!.Type,
            WaitKey = stepState.Wait.Key,
            WaitReason = stepState.Wait.Reason,
            ExpectedSignalType = GetExpectedSignalType(stepState.Wait),
            Metadata = metadata
        };
    }

    public static string? GetExpectedSignalType(WaitDescriptor? wait)
    {
        if (wait?.Metadata is null)
        {
            return null;
        }

        return wait.Metadata.TryGetValue("expected_signal_type", out var expected)
            ? expected?.ToString()
            : null;
    }

    private static Dictionary<string, object> CloneDictionary(IDictionary<string, object> values)
    {
        var clone = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            clone[key] = value!;
        }

        return clone;
    }
}
