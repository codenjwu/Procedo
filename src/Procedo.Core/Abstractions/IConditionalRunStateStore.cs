using System.Threading;
using System.Threading.Tasks;
using Procedo.Core.Runtime;

namespace Procedo.Core.Abstractions;

public interface IConditionalRunStateStore
{
    Task<bool> TrySaveRunAsync(WorkflowRunState runState, long expectedVersion, CancellationToken cancellationToken = default);
}
