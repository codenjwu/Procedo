using System.Threading.Tasks;

namespace Procedo.Plugin.SDK;

public interface IProcedoStep
{
    Task<StepResult> ExecuteAsync(StepContext context);
}
