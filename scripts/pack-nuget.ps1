param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "artifacts/nuget",
    [string]$Version,
    [switch]$SkipRestore,
    [switch]$SkipBuild,
    [ValidateSet("minimal", "public", "all", "custom")]
    [string]$Profile = "minimal",
    [switch]$IncludeSystemPlugin,
    [switch]$IncludeDemoPlugin,
    [string[]]$Projects
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $repoRoot

if ($Profile -eq "public" -and [string]::IsNullOrWhiteSpace($Version)) {
    throw "Profile 'public' requires -Version so published package versions are explicit and match release notes/changelog."
}

function Normalize-Path {
    param([string]$Path)

    return [System.IO.Path]::GetFullPath($Path)
}

function Is-PackableLibraryProject {
    param([string]$ProjectPath)

    if ($ProjectPath -match "\\tests\\" -or $ProjectPath -match "\\examples\\") {
        return $false
    }

    [xml]$xml = Get-Content $ProjectPath
    $propertyGroups = @($xml.SelectNodes("/Project/PropertyGroup"))

    $isTestProject = $false
    $isPackable = $true
    $outputType = ""

    foreach ($group in $propertyGroups) {
        if (($group.PSObject.Properties.Name -contains "IsTestProject") -and [string]$group.IsTestProject -eq "true") {
            $isTestProject = $true
        }

        if (($group.PSObject.Properties.Name -contains "IsPackable") -and [string]$group.IsPackable -eq "false") {
            $isPackable = $false
        }

        if (($group.PSObject.Properties.Name -contains "OutputType") -and $group.OutputType) {
            $outputType = [string]$group.OutputType
        }
    }

    if ($isTestProject -or -not $isPackable -or $outputType -eq "Exe") {
        return $false
    }

    return $true
}

function Get-ProjectReferences {
    param([string]$ProjectPath)

    [xml]$xml = Get-Content $ProjectPath
    $refs = @()

    foreach ($ref in @($xml.SelectNodes("/Project/ItemGroup/ProjectReference"))) {
        if (-not $ref.Include) {
            continue
        }

        $refPath = Normalize-Path (Join-Path (Split-Path $ProjectPath -Parent) ([string]$ref.Include))
        if (Test-Path $refPath) {
            $refs += $refPath
        }
    }

    return $refs | Sort-Object -Unique
}

function Resolve-ProjectClosure {
    param([string[]]$Roots)

    $queue = New-Object System.Collections.Generic.Queue[string]
    $visited = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($root in $Roots) {
        $normalized = Normalize-Path $root
        if ($visited.Add($normalized)) {
            $queue.Enqueue($normalized)
        }
    }

    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        foreach ($ref in Get-ProjectReferences -ProjectPath $current) {
            if ($visited.Add($ref)) {
                $queue.Enqueue($ref)
            }
        }
    }

    $result = @()
    foreach ($path in $visited) {
        if (Is-PackableLibraryProject -ProjectPath $path) {
            $result += $path
        }
    }

    return $result | Sort-Object -Unique
}

function Get-AllPackableProjects {
    param([string]$Root)

    $all = Get-ChildItem -Path $Root -Recurse -Filter *.csproj -File | ForEach-Object { $_.FullName }
    return $all | Where-Object { Is-PackableLibraryProject -ProjectPath $_ } | Sort-Object -Unique
}

function Get-PublicDependencyAllowList {
    return @{
        "Procedo.Plugin.SDK" = @()
        "Procedo.Engine" = @("Procedo.Plugin.SDK")
        "Procedo.Hosting" = @("Procedo.Engine", "Procedo.Plugin.SDK")
        "Procedo.Extensions.DependencyInjection" = @("Procedo.Hosting", "Procedo.Plugin.SDK")
        "Procedo.Plugin.System" = @("Procedo.Plugin.SDK")
    }
}

function Update-NuspecDependencies {
    param(
        [string]$PackagePath,
        [hashtable]$AllowList
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::Open($PackagePath, [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
        if (-not $nuspecEntry) {
            return
        }

        $packageId = [System.IO.Path]::GetFileNameWithoutExtension($nuspecEntry.FullName)
        if (-not $AllowList.ContainsKey($packageId)) {
            return
        }

        $allowedDependencyIds = @($AllowList[$packageId])

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $ns = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
        $ns.AddNamespace("n", "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")

        $dependencyGroups = @($nuspec.SelectNodes("/n:package/n:metadata/n:dependencies/n:group", $ns))
        foreach ($group in $dependencyGroups) {
            $dependencies = @($group.SelectNodes("n:dependency", $ns))
            foreach ($dependency in $dependencies) {
                if ($allowedDependencyIds -notcontains $dependency.id) {
                    [void]$group.RemoveChild($dependency)
                }
            }

            if (-not $group.SelectSingleNode("n:dependency", $ns)) {
                [void]$group.ParentNode.RemoveChild($group)
            }
        }

        $dependenciesNode = $nuspec.SelectSingleNode("/n:package/n:metadata/n:dependencies", $ns)
        if ($dependenciesNode -and -not $dependenciesNode.SelectSingleNode("n:group", $ns)) {
            [void]$dependenciesNode.ParentNode.RemoveChild($dependenciesNode)
        }

        $updatedNuspec = $nuspec.OuterXml
        $entryName = $nuspecEntry.FullName
        $nuspecEntry.Delete()
        $newEntry = $zip.CreateEntry($entryName)
        $writer = [System.IO.StreamWriter]::new($newEntry.Open())
        try {
            $writer.Write($updatedNuspec)
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Get-SeedProjectsForProfile {
    param(
        [string]$ProfileName,
        [bool]$IncludeSystem,
        [bool]$IncludeDemo
    )

    switch ($ProfileName) {
        "minimal" {
            $seed = @(
                "src/Procedo.Engine/Procedo.Engine.csproj"
            )

            if ($IncludeSystem) {
                $seed += "plugins/Procedo.Plugin.System/Procedo.Plugin.System.csproj"
            }

            if ($IncludeDemo) {
                $seed += "examples/Procedo.Plugin.Demo/Procedo.Plugin.Demo.csproj"
            }

            return $seed
        }
        "public" {
            $seed = @(
                "src/Procedo.Engine/Procedo.Engine.csproj",
                "src/Procedo.Hosting/Procedo.Hosting.csproj",
                "src/Procedo.Plugin.SDK/Procedo.Plugin.SDK.csproj",
                "src/Procedo.Extensions.DependencyInjection/Procedo.Extensions.DependencyInjection.csproj"
            )

            if ($IncludeSystem) {
                $seed += "plugins/Procedo.Plugin.System/Procedo.Plugin.System.csproj"
            }

            if ($IncludeDemo) {
                $seed += "examples/Procedo.Plugin.Demo/Procedo.Plugin.Demo.csproj"
            }

            return $seed
        }
        "all" {
            return @()
        }
        default {
            return @()
        }
    }
}

$selectedProjects = @()

if ($Profile -eq "custom") {
    if (-not $Projects -or $Projects.Count -eq 0) {
        throw "Profile 'custom' requires -Projects."
    }

    $roots = @()
    foreach ($project in $Projects) {
        $roots += (Resolve-Path $project -ErrorAction Stop).Path
    }

    $selectedProjects = Resolve-ProjectClosure -Roots $roots
}
elseif ($Profile -eq "all") {
    $selectedProjects = Get-AllPackableProjects -Root $repoRoot
}
else {
    $seedRelative = Get-SeedProjectsForProfile -ProfileName $Profile -IncludeSystem $IncludeSystemPlugin.IsPresent -IncludeDemo $IncludeDemoPlugin.IsPresent
    $roots = @()
    foreach ($relative in $seedRelative) {
        $roots += Normalize-Path (Join-Path $repoRoot $relative)
    }

    $selectedProjects = Resolve-ProjectClosure -Roots $roots
}

if (-not $selectedProjects -or $selectedProjects.Count -eq 0) {
    throw "No packable projects selected."
}

$outputPath = Join-Path $repoRoot $OutputDir
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

Write-Host "Profile: $Profile" -ForegroundColor Cyan
Write-Host "Packaging projects:" -ForegroundColor Cyan
$selectedProjects | ForEach-Object { Write-Host " - $_" }
Write-Host "Output: $outputPath" -ForegroundColor Cyan

if (-not $SkipRestore) {
    Write-Host "Running restore..." -ForegroundColor Yellow
    dotnet restore Procedo.sln
}

$commonArgs = @(
    "pack",
    "--configuration", $Configuration,
    "--output", $outputPath,
    "-p:IncludeSymbols=true",
    "-p:SymbolPackageFormat=snupkg"
)

if ($SkipBuild) {
    $commonArgs += "--no-build"
}

if ($Version) {
    $commonArgs += "-p:PackageVersion=$Version"
}

foreach ($project in $selectedProjects) {
    Write-Host "Packing $project" -ForegroundColor Yellow
    $args = @($commonArgs + $project)
    & dotnet @args
}

if ($Profile -eq "public") {
    $allowList = Get-PublicDependencyAllowList
    Get-ChildItem -Path $outputPath -Filter *.nupkg | ForEach-Object {
        Update-NuspecDependencies -PackagePath $_.FullName -AllowList $allowList
    }
}

Write-Host "NuGet packing complete." -ForegroundColor Green
Get-ChildItem -Path $outputPath -Filter *.nupkg | ForEach-Object {
    Write-Host " - $($_.Name)"
}
