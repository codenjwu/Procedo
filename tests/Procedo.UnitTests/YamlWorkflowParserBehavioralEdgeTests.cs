using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserBehavioralEdgeTests
{
    [Fact]
    public void Parse_Should_Keep_Inline_Comment_Text_As_Part_Of_Value_CurrentBehavior()
    {
        var yaml = """
            name: inline_comment
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    message: hello # this stays in value currently
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var message = workflow.Stages[0].Jobs[0].Steps[0].With["message"];

        Assert.Equal("hello # this stays in value currently", message);
    }

    [Fact]
    public void Parse_Should_Handle_Negative_Int_And_Preserve_Null_Value()
    {
        var yaml = """
            name: scalar_types
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    negative: -4
                    ratio: 2.5
                    nullable: null
            """;

        var with = new YamlWorkflowParser().Parse(yaml).Stages[0].Jobs[0].Steps[0].With;

        Assert.Equal(-4, with["negative"]);
        Assert.Equal("2.5", with["ratio"]);
        Assert.Null(with["nullable"]);
    }

    [Fact]
    public void Parse_Should_Parse_Tab_Indented_Workflow_CurrentBehavior()
    {
        var yaml = "name: x\nversion: 1\nstages:\n\t- stage: s1\n\t  jobs:\n\t  - job: j1\n\t    steps:\n\t    - step: a\n\t      type: system.echo";

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("x", workflow.Name);
        Assert.Single(workflow.Stages);
    }

    [Fact]
    public void Parse_Should_Use_Last_Root_Key_When_Duplicated()
    {
        var yaml = """
            name: first
            name: second
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("second", workflow.Name);
    }

    [Fact]
    public void Parse_Should_Allow_Missing_Step_Type_And_Keep_Empty_String_CurrentBehavior()
    {
        var yaml = """
            name: missing_type
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
            """;

        var step = new YamlWorkflowParser().Parse(yaml).Stages[0].Jobs[0].Steps[0];

        Assert.Equal("a", step.Step);
        Assert.Equal(string.Empty, step.Type);
    }
}
