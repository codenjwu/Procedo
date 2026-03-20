using Procedo.Core.Abstractions;
using Procedo.Core.Runtime;

namespace Procedo.ContractTests;

public sealed class RunStateStoreCompatibilityCrossTargetContractTests
{
    [Fact]
    public void LegacyRunStateStore_Can_Implement_IRunStateStore_Without_CallbackInterfaces()
    {
        IRunStateStore store = new LegacyRunStateStore();

        Assert.False(store is IWaitingRunQueryStore);
        Assert.False(store is IConditionalRunStateStore);
    }

    private sealed class LegacyRunStateStore : IRunStateStore
    {
        public Task<WorkflowRunState?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
            => Task.FromResult<WorkflowRunState?>(null);

        public Task<IReadOnlyList<WorkflowRunState>> ListRunsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkflowRunState>>(Array.Empty<WorkflowRunState>());

        public Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
