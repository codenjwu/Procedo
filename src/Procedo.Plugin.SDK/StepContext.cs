using System.Collections.Generic;
using System.Threading;
using Procedo.Core.Runtime;

namespace Procedo.Plugin.SDK;

public sealed class StepContext
{
    public string RunId { get; set; } = string.Empty;

    public string StepId { get; set; } = string.Empty;

    public IDictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();

    public IDictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();

    public ILogger Logger { get; set; } = new ConsoleLogger();

    public CancellationToken CancellationToken { get; set; }

    public ResumeRequest? Resume { get; set; }
}
