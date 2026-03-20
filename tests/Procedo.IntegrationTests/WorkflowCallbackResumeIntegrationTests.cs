using System.Text.Json;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public sealed class WorkflowCallbackResumeIntegrationTests
{
    [Fact]
    public async Task Example_71_CallbackResumeIdentityDemo_Should_Query_And_Resume_By_Wait_Identity()
    {
        var root = CreateTempDirectory("procedo-callback-identity");
        var workflowPath = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "71_callback_resume_identity_demo.yaml");
        var outputFile = Path.Combine(root, "callback-resume-identity.json");

        try
        {
            var host = CreateHost(root);
            var workflow = LoadWorkflow(workflowPath, root, outputFile);

            var first = await host.ExecuteWorkflowAsync(workflow);

            Assert.False(first.Success);
            Assert.True(first.Waiting);

            var waits = await host.FindWaitingRunsAsync(new WaitingRunQuery
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo",
                ExpectedSignalType = "approve"
            });

            var wait = Assert.Single(waits);
            Assert.Equal("callback", wait.Stage);
            Assert.Equal("approval", wait.Job);
            Assert.Equal("wait_for_callback", wait.StepId);
            Assert.Equal("approve", wait.ExpectedSignalType);

            var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo",
                SignalType = "approve",
                Payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["approved_by"] = "callback-bot"
                }
            });

            Assert.True(resumed.Success, resumed.Error);
            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var result = json.RootElement.GetProperty("result");
            Assert.Equal("approve", result.GetProperty("signal_type").GetString());
            Assert.Equal("callback-bot", result.GetProperty("payload").GetProperty("approved_by").GetString());
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Example_71_CallbackResumeIdentityDemo_Should_Fail_On_Duplicate_Match_By_Default_And_Resume_Newest_When_Requested()
    {
        var root = CreateTempDirectory("procedo-callback-duplicate");
        var workflowPath = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "71_callback_resume_identity_demo.yaml");

        try
        {
            var host = CreateHost(root);

            Assert.True((await host.ExecuteWorkflowAsync(LoadWorkflow(workflowPath, root, Path.Combine(root, "a.json")))).Waiting);
            await Task.Delay(20);
            Assert.True((await host.ExecuteWorkflowAsync(LoadWorkflow(workflowPath, root, Path.Combine(root, "b.json")))).Waiting);

            var duplicate = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo",
                SignalType = "approve"
            });

            Assert.False(duplicate.Success);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, duplicate.ErrorCode);

            var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo",
                SignalType = "approve",
                MatchBehavior = WaitingRunMatchBehavior.ResumeNewest
            });

            Assert.True(resumed.Success, resumed.Error);

            var remaining = await host.FindWaitingRunsAsync(new WaitingRunQuery
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo"
            });

            Assert.Single(remaining);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Example_71_CallbackResumeIdentityDemo_Should_Reject_Wrong_Signal_Type()
    {
        var root = CreateTempDirectory("procedo-callback-signal");
        var workflowPath = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "71_callback_resume_identity_demo.yaml");

        try
        {
            var host = CreateHost(root);

            Assert.True((await host.ExecuteWorkflowAsync(LoadWorkflow(workflowPath, root, Path.Combine(root, "signal.json")))).Waiting);

            var result = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-identity-demo",
                SignalType = "reject"
            });

            Assert.False(result.Success);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, result.ErrorCode);
            Assert.Contains("expects signal type", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Example_72_CallbackResumeTwoCycleDemo_Should_Support_Two_Identity_Based_Resume_Cycles()
    {
        var root = CreateTempDirectory("procedo-callback-two-cycle");
        var workflowPath = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "72_callback_resume_two_cycle_demo.yaml");
        var outputFile = Path.Combine(root, "callback-resume-two-cycle.json");

        try
        {
            var host = CreateHost(root);
            var workflow = LoadWorkflow(workflowPath, root, outputFile);

            var first = await host.ExecuteWorkflowAsync(workflow);

            Assert.True(first.Waiting);

            var second = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-cycle-1",
                SignalType = "continue-1",
                Payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["cycle"] = 1
                }
            });

            Assert.False(second.Success);
            Assert.True(second.Waiting);

            var waiting = await host.FindWaitingRunsAsync(new WaitingRunQuery
            {
                WaitType = "signal",
                WaitKey = "callback-cycle-2"
            });

            Assert.Single(waiting);
            Assert.Equal("continue-2", waiting[0].ExpectedSignalType);

            var third = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-cycle-2",
                SignalType = "continue-2",
                Payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["cycle"] = 2
                }
            });

            Assert.True(third.Success, third.Error);

            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var cycles = json.RootElement.GetProperty("cycles");
            Assert.Equal("continue-1", cycles.GetProperty("first").GetProperty("signal_type").GetString());
            Assert.Equal(1, cycles.GetProperty("first").GetProperty("payload").GetProperty("cycle").GetInt32());
            Assert.Equal("continue-2", cycles.GetProperty("second").GetProperty("signal_type").GetString());
            Assert.Equal(2, cycles.GetProperty("second").GetProperty("payload").GetProperty("cycle").GetInt32());
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact]
    public async Task Example_73_CallbackResumeSnapshotSafetyDemo_Should_Use_Persisted_Workflow_Snapshot_On_Identity_Based_Resume()
    {
        var root = CreateTempDirectory("procedo-callback-snapshot");
        var workflowPath = Path.Combine(root, "73_callback_resume_snapshot_safety_demo.yaml");
        File.Copy(
            Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "73_callback_resume_snapshot_safety_demo.yaml"),
            workflowPath);
        var outputFile = Path.Combine(root, "callback-resume-snapshot.json");

        try
        {
            var host = CreateHost(root);
            var workflow = LoadWorkflow(workflowPath, root, outputFile);

            var first = await host.ExecuteWorkflowAsync(workflow);

            Assert.True(first.Waiting);

            var updatedYaml = File.ReadAllText(workflowPath).Replace("original-snapshot", "changed-snapshot", StringComparison.Ordinal);
            File.WriteAllText(workflowPath, updatedYaml);

            var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "callback-snapshot-demo",
                SignalType = "approve"
            });

            Assert.True(resumed.Success, resumed.Error);
            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var result = json.RootElement.GetProperty("result");
            Assert.Equal("original-snapshot", result.GetProperty("marker").GetString());
            Assert.Equal("approve", result.GetProperty("signal_type").GetString());
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static ProcedoHost CreateHost(string stateDirectory)
        => new ProcedoHostBuilder()
            .ConfigurePlugins(static registry =>
            {
                registry.AddSystemPlugin();
                registry.AddDemoPlugin();
            })
            .UseLocalRunStateStore(stateDirectory)
            .Build();

    private static WorkflowDefinition LoadWorkflow(string workflowPath, string workspaceRoot, string outputFile)
    {
        var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);
        workflow.Variables["workspace"] = workspaceRoot;
        workflow.Variables["output_file"] = outputFile;
        return workflow;
    }

    private static string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, true); } catch { }
    }
}
