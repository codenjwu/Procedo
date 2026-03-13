namespace Procedo.Plugin.System;

public sealed class SystemPluginSecurityOptions
{
    public bool AllowHttpRequests { get; set; } = true;

    public bool AllowFileSystemAccess { get; set; } = true;

    public bool AllowProcessExecution { get; set; } = true;

    public bool AllowUnsafeExecutables { get; set; }

    public IList<string> AllowedPathRoots { get; } = new List<string>();

    public IList<string> AllowedHttpHosts { get; } = new List<string>();

    public IList<string> AllowedExecutables { get; } = new List<string>();
}
