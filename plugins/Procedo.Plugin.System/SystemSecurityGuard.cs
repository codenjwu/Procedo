namespace Procedo.Plugin.System;

internal sealed class SystemSecurityGuard
{
    private readonly SystemPluginSecurityOptions _options;

    public SystemSecurityGuard(SystemPluginSecurityOptions? options)
    {
        _options = options ?? new SystemPluginSecurityOptions();
    }

    public string? EnsureHttpAllowed(string url)
    {
        if (!_options.AllowHttpRequests)
        {
            return "HTTP requests are disabled by system plugin security policy.";
        }

        if (_options.AllowedHttpHosts.Count == 0)
        {
            return null;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return $"HTTP url '{url}' is not a valid absolute URI.";
        }

        foreach (var allowedHost in _options.AllowedHttpHosts)
        {
            if (string.Equals(uri.Host, allowedHost, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return $"HTTP host '{uri.Host}' is not allowed by system plugin security policy.";
    }

    public string? EnsureProcessAllowed(string fileName, bool allowUnsafeRequested, string? workingDirectory, IReadOnlySet<string> blockedExecutables)
    {
        if (!_options.AllowProcessExecution)
        {
            return "Process execution is disabled by system plugin security policy.";
        }

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            var directoryError = EnsurePathAllowed(workingDirectory, "working_directory");
            if (directoryError is not null)
            {
                return directoryError;
            }
        }

        var executableName = Path.GetFileName(fileName);
        if (_options.AllowedExecutables.Count > 0
            && !_options.AllowedExecutables.Any(x => string.Equals(x, executableName, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Executable '{executableName}' is not allowed by system plugin security policy.";
        }

        var allowUnsafe = allowUnsafeRequested || _options.AllowUnsafeExecutables;
        if (!allowUnsafe && blockedExecutables.Contains(executableName))
        {
            return $"Executable '{executableName}' is blocked by default. Set allow_unsafe_executable=true or update system plugin security options to override.";
        }

        return null;
    }

    public string? EnsurePathAllowed(string path, string inputName)
    {
        if (!_options.AllowFileSystemAccess)
        {
            return $"File system access is disabled by system plugin security policy for input '{inputName}'.";
        }

        if (_options.AllowedPathRoots.Count == 0)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(path);
        foreach (var root in _options.AllowedPathRoots)
        {
            var fullRoot = Path.GetFullPath(root);
            if (IsSameOrChildPath(fullPath, fullRoot))
            {
                return null;
            }
        }

        return $"Path '{fullPath}' is outside the allowed system plugin roots.";
    }

    private static bool IsSameOrChildPath(string fullPath, string fullRoot)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var normalizedPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRoot = fullRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.Equals(normalizedPath, normalizedRoot, comparison))
        {
            return true;
        }

        var rootWithSeparator = normalizedRoot + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(rootWithSeparator, comparison)
            || normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);
    }
}
