using Procedo.DSL;

namespace Procedo.UnitTests;

public class YamlWorkflowParserExecutionPolicyTests
{
    [Fact]
    public void Parse_Should_Read_Workflow_Job_And_Step_Execution_Policies()
    {
        var yaml = """
name: policy_pipeline
version: 1
max_parallelism: 4
continue_on_error: true

stages:
- stage: s1
  jobs:
  - job: j1
    max_parallelism: 2
    continue_on_error: false
    steps:
    - step: a
      type: system.echo
      retries: 3
      timeout_ms: 1500
      continue_on_error: true
      with:
        message: hello
""";

        var wf = new YamlWorkflowParser().Parse(yaml);

        Assert.Equal(4, wf.MaxParallelism);
        Assert.True(wf.ContinueOnError);

        var job = wf.Stages[0].Jobs[0];
        Assert.Equal(2, job.MaxParallelism);
        Assert.False(job.ContinueOnError);

        var step = job.Steps[0];
        Assert.Equal(3, step.Retries);
        Assert.Equal(1500, step.TimeoutMs);
        Assert.True(step.ContinueOnError);
    }
}
