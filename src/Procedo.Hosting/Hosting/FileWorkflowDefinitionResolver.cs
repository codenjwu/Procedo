using Procedo.Core.Abstractions;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;

namespace Procedo.Engine.Hosting;

public sealed class FileWorkflowDefinitionResolver : IWorkflowDefinitionResolver
{
    private readonly WorkflowTemplateLoader _loader = new();

    public Task<WorkflowDefinition> ResolveAsync(PersistedWorkflowReference reference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (reference is null)
        {
            throw new ArgumentNullException(nameof(reference));
        }

        if (!string.IsNullOrWhiteSpace(reference.WorkflowDefinitionSnapshot))
        {
            if (!WorkflowDefinitionSnapshotCodec.MatchesFingerprint(
                reference.WorkflowDefinitionSnapshot,
                reference.WorkflowDefinitionFingerprint))
            {
                throw new InvalidOperationException(
                    $"Run '{reference.RunId}' contains a workflow snapshot whose fingerprint does not match the persisted workflow identity.");
            }

            return Task.FromResult(WorkflowDefinitionSnapshotCodec.Deserialize(reference.WorkflowDefinitionSnapshot));
        }

        if (string.IsNullOrWhiteSpace(reference.WorkflowSourcePath))
        {
            throw new InvalidOperationException(
                $"Run '{reference.RunId}' does not contain a persisted workflow snapshot and cannot be resolved automatically.");
        }

        throw new InvalidOperationException(
            $"Run '{reference.RunId}' predates persisted workflow snapshots. Use resume-by-runId with the original workflow definition or provide a custom workflow resolver.");
    }
}
