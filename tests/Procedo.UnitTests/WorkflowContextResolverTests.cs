using Procedo.Core.Models;
using Procedo.Expressions;

namespace Procedo.UnitTests;

public class WorkflowContextResolverTests
{
    [Fact]
    public void BuildInitialVariables_Should_Resolve_Params_And_Workflow_Variables()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", Required = true },
                ["retry_count"] = new() { Type = "int", Default = 2 }
            },
            ParameterValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = "prod"
            },
            Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["artifact"] = "pkg-${params.environment}",
                ["summary"] = "${vars.artifact}-${params.retry_count}"
            }
        };

        var variables = WorkflowContextResolver.BuildInitialVariables(workflow);

        Assert.Equal("prod", variables["params.environment"]);
        Assert.Equal(2, variables["params.retry_count"]);
        Assert.Equal("pkg-prod", variables["vars.artifact"]);
        Assert.Equal("pkg-prod-2", variables["vars.summary"]);
    }

    [Fact]
    public void ResolveParameters_Should_Coerce_Typed_Values()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["retry_count"] = new() { Type = "int", Required = true },
                ["dry_run"] = new() { Type = "bool", Default = false }
            },
            ParameterValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["retry_count"] = "3"
            }
        };

        var resolved = WorkflowContextResolver.ResolveParameters(workflow);

        Assert.Equal(3, resolved["retry_count"]);
        Assert.Equal(false, resolved["dry_run"]);
    }

    [Fact]
    public void ResolveParameters_Should_Apply_AllowedValues_Array_ItemTypes_And_RequiredProperties()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", AllowedValues = { "dev", "prod" } },
                ["targets"] = new() { Type = "array", ItemType = "int" },
                ["metadata"] = new() { Type = "object", RequiredProperties = { "team" } }
            },
            ParameterValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = "prod",
                ["targets"] = new List<object> { "1", 2L },
                ["metadata"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["team"] = "platform"
                }
            }
        };

        var resolved = WorkflowContextResolver.ResolveParameters(workflow);

        Assert.Equal("prod", resolved["environment"]);
        Assert.Equal(new[] { 1, 2 }, Assert.IsType<List<object>>(resolved["targets"]).Cast<int>().ToArray());
        Assert.Equal("platform", Assert.IsType<Dictionary<string, object>>(resolved["metadata"])["team"]);
    }

    [Fact]
    public void ResolveParameters_Should_Reject_Value_Outside_Declared_Constraints()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ParameterDefinitions = new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = new() { Type = "string", AllowedValues = { "dev", "prod" } }
            },
            ParameterValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["environment"] = "test"
            }
        };

        var ex = Assert.Throws<WorkflowContextResolutionException>(() => WorkflowContextResolver.ResolveParameters(workflow));
        Assert.Contains("allowed values", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
