using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class WaitDescriptor
{
    public string Type { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public string? Key { get; set; }

    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
