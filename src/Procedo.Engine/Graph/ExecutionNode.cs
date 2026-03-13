using System.Collections.Generic;
using Procedo.Core.Execution;
using Procedo.Core.Models;

namespace Procedo.Engine.Graph;

public sealed class ExecutionNode
{
    public ExecutionNode(StepDefinition step)
    {
        Step = step;
    }

    public StepDefinition Step { get; }

    public NodeState State { get; set; } = NodeState.Pending;

    public List<ExecutionNode> Dependencies { get; } = new();

    public Dictionary<string, object> Outputs { get; } = new();

    public string? Error { get; set; }

    public string? ErrorCode { get; set; }
}
