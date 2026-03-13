using System;

namespace Procedo.Plugin.SDK;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class StepInputAttribute : Attribute
{
    public StepInputAttribute(string name)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Input name is required.", nameof(name))
            : name;
    }

    public string Name { get; }
}
