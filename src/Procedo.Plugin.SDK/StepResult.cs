using System.Collections.Generic;
using Procedo.Core.Runtime;

namespace Procedo.Plugin.SDK;

public sealed class StepResult
{
    public bool Success { get; set; }

    public bool Waiting { get; set; }

    public IDictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();

    public string? Error { get; set; }

    public WaitDescriptor? Wait { get; set; }
}
