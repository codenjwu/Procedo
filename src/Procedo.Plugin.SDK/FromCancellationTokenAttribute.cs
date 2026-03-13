using System;

namespace Procedo.Plugin.SDK;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class FromCancellationTokenAttribute : Attribute
{
}
