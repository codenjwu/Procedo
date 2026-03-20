using System.Diagnostics;

namespace Procedo.IntegrationTests;

public sealed class EmbeddingExampleProjectSmokeTests
{
    public static IEnumerable<object[]> DirectSmokeProjects()
        => ExampleCatalogInventory.GetProjectEntries()
            .Where(static entry => entry.VerificationMode == ExampleVerificationMode.DirectSmoke)
            .Select(static entry => new object[] { entry.Key, entry.RelativePath });

    public static IEnumerable<object[]> ArgumentSmokeProjects()
    {
        yield return new object[]
        {
            "callback-resume-host",
            "examples/Procedo.Example.CallbackResumeHost/Procedo.Example.CallbackResumeHost.csproj",
            new[]
            {
                "--workflow", "examples/71_callback_resume_identity_demo.yaml",
                "--wait-type", "signal",
                "--wait-key", "callback-identity-demo",
                "--expected-signal", "approve",
                "--signal-type", "approve",
                "--payload-json", "{\"approved_by\":\"arg-smoke\",\"ticket\":\"CHG-711\"}"
            }
        };

        yield return new object[]
        {
            "advanced-observability",
            "examples/Procedo.Example.AdvancedObservability/Procedo.Example.AdvancedObservability.csproj",
            new[]
            {
                "--workflow", "examples/78_template_persisted_resume_observability_demo.yaml",
                "--resume-signal", "approve",
                "--resume-payload-json", "{\"ticket\":\"CHG-781\",\"approved_by\":\"arg-smoke\"}"
            }
        };

        yield return new object[]
        {
            "parity-runner",
            "examples/Procedo.Example.ParityRunner/Procedo.Example.ParityRunner.csproj",
            new[]
            {
                "--workflow", "examples/69_max_parallelism_parity_demo.yaml"
            }
        };

        yield return new object[]
        {
            "policy-host",
            "examples/Procedo.Example.PolicyHost/Procedo.Example.PolicyHost.csproj",
            new[]
            {
                "--artifacts-dir", ".procedo/arg-smoke-policy-host"
            }
        };

        yield return new object[]
        {
            "custom-resolver-store",
            "examples/Procedo.Example.CustomResolverStore/Procedo.Example.CustomResolverStore.csproj",
            new[]
            {
                "--workflow", "examples/71_callback_resume_identity_demo.yaml",
                "--wait-key", "callback-identity-demo",
                "--signal-type", "approve"
            }
        };
    }

    [Theory]
    [MemberData(nameof(DirectSmokeProjects))]
    public async Task DirectSmoke_ProjectExamples_Should_Run_Successfully(string key, string relativePath)
        => await RunProjectAndAssertSuccessAsync(key, relativePath, Array.Empty<string>());

    [Theory]
    [MemberData(nameof(ArgumentSmokeProjects))]
    public async Task ArgumentSmoke_ProjectExamples_Should_Run_Successfully(string key, string relativePath, string[] projectArgs)
        => await RunProjectAndAssertSuccessAsync(key, relativePath, projectArgs);

    private static async Task RunProjectAndAssertSuccessAsync(string key, string relativePath, IReadOnlyList<string> projectArgs)
    {
        var repoRoot = ExampleCatalogInventory.GetRepoRoot();
        var projectPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        foreach (var projectArg in projectArgs)
        {
            startInfo.ArgumentList.Add(projectArg);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var output = await process!.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(
            process.ExitCode == 0,
            $"Project '{key}' failed with exit code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{output}{Environment.NewLine}STDERR:{Environment.NewLine}{error}");
    }
}
