using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Procedo.Core.Runtime;

namespace Procedo.Core.Abstractions;

public interface IWaitingRunQueryStore
{
    Task<IReadOnlyList<ActiveWaitState>> FindWaitingRunsAsync(WaitingRunQuery query, CancellationToken cancellationToken = default);
}
