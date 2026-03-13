namespace Procedo.Observability;

public static class ExecutionEventGuards
{
    public static bool TryValidateRequiredFields(ExecutionEvent executionEvent, out string? error)
    {
        if (executionEvent is null)
        {
            error = "Execution event is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(executionEvent.RunId))
        {
            error = "RunId is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(executionEvent.WorkflowName))
        {
            error = "WorkflowName is required.";
            return false;
        }

        if (executionEvent.EventType is ExecutionEventType.StepStarted or ExecutionEventType.StepCompleted or ExecutionEventType.StepFailed or ExecutionEventType.StepSkipped or ExecutionEventType.StepWaiting)
        {
            if (string.IsNullOrWhiteSpace(executionEvent.Stage)
                || string.IsNullOrWhiteSpace(executionEvent.Job)
                || string.IsNullOrWhiteSpace(executionEvent.StepId)
                || string.IsNullOrWhiteSpace(executionEvent.StepType))
            {
                error = "Step scope fields are required for step events.";
                return false;
            }
        }

        error = null;
        return true;
    }
}

