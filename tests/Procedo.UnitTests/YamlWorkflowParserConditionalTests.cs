using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserConditionalTests
{
    [Fact]
    public void Parse_Should_Apply_If_Else_Blocks_In_Mappings_And_Sequences()
    {
        var yaml = """
            name: conditional_pipeline
            version: 1
            parameters:
              environment: prod
            variables:
              ${{ if eq(params.environment, 'prod') }}:
                endpoint: api.contoso.com
              ${{ else }}:
                endpoint: api-dev.contoso.com
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                  ${{ if eq(params.environment, 'prod') }}:
                  - step: prod_announce
                    type: system.echo
                    with:
                      message: "${vars.endpoint}"
                  ${{ else }}:
                  - step: dev_announce
                    type: system.echo
                    with:
                      message: "${vars.endpoint}"
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("api.contoso.com", workflow.Variables["endpoint"]);
        var steps = workflow.Stages[0].Jobs[0].Steps;
        Assert.Single(steps);
        Assert.Equal("prod_announce", steps[0].Step);
    }

    [Fact]
    public void Parse_Should_Apply_ElseIf_Block_When_If_Is_False()
    {
        var yaml = """
            name: conditional_pipeline
            version: 1
            parameters:
              environment: qa
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                  ${{ if eq(params.environment, 'prod') }}:
                  - step: prod_announce
                    type: system.echo
                  ${{ elseif eq(params.environment, 'qa') }}:
                  - step: qa_announce
                    type: system.echo
                  ${{ else }}:
                  - step: dev_announce
                    type: system.echo
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        var steps = workflow.Stages[0].Jobs[0].Steps;
        Assert.Single(steps);
        Assert.Equal("qa_announce", steps[0].Step);
    }

    [Fact]
    public void Parse_Should_Expand_Each_Block_For_Array_Parameters()
    {
        var yaml = """
            name: each_pipeline
            version: 1
            parameters:
              targets:
              - eastus
              - westus
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                  ${{ each target in params.targets }}:
                  - step: announce_${target}
                    type: system.echo
                    with:
                      message: "deploy ${target}"
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        var steps = workflow.Stages[0].Jobs[0].Steps;
        Assert.Equal(2, steps.Count);
        Assert.Equal("announce_eastus", steps[0].Step);
        Assert.Equal("announce_westus", steps[1].Step);
        Assert.Equal("deploy eastus", steps[0].With["message"]);
        Assert.Equal("deploy westus", steps[1].With["message"]);
    }

    [Fact]
    public void Parse_Should_Throw_When_Each_Target_Is_Not_An_Array()
    {
        var yaml = """
            name: invalid_each_pipeline
            version: 1
            parameters:
              target: eastus
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                  ${{ each item in params.target }}:
                  - step: announce_${item}
                    type: system.echo
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => new YamlWorkflowParser().Parse(yaml));
        Assert.Contains("must evaluate to an array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Should_Throw_When_Each_Target_Is_An_Object()
    {
        var yaml = """
            name: invalid_each_pipeline
            version: 1
            parameters:
              regions:
                eastus: primary
                westus: secondary
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                  ${{ each item in params.regions }}:
                  - step: announce_${item}
                    type: system.echo
            """;

        var ex = Assert.Throws<InvalidOperationException>(() => new YamlWorkflowParser().Parse(yaml));
        Assert.Contains("must evaluate to an array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Should_Preserve_Runtime_Condition_On_Steps()
    {
        var yaml = """
            name: conditional_runtime_pipeline
            version: 1
            stages:
            - stage: deploy
              jobs:
              - job: main
                steps:
                - step: gated
                  type: system.echo
                  condition: eq(params.environment, 'prod')
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("eq(params.environment, 'prod')", workflow.Stages[0].Jobs[0].Steps[0].Condition);
    }
}
