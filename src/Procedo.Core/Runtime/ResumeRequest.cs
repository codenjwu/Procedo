using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class ResumeRequest
{
    public string? SignalType { get; set; }

    public IDictionary<string, object> Payload { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
