using Procedo.Core.Abstractions;
using Procedo.Core.Execution;
using Procedo.DSL;
using Procedo.Observability;
using Procedo.Plugin.SDK;
using Procedo.Validation.Models;

namespace Procedo.Engine.Hosting;

public sealed class ProcedoHostOptions
{
    public ILogger Logger { get; set; } = new ConsoleLogger();

    public IExecutionEventSink? EventSink { get; set; }

    public WorkflowExecutionOptions Execution { get; } = WorkflowExecutionOptions.Default;

    public ValidationOptions Validation { get; } = new();

    public IRunStateStore? RunStateStore { get; set; }

    public IWorkflowDefinitionResolver? WorkflowDefinitionResolver { get; set; }

    public string? ResumeRunId { get; set; }

    public bool SkipValidation { get; set; }

    public IWorkflowParser Parser { get; set; } = new YamlWorkflowParser();
}
