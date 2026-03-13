namespace Procedo.Observability;

public enum ExecutionEventType
{
    RunStarted,
    RunCompleted,
    RunFailed,
    StepStarted,
    StepCompleted,
    StepFailed,
    StepSkipped,
    StepWaiting,
    RunWaiting,
    RunResumed
}
