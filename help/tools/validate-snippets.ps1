param(
    [string]$ManifestPath = "help/tests/snippet-tests/commands.json",
    [string]$RepoRoot = "."
)

$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param(
        [string]$BasePath,
        [string]$ChildPath
    )

    $combined = Join-Path $BasePath $ChildPath
    return [System.IO.Path]::GetFullPath($combined)
}

$repoRootPath = [System.IO.Path]::GetFullPath($RepoRoot)
$manifestFullPath = Resolve-RepoPath -BasePath $repoRootPath -ChildPath $ManifestPath

if (-not (Test-Path $manifestFullPath)) {
    throw "Manifest file not found: $manifestFullPath"
}

$items = Get-Content $manifestFullPath -Raw | ConvertFrom-Json

if (-not $items -or $items.Count -eq 0) {
    throw "No snippet commands found in manifest: $manifestFullPath"
}

$failures = @()

Write-Host "Validating $($items.Count) documented commands from $manifestFullPath" -ForegroundColor Cyan
Write-Host "Commands are executed sequentially to avoid concurrent dotnet restore/build races." -ForegroundColor Cyan

foreach ($item in $items) {
    $cwd = Resolve-RepoPath -BasePath $repoRootPath -ChildPath $item.cwd

    Write-Host ""
    Write-Host "[$($item.id)] $($item.description)" -ForegroundColor Yellow
    Write-Host "cwd: $cwd"
    Write-Host "cmd: $($item.command)"

    Push-Location $cwd
    try {
        Invoke-Expression $item.command
        $actualExitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($actualExitCode -ne $item.expectedExitCode) {
        $failures += [PSCustomObject]@{
            Id = $item.id
            ExpectedExitCode = $item.expectedExitCode
            ActualExitCode = $actualExitCode
        }

        Write-Host "Result: FAIL (expected exit code $($item.expectedExitCode), got $actualExitCode)" -ForegroundColor Red
    }
    else {
        Write-Host "Result: PASS" -ForegroundColor Green
    }
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Snippet validation failed." -ForegroundColor Red
    $failures | Format-Table -AutoSize
    exit 1
}

Write-Host ""
Write-Host "All documented commands validated successfully." -ForegroundColor Green
