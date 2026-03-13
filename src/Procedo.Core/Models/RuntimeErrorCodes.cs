namespace Procedo.Core.Models;

public static class RuntimeErrorCodes
{
    public const string None = "PR000";
    public const string JobFailed = "PR100";
    public const string PluginNotFound = "PR101";
    public const string StepResultFailed = "PR102";
    public const string StepException = "PR103";
    public const string StepTimeout = "PR104";
    public const string Cancelled = "PR105";
    public const string DependencyBlocked = "PR106";
    public const string SchedulerDeadlock = "PR107";
    public const string Waiting = "PR108";
    public const string InvalidResume = "PR109";
    public const string WorkflowLoadFailed = "PR200";
    public const string ValidationFailed = "PR201";
    public const string ConfigurationInvalid = "PR202";
    public const string WorkflowFileNotFound = "PR203";
}
