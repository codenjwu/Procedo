using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserAdvancedEdgeCaseTests
{
    [Fact]
    public void Parse_Should_Keep_Quoted_Values_With_Colons()
    {
        var yaml = """
            name: quoted_colons
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: s:1
                  type: system.echo
                  with:
                    message: "http://api.local:8080/v1"
                    note: "phase:ingest:ready"
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var step = workflow.Stages[0].Jobs[0].Steps[0];

        Assert.Equal("s:1", step.Step);
        Assert.Equal("http://api.local:8080/v1", step.With["message"]);
        Assert.Equal("phase:ingest:ready", step.With["note"]);
    }

    [Fact]
    public void Parse_Should_Use_Last_Value_When_Duplicate_Keys_Appear()
    {
        var yaml = """
            name: duplicate_keys
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    retries: 1
                    retries: 5
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var with = workflow.Stages[0].Jobs[0].Steps[0].With;

        Assert.Equal(5, with["retries"]);
    }

    [Fact]
    public void Parse_Should_Preserve_Nested_With_Object_For_Complex_Payloads()
    {
        var yaml = """
            name: nested_with
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    payload:
                      source: crm
                      flags:
                      - fast
                      - safe
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var payload = workflow.Stages[0].Jobs[0].Steps[0].With["payload"];

        var payloadMap = Assert.IsType<Dictionary<string, object?>>(payload);
        Assert.Equal("crm", payloadMap["source"]);

        var flags = Assert.IsType<List<object?>>(payloadMap["flags"]);
        Assert.Equal(["fast", "safe"], flags.Cast<string>().ToArray());
    }

    [Fact]
    public void Parse_Should_Not_Resolve_Anchors_Or_Aliases_And_Keep_Literals()
    {
        var yaml = """
            name: anchors_aliases
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: base
                  type: system.echo
                  with:
                    message: "&base hello"
                - step: use
                  type: system.echo
                  depends_on: "*base"
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var first = workflow.Stages[0].Jobs[0].Steps[0];
        var second = workflow.Stages[0].Jobs[0].Steps[1];

        Assert.Equal("&base hello", first.With["message"]);
        Assert.Single(second.DependsOn);
        Assert.Equal("*base", second.DependsOn[0]);
    }
}
