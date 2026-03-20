using Procedo.Core.Models;
using Procedo.Core.Runtime;

namespace Procedo.Core.Abstractions;

public static class RunStateStoreExtensions
{
    public static async Task<IReadOnlyList<ActiveWaitState>> FindWaitingRunsCompatAsync(
        this IRunStateStore store,
        WaitingRunQuery query,
        CancellationToken cancellationToken = default)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (store is IWaitingRunQueryStore queryStore)
        {
            return await queryStore.FindWaitingRunsAsync(query, cancellationToken).ConfigureAwait(false);
        }

        var matches = new List<ActiveWaitState>();
        var runs = await store.ListRunsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var run in runs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (run.Status != RunStatus.Waiting)
            {
                continue;
            }

            var wait = ActiveWaitStateProjector.TryProject(run, query.IncludeMetadata);
            if (wait is null || !Matches(wait, query))
            {
                continue;
            }

            matches.Add(wait);
        }

        matches.Sort(CompareNewestFirst);
        if (query.Limit is > 0 && matches.Count > query.Limit.Value)
        {
            return matches.Take(query.Limit.Value).ToArray();
        }

        return matches;
    }

    public static async Task<bool> TrySaveRunCompatAsync(
        this IRunStateStore store,
        WorkflowRunState runState,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        if (store is null)
        {
            throw new ArgumentNullException(nameof(store));
        }

        if (runState is null)
        {
            throw new ArgumentNullException(nameof(runState));
        }

        if (store is IConditionalRunStateStore conditionalStore)
        {
            return await conditionalStore.TrySaveRunAsync(runState, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        await store.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public static bool SupportsConditionalSave(this IRunStateStore store)
        => store is IConditionalRunStateStore;

    private static bool Matches(ActiveWaitState wait, WaitingRunQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.WorkflowName)
            && !string.Equals(wait.WorkflowName, query.WorkflowName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.WaitType)
            && !string.Equals(wait.WaitType, query.WaitType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.WaitKey)
            && !string.Equals(wait.WaitKey, query.WaitKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.StepId)
            && !string.Equals(wait.StepId, query.StepId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ExpectedSignalType)
            && !string.Equals(wait.ExpectedSignalType, query.ExpectedSignalType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static int CompareNewestFirst(ActiveWaitState left, ActiveWaitState right)
    {
        var waitingSince = Nullable.Compare(right.WaitingSinceUtc, left.WaitingSinceUtc);
        if (waitingSince != 0)
        {
            return waitingSince;
        }

        return string.Compare(left.RunId, right.RunId, StringComparison.OrdinalIgnoreCase);
    }
}
