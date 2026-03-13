using System;
using System.Collections.Generic;
using System.Linq;
using Procedo.Core.Models;

namespace Procedo.Engine.Graph;

public sealed class ExecutionGraphBuilder
{
    public IReadOnlyDictionary<string, ExecutionNode> Build(JobDefinition job)
    {
        var nodes = job.Steps.ToDictionary(s => s.Step, s => new ExecutionNode(s), StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes.Values)
        {
            foreach (var dependsOn in node.Step.DependsOn)
            {
                if (!nodes.TryGetValue(dependsOn, out var dependency))
                {
                    throw new InvalidOperationException(
                        $"Step '{node.Step.Step}' depends on unknown step '{dependsOn}'.");
                }

                node.Dependencies.Add(dependency);
            }
        }

        return nodes;
    }
}
