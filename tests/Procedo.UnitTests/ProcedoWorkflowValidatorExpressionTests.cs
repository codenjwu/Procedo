using Procedo.Core.Models;
using Procedo.Validation;

namespace Procedo.UnitTests;

public class ProcedoWorkflowValidatorExpressionTests
{
    [Fact]
    public void Validate_Should_Allow_Valid_Step_Output_Expression_When_Dependency_Exists()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "expr-ok",
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
                                new StepDefinition
                                {
                                    Step = "b",
                                    Type = "system.echo",
                                    DependsOn = { "a" },
                                    With =
                                    {
                                        ["message"] = "${steps.a.outputs.value}"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.DoesNotContain(result.Errors, e => e.Code is "PV310" or "PV311" or "PV313");
    }

    [Fact]
    public void Validate_Should_Report_Unsupported_Expression_Format()
    {
        var workflow = BuildSingleStepWithMessage("${abc}");

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV310");
    }

    [Fact]
    public void Validate_Should_Report_Unknown_Referenced_Step()
    {
        var workflow = BuildSingleStepWithMessage("${steps.missing.outputs.value}");

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV311");
    }

    [Fact]
    public void Validate_Should_Report_Reference_Without_Dependency_Chain()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "expr-dep",
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
                                new StepDefinition { Step = "b", Type = "system.echo" },
                                new StepDefinition
                                {
                                    Step = "c",
                                    Type = "system.echo",
                                    DependsOn = { "b" },
                                    With =
                                    {
                                        ["message"] = "${steps.a.outputs.value}"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV313");
    }

    [Fact]
    public void Validate_Should_Allow_Function_Expressions_With_Known_References()
    {
        var workflow = BuildSingleStepWithMessage("${format('Deploy {0}', params.environment)}");
        workflow.ParameterDefinitions["environment"] = new ParameterDefinition
        {
            Type = "string",
            Required = true
        };
        workflow.ParameterValues["environment"] = "prod";

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.DoesNotContain(result.Errors, e => e.Code is "PV310" or "PV314");
    }

    [Fact]
    public void Validate_Should_Allow_Runtime_Condition_With_Known_Step_Dependency()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "expr-condition-ok",
            Version = 1,
            ParameterDefinitions =
            {
                ["environment"] = new ParameterDefinition { Type = "string", Required = true }
            },
            ParameterValues =
            {
                ["environment"] = "prod"
            },
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
                                new StepDefinition
                                {
                                    Step = "b",
                                    Type = "system.echo",
                                    DependsOn = { "a" },
                                    Condition = "and(eq(params.environment, 'prod'), eq(steps.a.outputs.value, 'ok'))"
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.DoesNotContain(result.Errors, e => e.Code is "PV310" or "PV311" or "PV313" or "PV314");
    }

    [Fact]
    public void Validate_Should_Report_Unknown_Variable_In_Runtime_Condition()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "expr-condition-fail",
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
                                    Condition = "eq(vars.missing, 'x')"
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = new ProcedoWorkflowValidator().Validate(workflow);

        Assert.Contains(result.Errors, e => e.Code == "PV315" && e.Path.EndsWith(".condition", StringComparison.Ordinal));
    }

    private static WorkflowDefinition BuildSingleStepWithMessage(string message)
        => new()
        {
            Name = "expr",
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
                                    With =
                                    {
                                        ["message"] = message
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
}
