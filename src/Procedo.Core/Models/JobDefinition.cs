using System.Collections.Generic;

namespace Procedo.Core.Models;

public sealed class JobDefinition
{
    public string Job { get; set; } = string.Empty;

    public int? MaxParallelism { get; set; }

    public bool? ContinueOnError { get; set; }

    public List<StepDefinition> Steps { get; set; } = new();
}
