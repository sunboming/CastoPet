param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts\local-package'))
$expectedPrefix = $repoRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $artifactRoot.StartsWith($expectedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Packaging output must remain inside the repository."
}

$publishDir = Join-Path $artifactRoot 'publish'
$packageDir = Join-Path $artifactRoot 'packages'
$projectFile = Join-Path $repoRoot 'src\CastoPet\CastoPet.csproj'

if (Test-Path -LiteralPath $artifactRoot) {
    Remove-Item -LiteralPath $artifactRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "Could not restore vpk 1.2.0." }

    dotnet publish $projectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:Version=$Version `
        -o $publishDir
    if ($LASTEXITCODE -ne 0) { throw "CastoPet publish failed." }

    dotnet tool run vpk pack `
        --packId CastoPet.App `
        --packVersion $Version `
        --packDir $publishDir `
        --mainExe CastoPet.exe `
        --packTitle CastoPet `
        --outputDir $packageDir
    if ($LASTEXITCODE -ne 0) { throw "Velopack package creation failed." }
}
finally {
    Pop-Location
}

Write-Host "Local unsigned packages created at: $packageDir"
