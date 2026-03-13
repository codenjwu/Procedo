using Procedo.Core.Models;
using Procedo.Plugin.SDK;
using Procedo.Validation;

namespace Procedo.UnitTests;

public class ProcedoWorkflowValidatorTests
{
    [Fact]
    public void Validate_Should_Return_No_Issues_For_Valid_Workflow()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "valid",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "j1",
                            Steps =
                            {
                                new StepDefinition { Step = "a", Type = "system.echo" },
                                new StepDefinition { Step = "b", Type = "system.echo", DependsOn = { "a" } }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("system.echo", () => new SuccessStep());

        var result = new ProcedoWorkflowValidator().Validate(workflow, registry);

        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_Should_Report_Required_Field_Errors()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "",
            Version = 0,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "",
                            Steps =
                            {
                                new StepDefinition { Step = "", Type = "" }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.True(result.HasErrors);
        Assert.Contains(result.Errors, e => e.Code == "PV001");
        Assert.Contains(result.Errors, e => e.Code == "PV002");
        Assert.Contains(result.Errors, e => e.Code == "PV100");
        Assert.Contains(result.Errors, e => e.Code == "PV200");
        Assert.Contains(result.Errors, e => e.Code == "PV300");
        Assert.Contains(result.Errors, e => e.Code == "PV302");
    }

    [Fact]
    public void Validate_Should_Report_Duplicate_Ids()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "dup",
            Version = 1,
            Stages =
            {
                new StageDefinition { Stage = "same" },
                new StageDefinition
                {
                    Stage = "same",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = "step", Type = "system.echo" },
                                new StepDefinition { Step = "step", Type = "system.echo" }
                            }
                        },
                        new JobDefinition { Job = "job" }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV101");
        Assert.Contains(result.Errors, e => e.Code == "PV201");
        Assert.Contains(result.Errors, e => e.Code == "PV301");
    }

    [Fact]
    public void Validate_Should_Report_Dependency_Issues_And_Warnings()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "deps",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "j1",
                            Steps =
                            {
                                new StepDefinition
                                {
                                    Step = "a",
                                    Type = "system.echo",
                                    DependsOn = { "a", "missing", "missing", "" }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV306");
        Assert.Contains(result.Errors, e => e.Code == "PV307");
        Assert.Contains(result.Errors, e => e.Code == "PV305");
        Assert.Contains(result.Warnings, w => w.Code == "PV308");
    }

    [Fact]
    public void Validate_Should_Report_Cycle_Error()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "cycle",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "j1",
                            Steps =
                            {
                                new StepDefinition { Step = "a", Type = "system.echo", DependsOn = { "b" } },
                                new StepDefinition { Step = "b", Type = "system.echo", DependsOn = { "a" } }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV309");
    }

    [Fact]
    public void Validate_Should_Report_Invalid_Step_Type_Format()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "type",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "j1",
                            Steps =
                            {
                                new StepDefinition { Step = "a", Type = "badtype" },
                                new StepDefinition { Step = "b", Type = "too.many.parts" },
                                new StepDefinition { Step = "c", Type = "ok.but#bad" }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Equal(3, result.Errors.Count(e => e.Code == "PV303"));
    }

    [Fact]
    public void Validate_Should_Skip_Plugin_Resolution_When_Registry_Not_Provided()
    {
        var workflow = BuildSingleStepWorkflow("unknown.plugin");

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.DoesNotContain(result.Errors, e => e.Code == "PV304");
    }

    [Fact]
    public void Validate_Should_Report_Missing_Plugin_When_Registry_Provided()
    {
        var workflow = BuildSingleStepWorkflow("unknown.plugin");
        IPluginRegistry registry = new PluginRegistry();

        var result = new ProcedoWorkflowValidator().Validate(workflow, registry);

        Assert.Contains(result.Errors, e => e.Code == "PV304");
    }

    private static WorkflowDefinition BuildSingleStepWorkflow(string type)
        => new()
        {
            Name = "wf",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "j1",
                            Steps = { new StepDefinition { Step = "a", Type = type } }
                        }
                    }
                }
            }
        };

    private sealed class SuccessStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }
}
