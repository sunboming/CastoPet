[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",

    [switch]$SkipTests,

    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$ArtifactsRoot = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot "artifacts\packages"))

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Assert-ChildPath {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Parent
    )

    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($fullParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Packaging path '$fullPath' must stay under '$fullParent'."
    }
}

Push-Location $RepositoryRoot
try {
    if ([string]::IsNullOrWhiteSpace($Version)) {
        [xml]$sharedProperties = Get-Content -LiteralPath "Directory.Build.props" -Raw
        $Version = [string]$sharedProperties.Project.PropertyGroup.VersionPrefix
    }

    if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
        throw "Version '$Version' is not a supported semantic version."
    }

    $gitStatus = @(& git status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect the Git working tree."
    }

    $isDirty = $gitStatus.Count -gt 0
    if ($isDirty -and !$AllowDirty) {
        throw "Packaging requires a clean Git working tree. Use -AllowDirty only for a local smoke test."
    }

    $commit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) {
        throw "Could not resolve the source commit."
    }

    $identity = [ordered]@{ PackageId = "CastoPet"; DisplayName = "CastoPet" }
    $PackageRoot = [IO.Path]::GetFullPath((Join-Path $ArtifactsRoot $Version))
    Assert-ChildPath -Path $PackageRoot -Parent $ArtifactsRoot
    if (Test-Path -LiteralPath $PackageRoot) {
        Remove-Item -LiteralPath $PackageRoot -Recurse -Force
    }

    $PublishDirectory = Join-Path $PackageRoot "publish"
    $OutputDirectory = Join-Path $PackageRoot "packages"
    New-Item -ItemType Directory -Force -Path $PublishDirectory, $OutputDirectory | Out-Null

    Invoke-Checked -FilePath "dotnet" -Arguments @("tool", "restore")
    Invoke-Checked -FilePath "dotnet" -Arguments @(
        "restore",
        "CastoPet.sln")
    if (!$SkipTests) {
        Invoke-Checked -FilePath "dotnet" -Arguments @(
            "run",
            "--project", "tests/CastoPet.Tests/CastoPet.Tests.csproj",
            "-c", "Release",
            "--no-restore")
    }

    Invoke-Checked -FilePath "dotnet" -Arguments @(
        "restore",
        "src/CastoPet/CastoPet.csproj",
        "-r", $RuntimeIdentifier)
    Invoke-Checked -FilePath "dotnet" -Arguments @(
        "publish",
        "src/CastoPet/CastoPet.csproj",
        "-c", "Release",
        "-r", $RuntimeIdentifier,
        "--self-contained", "true",
        "--no-restore",
        "-p:Version=$Version",
        "-p:DebugSymbols=false",
        "-p:DebugType=None",
        "-o", $PublishDirectory)

    $mainExecutable = Join-Path $PublishDirectory "CastoPet.exe"
    if (!(Test-Path -LiteralPath $mainExecutable -PathType Leaf)) {
        throw "Published payload does not contain CastoPet.exe."
    }

    Invoke-Checked -FilePath "dotnet" -Arguments @(
        "tool", "run", "vpk", "--", "pack",
        "--packId", $identity.PackageId,
        "--packVersion", $Version,
        "--packDir", $PublishDirectory,
        "--mainExe", "CastoPet.exe",
        "--packTitle", $identity.DisplayName,
        "--runtime", $RuntimeIdentifier,
        "--icon", (Join-Path $RepositoryRoot "src\CastoPet\Assets\AppIcon.ico"),
        "--outputDir", $OutputDirectory)

    $packageFiles = @(Get-ChildItem -LiteralPath $OutputDirectory -File | Sort-Object Name)
    if (!($packageFiles | Where-Object Extension -eq ".exe")) {
        throw "Velopack did not produce an installer executable."
    }

    $metadataFiles = @($packageFiles | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        [ordered]@{
            name = $_.Name
            bytes = $_.Length
            sha256 = $hash.Hash.ToLowerInvariant()
        }
    })
    $metadata = [ordered]@{
        schemaVersion = 1
        packageId = $identity.PackageId
        displayName = $identity.DisplayName
        version = $Version
        runtimeIdentifier = $RuntimeIdentifier
        sourceCommit = $commit
        dirtySource = $isDirty
        unsigned = $true
        files = $metadataFiles
    }
    $metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PackageRoot "build-metadata.json") -Encoding utf8NoBOM

    Write-Output "Package created: $OutputDirectory"
}
finally {
    Pop-Location
}
