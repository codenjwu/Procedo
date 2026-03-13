using System.Reflection;
using System.Text.Json;

namespace Procedo.UnitTests;

public class RuntimeConfigurationPrecedenceTests
{
    [Fact]
    public void ParseArguments_Should_Apply_Precedence_Defaults_Config_Env_Cli()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-config-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                workflowPath = "examples/hello_pipeline.yaml",
                execution = new
                {
                    defaultMaxParallelism = 2,
                    defaultStepRetries = 1
                }
            });
            File.WriteAllText(tempFile, json);

            var original = Environment.GetEnvironmentVariable("PROCEDO_MAX_PARALLELISM");
            Environment.SetEnvironmentVariable("PROCEDO_MAX_PARALLELISM", "3");
            try
            {
                var options = InvokeParseArguments(new[] { "--config", tempFile, "--max-parallelism", "4" });
                var execution = GetProperty(options, "Execution");
                var maxParallelism = (int)GetProperty(execution, "DefaultMaxParallelism");
                Assert.Equal(4, maxParallelism);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PROCEDO_MAX_PARALLELISM", original);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Use_Env_When_Cli_Does_Not_Override()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-config-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                workflowPath = "examples/hello_pipeline.yaml",
                execution = new
                {
                    defaultMaxParallelism = 2
                }
            });
            File.WriteAllText(tempFile, json);

            var original = Environment.GetEnvironmentVariable("PROCEDO_MAX_PARALLELISM");
            Environment.SetEnvironmentVariable("PROCEDO_MAX_PARALLELISM", "3");
            try
            {
                var options = InvokeParseArguments(new[] { "--config", tempFile });
                var execution = GetProperty(options, "Execution");
                var maxParallelism = (int)GetProperty(execution, "DefaultMaxParallelism");
                Assert.Equal(3, maxParallelism);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PROCEDO_MAX_PARALLELISM", original);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Load_SystemSecurity_From_Config()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-security-config-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                systemSecurity = new
                {
                    allowHttpRequests = false,
                    allowedPathRoots = new[] { ".procedo/runs", ".procedo/artifacts" },
                    allowedExecutables = new[] { "dotnet", "git" }
                }
            });
            File.WriteAllText(tempFile, json);

            var options = InvokeParseArguments(new[] { "--config", tempFile });
            var systemSecurity = GetProperty(options, "SystemSecurity");

            Assert.False((bool)GetProperty(systemSecurity, "AllowHttpRequests"));
            var allowedExecutables = (System.Collections.IEnumerable)GetProperty(systemSecurity, "AllowedExecutables");
            Assert.Contains("dotnet", allowedExecutables.Cast<object>().Select(x => x.ToString()));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Apply_SystemSecurity_Environment_Overrides()
    {
        var original = Environment.GetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_HTTP_HOSTS");
        Environment.SetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_HTTP_HOSTS", "api.contoso.test,localhost");
        try
        {
            var options = InvokeParseArguments(Array.Empty<string>());
            var systemSecurity = GetProperty(options, "SystemSecurity");
            var allowedHttpHosts = (System.Collections.IEnumerable)GetProperty(systemSecurity, "AllowedHttpHosts");
            var values = allowedHttpHosts.Cast<object>().Select(x => x.ToString()).ToArray();

            Assert.Contains("api.contoso.test", values);
            Assert.Contains("localhost", values);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PROCEDO_SYSTEM_ALLOWED_HTTP_HOSTS", original);
        }
    }

    [Fact]
    public void ParseArguments_Should_Read_Resume_Signal_And_Payload_From_Cli()
    {
        var payloadPath = Path.Combine(Path.GetTempPath(), $"procedo-resume-payload-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(payloadPath, "{\"approved_by\":\"operator\",\"attempt\":2}");
            var options = InvokeParseArguments(new[] { "--resume", "run-123", "--resume-signal", "continue", "--resume-payload-json", payloadPath });

            Assert.Equal("run-123", GetProperty(options, "ResumeRunId"));
            Assert.Equal("continue", GetProperty(options, "ResumeSignalType"));
            var payload = (System.Collections.IDictionary)GetProperty(options, "ResumePayload");
            Assert.Equal("operator", payload["approved_by"]?.ToString());
        }
        finally
        {
            if (File.Exists(payloadPath))
            {
                File.Delete(payloadPath);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Reject_ResumeSignal_Without_RunId()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--resume-signal", "continue" }));
        Assert.Contains("resume run id", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Enable_ListWaiting_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--list-waiting", "--state-dir", ".procedo/runs" });

        Assert.True((bool)GetProperty(options, "ListWaiting"));
    }

    [Fact]
    public void ParseArguments_Should_Reject_Executable_AllowList_Entries_That_Are_Paths()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-security-config-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                systemSecurity = new
                {
                    allowedExecutables = new[] { "tools/dotnet" }
                }
            });
            File.WriteAllText(tempFile, json);

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--config", tempFile }));
            Assert.Contains("must be a file name", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Reject_Persistence_When_FileSystem_Access_Is_Disabled()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-security-config-{Guid.NewGuid():N}.json");
        try
        {
            var json = JsonSerializer.Serialize(new
            {
                persist = true,
                systemSecurity = new
                {
                    allowFileSystemAccess = false
                }
            });
            File.WriteAllText(tempFile, json);

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--config", tempFile }));
            Assert.Contains("Persistence requires system file access", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Reject_StateDirectory_Outside_Allowed_Path_Roots()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"procedo-security-config-{Guid.NewGuid():N}.json");
        try
        {
            var root = Path.Combine(Path.GetTempPath(), "procedo-allowed", Guid.NewGuid().ToString("N"));
            var json = JsonSerializer.Serialize(new
            {
                persist = true,
                stateDirectory = Path.Combine(Path.GetTempPath(), "procedo-state-outside", Guid.NewGuid().ToString("N")),
                systemSecurity = new
                {
                    allowFileSystemAccess = true,
                    allowedPathRoots = new[] { root }
                }
            });
            File.WriteAllText(tempFile, json);

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--config", tempFile }));
            Assert.Contains("State directory", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Read_Workflow_Parameters_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "examples/48_template_parameters_demo.yaml", "--param", "environment=prod", "--param", "region=westus" });
        var parameters = (System.Collections.IDictionary)GetProperty(options, "Parameters");

        Assert.Equal("prod", parameters["environment"]?.ToString());
        Assert.Equal("westus", parameters["region"]?.ToString());
    }

    [Fact]
    public void ParseArguments_Should_Read_Typed_Workflow_Parameters_From_Cli_Json_Literals()
    {
        var options = InvokeParseArguments(new[]
        {
            "examples/48_template_parameters_demo.yaml",
            "--param", "enabled=true",
            "--param", "retryCount=3",
            "--param", "ratio=1.5",
            "--param", "targets=[\"eastus\",\"westus\"]",
            "--param", "metadata={\"team\":\"platform\",\"priority\":2}"
        });
        var parameters = (System.Collections.IDictionary)GetProperty(options, "Parameters");

        Assert.Equal(true, parameters["enabled"]);
        Assert.Equal(3L, parameters["retryCount"]);
        Assert.Equal(1.5d, parameters["ratio"]);
        var targets = Assert.IsAssignableFrom<System.Collections.IEnumerable>(parameters["targets"]);
        Assert.Equal(new[] { "eastus", "westus" }, targets.Cast<object>().Select(static x => x.ToString()).ToArray());
        var metadata = Assert.IsAssignableFrom<System.Collections.IDictionary>(parameters["metadata"]);
        Assert.Equal("platform", metadata["team"]?.ToString());
    }

    [Fact]
    public void ParseArguments_Should_Read_Workflow_Parameters_From_Cli_Json_File()
    {
        var payloadPath = Path.Combine(Path.GetTempPath(), $"procedo-parameter-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(payloadPath, "{\"team\":\"platform\",\"rings\":[\"canary\",\"prod\"]}");
            var options = InvokeParseArguments(new[]
            {
                "examples/48_template_parameters_demo.yaml",
                "--param", $"deployment=@{payloadPath}"
            });
            var parameters = (System.Collections.IDictionary)GetProperty(options, "Parameters");
            var deployment = Assert.IsAssignableFrom<System.Collections.IDictionary>(parameters["deployment"]);
            Assert.Equal("platform", deployment["team"]?.ToString());
        }
        finally
        {
            if (File.Exists(payloadPath))
            {
                File.Delete(payloadPath);
            }
        }
    }

    [Fact]
    public void ParseArguments_Should_Read_ShowRunId_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--show-run", "run-456", "--state-dir", ".procedo/runs" });

        Assert.Equal("run-456", GetProperty(options, "ShowRunId"));
    }

    [Fact]
    public void ParseArguments_Should_Read_DeleteCompleted_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--delete-completed", "--state-dir", ".procedo/runs" });

        Assert.True((bool)GetProperty(options, "DeleteCompleted"));
    }

    [Fact]
    public void ParseArguments_Should_Read_DeleteFailed_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--delete-failed", "--state-dir", ".procedo/runs" });

        Assert.True((bool)GetProperty(options, "DeleteFailed"));
    }

    [Fact]
    public void ParseArguments_Should_Read_DeleteAllOlderThan_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--delete-all-older-than", "00:30:00", "--state-dir", ".procedo/runs" });

        Assert.Equal(TimeSpan.FromMinutes(30), GetProperty(options, "DeleteAllOlderThan"));
    }

    [Fact]
    public void ParseArguments_Should_Read_DeleteWaitingOlderThan_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--delete-waiting-older-than", "00:45:00", "--state-dir", ".procedo/runs" });

        Assert.Equal(TimeSpan.FromMinutes(45), GetProperty(options, "DeleteWaitingOlderThan"));
    }
    [Fact]
    public void ParseArguments_Should_Read_DeleteRunId_From_Cli()
    {
        var options = InvokeParseArguments(new[] { "--delete-run", "run-123", "--state-dir", ".procedo/runs" });

        Assert.Equal("run-123", GetProperty(options, "DeleteRunId"));
    }

    [Fact]
    public void ParseArguments_Should_Reject_ListWaiting_And_DeleteRun_Together()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--list-waiting", "--delete-run", "run-123" }));
        Assert.Contains("cannot be used together", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_ListWaiting_And_ShowRun_Together()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--list-waiting", "--show-run", "run-123" }));
        Assert.Contains("cannot be used together", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_DeleteRun_And_ShowRun_Together()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--delete-run", "run-123", "--show-run", "run-456" }));
        Assert.Contains("cannot be used together", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_Multiple_Bulk_Delete_Filters()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--delete-completed", "--delete-failed" }));
        Assert.Contains("Only one bulk delete filter", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_Bulk_Delete_With_ShowRun()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--delete-completed", "--show-run", "run-1" }));
        Assert.Contains("Bulk delete options", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_Invalid_DeleteAllOlderThan_Value()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--delete-all-older-than", "soon" }));
        Assert.Contains("positive timespan", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArguments_Should_Reject_DeleteWaitingOlderThan_Combined_With_Another_Bulk_Filter()
    {
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseArguments(new[] { "--delete-waiting-older-than", "00:30:00", "--delete-failed" }));
        Assert.Contains("Only one bulk delete filter", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }
    private static object InvokeParseArguments(string[] args)
    {
        var method = typeof(Procedo.Runtime.Program).GetMethod("ParseArguments", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return method!.Invoke(null, new object[] { args })!;
    }

    private static object GetProperty(object target, string propertyName)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        return prop!.GetValue(target)!;
    }
}













