using Procedo.Core.Models;
using Procedo.Validation;
using Procedo.Validation.Models;

namespace Procedo.UnitTests;

public class ProcedoWorkflowValidatorOptionsTests
{
    [Fact]
    public void Validate_Should_Keep_Duplicate_Dependency_As_Warning_In_Permissive_Mode()
    {
        var workflow = BuildWorkflowWithDuplicateDependency();

        var result = new ProcedoWorkflowValidator().Validate(workflow, options: ValidationOptions.Permissive);

        Assert.Contains(result.Warnings, w => w.Code == "PV308");
        Assert.DoesNotContain(result.Errors, e => e.Code == "PV308");
    }

    [Fact]
    public void Validate_Should_Promote_Duplicate_Dependency_To_Error_In_Strict_Mode()
    {
        var workflow = BuildWorkflowWithDuplicateDependency();

        var result = new ProcedoWorkflowValidator().Validate(workflow, options: ValidationOptions.Strict);

        Assert.Contains(result.Errors, e => e.Code == "PV308");
        Assert.DoesNotContain(result.Warnings, w => w.Code == "PV308");
    }

    private static WorkflowDefinition BuildWorkflowWithDuplicateDependency()
        => new()
        {
            Name = "opts",
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
                                    DependsOn = { "a", "a" }
                                }
                            }
                        }
                    }
                }
            }
        };
}
