using System;

namespace Procedo.Plugin.SDK;

public interface IPluginRegistry
{
    void Register(string stepType, Func<IProcedoStep> factory);

    bool TryRegister(string stepType, Func<IProcedoStep> factory);

    void RegisterOrThrow(string stepType, Func<IProcedoStep> factory);

    bool Contains(string stepType);

    bool TryResolve(string stepType, out IProcedoStep? step);
}
