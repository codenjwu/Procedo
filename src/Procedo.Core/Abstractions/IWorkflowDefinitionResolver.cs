using System.Threading;
using System.Threading.Tasks;
using Procedo.Core.Models;
using Procedo.Core.Runtime;

namespace Procedo.Core.Abstractions;

public interface IWorkflowDefinitionResolver
{
    Task<WorkflowDefinition> ResolveAsync(PersistedWorkflowReference reference, CancellationToken cancellationToken = default);
}
