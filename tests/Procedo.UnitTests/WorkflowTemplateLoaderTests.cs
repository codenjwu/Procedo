using Procedo.Core.Models;
using Procedo.DSL;

namespace Procedo.UnitTests;

public class WorkflowTemplateLoaderTests
{
    [Fact]
    public void LoadFromFile_Should_Merge_Template_ParameterValues_And_Variables()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, """
name: base_pipeline
version: 1
parameters:
  environment:
    type: string
    required: true
  region:
    type: string
    default: eastus
variables:
  artifact_name: "${params.environment}-${params.region}"
stages:
- stage: build
  jobs:
  - job: package
    steps:
    - step: announce
      type: system.echo
      with:
        message: "${vars.artifact_name}"
""");

            var workflowPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(workflowPath, """
template: ./base.yaml
name: child_pipeline
parameters:
  environment: prod
variables:
  artifact_name: "override-${params.environment}"
""");

            var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);

            Assert.Equal("child_pipeline", workflow.Name);
            Assert.Equal("prod", workflow.ParameterValues["environment"]);
            Assert.Equal("eastus", workflow.ParameterDefinitions["region"].Default?.ToString());
            Assert.Equal("override-${params.environment}", workflow.Variables["artifact_name"]);
            Assert.Single(workflow.Stages);
            Assert.Single(workflow.Stages[0].Jobs);
            Assert.Single(workflow.Stages[0].Jobs[0].Steps);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadFromFile_Should_Throw_For_Template_Cycles()
    {
        var root = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(root, "a.yaml"), "template: ./b.yaml\n");
            File.WriteAllText(Path.Combine(root, "b.yaml"), "template: ./a.yaml\n");

            var ex = Assert.Throws<InvalidOperationException>(() => new WorkflowTemplateLoader().LoadFromFile(Path.Combine(root, "a.yaml")));
            Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadFromFile_Should_Include_Source_And_Template_Path_When_Child_Defines_Stages()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, "name: base\nversion: 1\nstages:\n- stage: base\n  jobs:\n  - job: flow\n    steps:\n    - step: hello\n      type: system.echo\n      with:\n        message: hi\n");

            var workflowPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(workflowPath, "template: ./base.yaml\nstages:\n- stage: child\n");

            var ex = Assert.Throws<InvalidOperationException>(() => new WorkflowTemplateLoader().LoadFromFile(workflowPath));
            Assert.Contains(workflowPath, ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(templatePath, ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cannot define stages", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadFromFile_Should_Preserve_Null_Default_For_Required_Template_Parameter()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, """
name: base
version: 1
parameters:
  service_name:
    type: string
    required: true
stages:
- stage: build
  jobs:
  - job: package
    steps:
    - step: announce
      type: system.echo
      with:
        message: "${params.service_name}"
""");

            var workflowPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(workflowPath, """
template: ./base.yaml
parameters:
  service_name: orders-api
""");

            var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);

            Assert.True(workflow.ParameterDefinitions.ContainsKey("service_name"));
            Assert.Null(workflow.ParameterDefinitions["service_name"].Default);
            Assert.Equal("orders-api", workflow.ParameterValues["service_name"]);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadFromText_Should_Use_Inline_Source_Label_In_Template_Merge_Errors()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, "name: base\nversion: 1\nstages:\n- stage: base\n  jobs:\n  - job: flow\n    steps:\n    - step: hello\n      type: system.echo\n      with:\n        message: hi\n");

            var yaml = "template: ./base.yaml\nstages:\n- stage: child\n";
            var ex = Assert.Throws<InvalidOperationException>(() => new WorkflowTemplateLoader().LoadFromText(yaml, root));
            Assert.Contains("<inline>", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(templatePath, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void LoadFromFile_Should_Preserve_Step_Condition_When_Cloning_Template_Workflow()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflowPath = Path.Combine(root, "workflow.yaml");
            File.WriteAllText(workflowPath, """
name: gated
version: 1
parameters:
  environment: dev
stages:
- stage: deploy
  jobs:
  - job: main
    steps:
    - step: gated_step
      type: system.echo
      condition: eq(params.environment, 'prod')
      with:
        message: gated
""");

            var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);

            Assert.Equal("eq(params.environment, 'prod')", workflow.Stages[0].Jobs[0].Steps[0].Condition);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-template-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

