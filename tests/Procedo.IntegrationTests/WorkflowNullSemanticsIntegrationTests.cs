using System.Text.Json;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;
using Procedo.Persistence.Stores;

namespace Procedo.IntegrationTests;

public sealed class WorkflowNullSemanticsIntegrationTests
{
    [Fact]
    public async Task Example_63_NullSemanticsShowcase_Should_Preserve_Distinct_Null_Empty_And_String_Values()
    {
        var workflow = LoadWorkflow("63_null_semantics_showcase.yaml");
        var tempRoot = CreateTempDirectory("procedo-null-showcase");
        var outputFile = Path.Combine(tempRoot, "null-semantics.json");
        OverrideWorkspace(workflow, tempRoot, outputFile);

        try
        {
            var result = await ExecuteAsync(workflow);

            Assert.True(result.Success, result.Error);

            using var json = OpenJson(outputFile);
            var values = json.RootElement.GetProperty("values");
            Assert.Equal(JsonValueKind.Null, values.GetProperty("explicit_null").ValueKind);
            Assert.Equal(JsonValueKind.Null, values.GetProperty("tilde_null").ValueKind);
            Assert.Equal(string.Empty, values.GetProperty("blank_text").GetString());
            Assert.Equal("null", values.GetProperty("literal_null_text").GetString());

            var payload = values.GetProperty("object_payload");
            Assert.Equal(JsonValueKind.Null, payload.GetProperty("owner").ValueKind);
            Assert.Equal("platform", payload.GetProperty("team").GetString());
            Assert.Equal("null", payload.GetProperty("marker").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_64_TemplateNullOverrideDemo_Should_Preserve_Null_Overrides()
    {
        var workflow = LoadTemplatedWorkflow("64_template_null_override_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-null-template");
        var outputFile = Path.Combine(tempRoot, "template-null-semantics.json");
        OverrideWorkspace(workflow, tempRoot, outputFile);

        try
        {
            var result = await ExecuteAsync(workflow);

            Assert.True(result.Success, result.Error);

            using var json = OpenJson(outputFile);
            var values = json.RootElement.GetProperty("values");
            Assert.Equal(JsonValueKind.Null, values.GetProperty("optional_note").ValueKind);
            Assert.Equal(string.Empty, values.GetProperty("blankable_note").GetString());

            var metadata = values.GetProperty("metadata");
            Assert.Equal(JsonValueKind.Null, metadata.GetProperty("owner").ValueKind);
            Assert.Equal("stable", metadata.GetProperty("channel").GetString());
            Assert.Equal("null", metadata.GetProperty("marker").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_65_PersistedNullResumeDemo_Should_RoundTrip_Nulls_Across_Wait_Resume()
    {
        var workflow = LoadWorkflow("65_persisted_null_resume_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-null-resume");
        var outputFile = Path.Combine(tempRoot, "persisted-null-semantics.json");
        OverrideWorkspace(workflow, tempRoot, outputFile);
        var storeRoot = Path.Combine(tempRoot, "state");
        Directory.CreateDirectory(storeRoot);

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(storeRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: "null-resume");

            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var waitingState = await store.GetRunAsync("null-resume");
            Assert.NotNull(waitingState);
            Assert.True(waitingState!.WorkflowParameters.ContainsKey("approval_note"));
            Assert.Null(waitingState.WorkflowParameters["approval_note"]);
            Assert.Equal(string.Empty, waitingState.WorkflowParameters["blank_override"]);
            Assert.Equal("null", waitingState.WorkflowParameters["literal_null_text"]);

            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "null-resume",
                new ResumeRequest { SignalType = "continue" });

            Assert.True(resumed.Success, resumed.Error);

            using var json = OpenJson(outputFile);
            var persisted = json.RootElement.GetProperty("persisted");
            Assert.Equal(JsonValueKind.Null, persisted.GetProperty("approval_note").ValueKind);
            Assert.Equal(string.Empty, persisted.GetProperty("blank_override").GetString());
            Assert.Equal("null", persisted.GetProperty("literal_null_text").GetString());

            var finalState = await store.GetRunAsync("null-resume");
            Assert.NotNull(finalState);
            Assert.Equal(RunStatus.Completed, finalState!.Status);
            Assert.Null(finalState.WorkflowParameters["approval_note"]);
            Assert.Equal(string.Empty, finalState.WorkflowParameters["blank_override"]);
            Assert.Equal("null", finalState.WorkflowParameters["literal_null_text"]);
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    private static WorkflowDefinition LoadWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        var yaml = File.ReadAllText(path);
        return new YamlWorkflowParser().Parse(yaml);
    }

    private static WorkflowDefinition LoadTemplatedWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        return new WorkflowTemplateLoader().LoadFromFile(path);
    }

    private static void OverrideWorkspace(WorkflowDefinition workflow, string workspaceRoot, string outputFile)
    {
        workflow.Variables["workspace"] = workspaceRoot;
        workflow.Variables["output_file"] = outputFile;
    }

    private static async Task<WorkflowRunResult> ExecuteAsync(WorkflowDefinition workflow)
    {
        var engine = new ProcedoWorkflowEngine();
        return await engine.ExecuteAsync(workflow, CreateRegistry(), new NullLogger());
    }

    private static IPluginRegistry CreateRegistry()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
        return registry;
    }

    private static JsonDocument OpenJson(string path)
        => JsonDocument.Parse(File.ReadAllText(path));

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

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
