using System.Collections.Generic;

namespace Procedo.Plugin.SDK;

public interface ILogger
{
    void LogInformation(string message);

    void LogWarning(string message);

    void LogError(string message);
}
