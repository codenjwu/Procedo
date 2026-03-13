using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Procedo.Core.Runtime;

namespace Procedo.Core.Abstractions;

public interface IRunStateStore
{
    Task<WorkflowRunState?> GetRunAsync(string runId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRunState>> ListRunsAsync(CancellationToken cancellationToken = default);

    Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default);

    Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default);
}
