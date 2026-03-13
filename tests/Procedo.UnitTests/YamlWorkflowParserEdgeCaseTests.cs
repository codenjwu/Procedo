using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserEdgeCaseTests
{
    [Fact]
    public void Parse_Should_Default_Version_To_One_When_Missing()
    {
        var yaml = """
            name: no_version
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal(1, workflow.Version);
    }

    [Fact]
    public void Parse_Should_Handle_DependsOn_As_Scalar_String()
    {
        var yaml = """
            name: scalar_dep
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                - step: b
                  type: system.echo
                  depends_on: a
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var stepB = workflow.Stages[0].Jobs[0].Steps[1];

        Assert.Single(stepB.DependsOn);
        Assert.Equal("a", stepB.DependsOn[0]);
    }

    [Fact]
    public void Parse_Should_Parse_Int_And_Bool_Inputs()
    {
        var yaml = """
            name: typed_with
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    retries: 3
                    enabled: true
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var with = workflow.Stages[0].Jobs[0].Steps[0].With;

        Assert.Equal(3, with["retries"]);
        Assert.Equal(true, with["enabled"]);
    }
}
