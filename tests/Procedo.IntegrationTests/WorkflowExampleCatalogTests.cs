using Procedo.DSL;
using Procedo.Engine.Hosting;

namespace Procedo.IntegrationTests;

public sealed class WorkflowExampleCatalogTests
{
    public static IEnumerable<object[]> DirectSmokeExamples()
        => ExampleCatalogInventory.GetWorkflowEntries()
            .Where(static entry => entry.VerificationMode == ExampleVerificationMode.DirectSmoke)
            .Select(static entry => new object[] { entry.FileName });

    [Fact]
    public void WorkflowInventory_Should_Cover_All_TopLevel_Yaml_Examples()
    {
        var repoRoot = ExampleCatalogInventory.GetRepoRoot();
        var examplesRoot = Path.Combine(repoRoot, "examples");
        var diskFiles = Directory.GetFiles(examplesRoot, "*.yaml", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var inventoryFiles = ExampleCatalogInventory.GetWorkflowEntries()
            .Select(static entry => entry.FileName)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(diskFiles, inventoryFiles);
    }

    [Fact]
    public void ProjectInventory_Should_Point_To_Existing_Project_Files()
    {
        var repoRoot = ExampleCatalogInventory.GetRepoRoot();

        foreach (var project in ExampleCatalogInventory.GetProjectEntries())
        {
            var fullPath = Path.Combine(repoRoot, project.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(fullPath), $"Expected project example '{project.Key}' at '{fullPath}'.");
        }
    }

    [Fact]
    public void Readme_Should_Reference_All_Inventory_Entries()
    {
        var readme = File.ReadAllText(Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "README.md"));

        foreach (var workflow in ExampleCatalogInventory.GetWorkflowEntries())
        {
            Assert.Contains(workflow.FileName, readme, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var project in ExampleCatalogInventory.GetProjectEntries())
        {
            var projectName = Path.GetFileNameWithoutExtension(project.RelativePath);
            Assert.Contains(projectName, readme, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void WorkflowInventory_Should_Classify_All_Examples()
    {
        foreach (var workflow in ExampleCatalogInventory.GetWorkflowEntries())
        {
            Assert.False(string.IsNullOrWhiteSpace(workflow.Category));
            Assert.True(Enum.IsDefined(workflow.ExpectedOutcome));
            Assert.True(Enum.IsDefined(workflow.VerificationMode));
        }
    }

    [Fact]
    public void Catalog_Should_Have_A_Meaningful_Governed_Success_Subset()
    {
        var governedCount = ExampleCatalogInventory.GetWorkflowEntries()
            .Where(static entry => entry.ExpectedOutcome == ExampleExpectedOutcome.Success)
            .Count(static entry => entry.VerificationMode is ExampleVerificationMode.DirectSmoke or ExampleVerificationMode.DedicatedTest);

        Assert.True(governedCount >= 25, $"Expected at least 25 success examples to have automated execution verification, but found {governedCount}.");
    }

    [Fact]
    public void DirectSmoke_Examples_Should_Parse_And_Load()
    {
        var loader = new WorkflowTemplateLoader();
        var repoRoot = ExampleCatalogInventory.GetRepoRoot();

        foreach (var workflow in ExampleCatalogInventory.GetWorkflowEntries().Where(static entry => entry.VerificationMode == ExampleVerificationMode.DirectSmoke))
        {
            var fullPath = Path.Combine(repoRoot, workflow.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var definition = loader.LoadFromFile(fullPath);
            Assert.False(string.IsNullOrWhiteSpace(definition.Name));
        }
    }

    [Theory]
    [MemberData(nameof(DirectSmokeExamples))]
    public async Task DirectSmoke_Examples_Should_Run_Successfully(string fileName)
    {
        var repoRoot = ExampleCatalogInventory.GetRepoRoot();
        var path = Path.Combine(repoRoot, "examples", fileName);

        var host = ExampleCatalogInventory.CreateHostBuilder().Build();
        var result = await host.ExecuteFileAsync(path);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public void Dedicated_Verification_Examples_Should_Reference_A_Test()
    {
        var missingReference = ExampleCatalogInventory.GetWorkflowEntries()
            .Where(static entry => entry.VerificationMode == ExampleVerificationMode.DedicatedTest)
            .Where(static entry => string.IsNullOrWhiteSpace(entry.VerificationReference))
            .Select(static entry => entry.FileName)
            .ToArray();

        Assert.Empty(missingReference);
    }
}
