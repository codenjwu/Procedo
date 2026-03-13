using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserTests
{
    [Fact]
    public void Parse_Should_Map_Stages_Jobs_Steps_And_DependsOn()
    {
        var yaml = """
            name: pipeline
            version: 1

            stages:
            - stage: build
              jobs:
              - job: main
                steps:
                - step: download
                  type: system.echo
                  with:
                    message: "download"
                - step: parse
                  type: system.echo
                  depends_on:
                  - download
                  with:
                    message: "parse"
            """;

        var parser = new YamlWorkflowParser();
        var workflow = parser.Parse(yaml);

        Assert.Equal("pipeline", workflow.Name);
        Assert.Equal(1, workflow.Version);
        Assert.Single(workflow.Stages);
        Assert.Single(workflow.Stages[0].Jobs);
        Assert.Equal(2, workflow.Stages[0].Jobs[0].Steps.Count);

        var parseStep = workflow.Stages[0].Jobs[0].Steps[1];
        Assert.Equal("parse", parseStep.Step);
        Assert.Single(parseStep.DependsOn);
        Assert.Equal("download", parseStep.DependsOn[0]);
        Assert.Equal("parse", parseStep.With["message"]);
    }

    [Fact]
    public void Parse_Should_Map_Enhanced_Parameter_Schema_Fields()
    {
        var yaml = """
            name: pipeline
            parameters:
              environment:
                type: string
                allowed_values:
                - dev
                - prod
                min_length: 3
                max_length: 8
                pattern: "^[a-z]+$"
              retry_count:
                type: int
                min: 1
                max: 5
              targets:
                type: array
                item_type: string
              metadata:
                type: object
                required_properties:
                - team
                - owner
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        var environment = workflow.ParameterDefinitions["environment"];
        Assert.Equal(new[] { "dev", "prod" }, environment.AllowedValues.Select(static x => x.ToString()).ToArray());
        Assert.Equal(3, environment.MinLength);
        Assert.Equal(8, environment.MaxLength);
        Assert.Equal("^[a-z]+$", environment.Pattern);

        var retryCount = workflow.ParameterDefinitions["retry_count"];
        Assert.Equal(1, retryCount.Minimum);
        Assert.Equal(5, retryCount.Maximum);

        var targets = workflow.ParameterDefinitions["targets"];
        Assert.Equal("string", targets.ItemType);

        var metadata = workflow.ParameterDefinitions["metadata"];
        Assert.Equal(new[] { "team", "owner" }, metadata.RequiredProperties);
    }
}

