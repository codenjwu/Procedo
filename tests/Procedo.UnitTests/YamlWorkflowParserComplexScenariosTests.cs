using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserComplexScenariosTests
{
    [Fact]
    public void Parse_Should_Map_MultiStage_MultiJob_MixedDependencyWorkflow()
    {
        var yaml = """
            name: enterprise_pipeline
            version: 7

            stages:
            - stage: ingest
              jobs:
              - job: fetch
                steps:
                - step: download_users
                  type: system.echo
                  with:
                    message: "download users"
                - step: download_orders
                  type: system.echo
                  with:
                    message: "download orders"
              - job: validate
                steps:
                - step: schema_check
                  type: system.echo
                  depends_on:
                  - download_users
                  - download_orders
            - stage: transform
              jobs:
              - job: process
                steps:
                - step: normalize
                  type: system.echo
                  depends_on: schema_check
                - step: enrich
                  type: system.echo
                  depends_on:
                  - normalize
                - step: publish
                  type: system.echo
                  depends_on:
                  - enrich
                  with:
                    retries: 2
                    dry_run: false
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("enterprise_pipeline", workflow.Name);
        Assert.Equal(7, workflow.Version);
        Assert.Equal(2, workflow.Stages.Count);

        var ingest = workflow.Stages[0];
        Assert.Equal("ingest", ingest.Stage);
        Assert.Equal(2, ingest.Jobs.Count);

        var fetch = ingest.Jobs[0];
        Assert.Equal("fetch", fetch.Job);
        Assert.Equal(2, fetch.Steps.Count);

        var validate = ingest.Jobs[1];
        var schemaCheck = validate.Steps.Single();
        Assert.Equal("schema_check", schemaCheck.Step);
        Assert.Equal(2, schemaCheck.DependsOn.Count);
        Assert.Equal("download_users", schemaCheck.DependsOn[0]);
        Assert.Equal("download_orders", schemaCheck.DependsOn[1]);

        var transform = workflow.Stages[1];
        Assert.Equal("transform", transform.Stage);
        var process = transform.Jobs.Single();
        Assert.Equal(3, process.Steps.Count);

        var normalize = process.Steps[0];
        var enrich = process.Steps[1];
        var publish = process.Steps[2];

        Assert.Single(normalize.DependsOn);
        Assert.Equal("schema_check", normalize.DependsOn[0]);

        Assert.Single(enrich.DependsOn);
        Assert.Equal("normalize", enrich.DependsOn[0]);

        Assert.Single(publish.DependsOn);
        Assert.Equal("enrich", publish.DependsOn[0]);
        Assert.Equal(2, publish.With["retries"]);
        Assert.Equal(false, publish.With["dry_run"]);
    }

    [Fact]
    public void Parse_Should_Allow_Steps_Without_DependsOn()
    {
        var yaml = """
            name: independent_steps
            version: 1
            stages:
            - stage: s
              jobs:
              - job: j
                steps:
                - step: a
                  type: system.echo
                - step: b
                  type: system.echo
                - step: c
                  type: system.echo
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var steps = workflow.Stages[0].Jobs[0].Steps;

        Assert.All(steps, s => Assert.Empty(s.DependsOn));
    }

    [Fact]
    public void Parse_Should_Handle_Comments_And_Blank_Lines()
    {
        var yaml = """
            # top-level comment
            name: comment_case

            version: 1

            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: a
                  type: system.echo
                  with:
                    message: "hello"

            # trailing comment
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal("comment_case", workflow.Name);
        Assert.Single(workflow.Stages);
        Assert.Single(workflow.Stages[0].Jobs);
        Assert.Single(workflow.Stages[0].Jobs[0].Steps);
    }

    [Fact]
    public void Parse_Should_Keep_Step_Order_In_Complex_Workflow()
    {
        var yaml = """
            name: ordering
            version: 1
            stages:
            - stage: s1
              jobs:
              - job: j1
                steps:
                - step: start
                  type: system.echo
                - step: fanout_a
                  type: system.echo
                  depends_on: start
                - step: fanout_b
                  type: system.echo
                  depends_on: start
                - step: merge
                  type: system.echo
                  depends_on:
                  - fanout_a
                  - fanout_b
            """;

        var workflow = new YamlWorkflowParser().Parse(yaml);
        var steps = workflow.Stages[0].Jobs[0].Steps;

        Assert.Equal(["start", "fanout_a", "fanout_b", "merge"], steps.Select(s => s.Step).ToArray());
    }

    [Fact]
    public void Parse_Should_Throw_On_Malformed_Indentation()
    {
        var yaml = """
            name: broken
            version: 1
            stages:
              - stage: s1
               jobs:
               - job: j1
                 steps:
                 - step: a
                   type: system.echo
            """;

        Assert.Throws<InvalidOperationException>(() => new YamlWorkflowParser().Parse(yaml));
    }
}
