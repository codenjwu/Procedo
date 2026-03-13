using Procedo.Core.Models;

namespace Procedo.Core.Abstractions;

public interface IWorkflowParser
{
    WorkflowDefinition Parse(string yamlText);
}
