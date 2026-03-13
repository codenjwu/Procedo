namespace Procedo.Expressions;

public sealed class WorkflowContextResolutionException : Exception
{
    public WorkflowContextResolutionException(string message)
        : base(message)
    {
    }
}
