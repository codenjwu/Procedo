namespace Procedo.UnitTests;

public sealed class PackagingSmokeTests
{
    [Fact]
    public void PackScript_Should_Define_Public_Profile()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "scripts", "pack-nuget.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("[ValidateSet(\"minimal\", \"public\", \"all\", \"custom\")]", script);
        Assert.Contains("\"public\" {", script);
    }

    [Fact]
    public void PackScript_Public_Profile_Should_Include_Expected_Public_Projects()
    {
        var scriptPath = Path.Combine(GetRepoRoot(), "scripts", "pack-nuget.ps1");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("src/Procedo.Engine/Procedo.Engine.csproj", script);
        Assert.Contains("src/Procedo.Hosting/Procedo.Hosting.csproj", script);
        Assert.Contains("src/Procedo.Plugin.SDK/Procedo.Plugin.SDK.csproj", script);
        Assert.Contains("src/Procedo.Extensions.DependencyInjection/Procedo.Extensions.DependencyInjection.csproj", script);
    }

    [Fact]
    public void PackageGuide_Should_List_Public_Profile_Command()
    {
        var docPath = Path.Combine(GetRepoRoot(), "docs", "PACKAGE_GUIDE.md");
        var doc = File.ReadAllText(docPath);

        Assert.Contains("./scripts/pack-nuget.ps1 -Profile public -IncludeSystemPlugin", doc);
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Procedo.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Procedo.sln from the test output directory.");
    }
}
