using System.Diagnostics;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class ProcessRunStep : IProcedoStep
{
    private static readonly HashSet<string> BlockedExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd",
        "cmd.exe",
        "powershell",
        "powershell.exe",
        "pwsh",
        "pwsh.exe",
        "bash",
        "bash.exe",
        "sh",
        "sh.exe"
    };

    private readonly SystemSecurityGuard _securityGuard;

    public ProcessRunStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        var fileName = context.Inputs.TryGetValue("file_name", out var fileValue)
            ? SystemInputReader.GetString(fileValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new StepResult
            {
                Success = false,
                Error = "Input 'file_name' is required."
            };
        }

        var workingDirectory = context.Inputs.TryGetValue("working_directory", out var workingDirectoryValue)
            ? SystemInputReader.GetString(workingDirectoryValue)
            : null;

        var allowUnsafe = context.Inputs.TryGetValue("allow_unsafe_executable", out var allowUnsafeValue)
            && SystemInputReader.GetBool(allowUnsafeValue);

        var securityError = _securityGuard.EnsureProcessAllowed(fileName, allowUnsafe, workingDirectory, BlockedExecutables);
        if (securityError is not null)
        {
            return new StepResult
            {
                Success = false,
                Error = securityError
            };
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        if (context.Inputs.TryGetValue("arguments", out var argumentsValue))
        {
            foreach (var argument in SystemInputReader.GetValues(argumentsValue))
            {
                startInfo.ArgumentList.Add(SystemInputReader.GetString(argument));
            }
        }
        else if (context.Inputs.TryGetValue("arguments_text", out var argumentsTextValue))
        {
            startInfo.Arguments = SystemInputReader.GetString(argumentsTextValue);
        }

        var timeoutMs = context.Inputs.TryGetValue("timeout_ms", out var timeoutValue)
            ? Math.Max(1, SystemInputReader.GetInt(timeoutValue, 30000))
            : 30000;

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var waitTask = process.WaitForExitAsync(context.CancellationToken);
        var completed = await Task.WhenAny(waitTask, Task.Delay(timeoutMs, context.CancellationToken)).ConfigureAwait(false);

        if (completed != waitTask)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return new StepResult
            {
                Success = false,
                Error = $"Process timed out after {timeoutMs}ms."
            };
        }

        await waitTask.ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new StepResult
        {
            Success = process.ExitCode == 0,
            Error = process.ExitCode == 0 ? null : stderr,
            Outputs = new Dictionary<string, object>
            {
                ["exit_code"] = process.ExitCode,
                ["stdout"] = stdout,
                ["stderr"] = stderr,
                ["file_name"] = fileName
            }
        };
    }
}
