using System.Collections.Generic;

namespace Procedo.Core.Models;

public sealed class StageDefinition
{
    public string Stage { get; set; } = string.Empty;

    public List<JobDefinition> Jobs { get; set; } = new();
}
