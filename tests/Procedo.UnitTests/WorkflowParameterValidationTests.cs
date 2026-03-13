using System.Linq;
using Procedo.Core.Models;
using Procedo.DSL;
using Procedo.Plugin.SDK;
using Procedo.Validation;

namespace Procedo.UnitTests;

public class WorkflowParameterValidationTests
{
    [Fact]
    public void Validate_Should_Report_Missing_Required_Parameter()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", Required = true }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Issues, issue => issue.Code == "PV011");
    }

    [Fact]
    public void Validate_Should_Report_Variable_Cycle()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["a"] = "${vars.b}",
                ["b"] = "${vars.a}"
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Issues, issue => issue.Code == "PV020");
    }

    [Fact]
    public void Validate_Should_Assign_Template_SourcePath_For_Stage_Level_Issues()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, "name: base\nversion: 1\nstages:\n- stage: build\n  jobs:\n  - job: flow\n    steps:\n    - step: announce\n      type: missing.plugin\n");

            var childPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(childPath, "template: ./base.yaml\nname: child\n");

            var workflow = new WorkflowTemplateLoader().LoadFromFile(childPath);
            var result = new ProcedoWorkflowValidator().Validate(workflow, new PluginRegistry());
            var issue = Assert.Single(result.Issues.Where(static x => x.Code == "PV304"));

            Assert.Equal(templatePath, issue.SourcePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Validate_Should_Assign_Child_SourcePath_For_Supplied_Parameter_Issues()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, "name: base\nversion: 1\nparameters:\n  environment:\n    type: string\n    required: true\nstages:\n- stage: build\n  jobs:\n  - job: flow\n    steps:\n    - step: announce\n      type: system.echo\n      with:\n        message: hi\n");

            var childPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(childPath, "template: ./base.yaml\nparameters:\n  extra: value\n");

            var workflow = new WorkflowTemplateLoader().LoadFromFile(childPath);
            var result = new ProcedoWorkflowValidator().Validate(workflow);
            var issue = Assert.Single(result.Issues.Where(static x => x.Code == "PV013"));

            Assert.Equal(childPath, issue.SourcePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Validate_Should_Report_Invalid_Parameter_Schema_Constraints()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", MinLength = 10, MaxLength = 3 },
                ["retry_count"] = new() { Type = "int", Minimum = 10, Maximum = 2 },
                ["metadata"] = new() { Type = "string", RequiredProperties = { "team" } },
                ["targets"] = new() { Type = "string", ItemType = "int" }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Issues, issue => issue.Code == "PV024");
        Assert.Contains(result.Issues, issue => issue.Code == "PV022");
        Assert.Contains(result.Issues, issue => issue.Code == "PV027");
        Assert.Contains(result.Issues, issue => issue.Code == "PV026");
    }

    [Fact]
    public void Validate_Should_Report_Parameter_Value_Constraint_Violations()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", AllowedValues = { "dev", "prod" } },
                ["metadata"] = new() { Type = "object", RequiredProperties = { "team" } }
            },
            ParameterValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = "test",
                ["metadata"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Issues, issue => issue.Code == "PV014" && issue.Path == "parameters.environment");
        Assert.Contains(result.Issues, issue => issue.Code == "PV014" && issue.Path == "parameters.metadata");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-validation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
