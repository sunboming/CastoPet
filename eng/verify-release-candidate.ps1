[CmdletBinding()]
param(
    [string]$Version,

    [string]$PackageRoot,

    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Normalize-Markdown([string]$Value) {
    return $Value.Replace("`r`n", "`n").Trim()
}

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
[xml]$sharedProperties = Get-Content -LiteralPath (Join-Path $repositoryRoot "Directory.Build.props") -Raw
$repositoryVersion = [string]$sharedProperties.Project.PropertyGroup.VersionPrefix
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = $repositoryVersion
}
elseif ($Version -ne $repositoryVersion) {
    throw "Candidate version '$Version' must match Directory.Build.props version '$repositoryVersion'."
}

if ([string]::IsNullOrWhiteSpace($PackageRoot)) {
    $PackageRoot = Join-Path $repositoryRoot "artifacts\packages\$Version"
}
$PackageRoot = [IO.Path]::GetFullPath($PackageRoot)
$packageDirectory = Join-Path $PackageRoot "packages"
$metadataPath = Join-Path $PackageRoot "build-metadata.json"
$notesPath = Join-Path $repositoryRoot "docs\release-notes\$Version.md"

foreach ($requiredPath in @($packageDirectory, $metadataPath, $notesPath)) {
    if (!(Test-Path -LiteralPath $requiredPath)) {
        throw "Required candidate input '$requiredPath' does not exist."
    }
}

$notes = Normalize-Markdown (Get-Content -LiteralPath $notesPath -Raw)
if ([string]::IsNullOrWhiteSpace($notes) -or $notes.Contains("在此填写", [StringComparison]::Ordinal)) {
    throw "Release notes '$notesPath' are empty or still contain the generated placeholder."
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([string]$metadata.version -ne $Version) {
    throw "Build metadata version '$($metadata.version)' does not match '$Version'."
}
if (!$AllowDirty -and [bool]$metadata.dirtySource) {
    throw "Official release candidates must not be built from a dirty worktree."
}

$headCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve the candidate source commit."
}
if (!$AllowDirty -and [string]$metadata.sourceCommit -ne $headCommit) {
    throw "Candidate source commit '$($metadata.sourceCommit)' does not match HEAD '$headCommit'."
}

$requiredAssets = @(
    "CastoPet-win-Setup.msi",
    "CastoPet-win-Portable.zip",
    "CastoPet-$Version-full.nupkg",
    "releases.win.json")
$metadataFiles = @{}
foreach ($file in $metadata.files) {
    $metadataFiles[[string]$file.name] = $file
}
foreach ($assetName in $requiredAssets) {
    $assetPath = Join-Path $packageDirectory $assetName
    if (!(Test-Path -LiteralPath $assetPath -PathType Leaf)) {
        throw "Required candidate asset '$assetPath' does not exist."
    }
    if (!$metadataFiles.ContainsKey($assetName)) {
        throw "Build metadata does not describe '$assetName'."
    }
    $actualHash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $expectedHash = ([string]$metadataFiles[$assetName].sha256).ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        throw "SHA256 mismatch for '$assetName'."
    }
}

$feedPath = Join-Path $packageDirectory "releases.win.json"
$feed = Get-Content -LiteralPath $feedPath -Raw | ConvertFrom-Json
$fullAssets = @($feed.Assets | Where-Object { $_.Type -eq "Full" -and $_.Version -eq $Version })
if ($fullAssets.Count -ne 1) {
    throw "Update feed must contain exactly one full asset for version '$Version'."
}
$fullAsset = $fullAssets[0]
$nupkgName = "CastoPet-$Version-full.nupkg"
if ([string]$fullAsset.FileName -ne $nupkgName) {
    throw "Update feed points to '$($fullAsset.FileName)' instead of '$nupkgName'."
}
if ((Normalize-Markdown ([string]$fullAsset.NotesMarkdown)) -ne $notes) {
    throw "Update feed release notes do not match '$notesPath'."
}
$nupkgPath = Join-Path $packageDirectory $nupkgName
$nupkgHash = (Get-FileHash -LiteralPath $nupkgPath -Algorithm SHA256).Hash
if ($nupkgHash -ne [string]$fullAsset.SHA256) {
    throw "Update feed SHA256 does not match '$nupkgName'."
}

$archive = [IO.Compression.ZipFile]::OpenRead($nupkgPath)
try {
    $nuspecEntry = @($archive.Entries | Where-Object { $_.Name -like "*.nuspec" })
    if ($nuspecEntry.Count -ne 1) {
        throw "Candidate package must contain exactly one nuspec."
    }
    $reader = [IO.StreamReader]::new($nuspecEntry[0].Open())
    try {
        [xml]$nuspec = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}
finally {
    $archive.Dispose()
}
if ([string]$nuspec.package.metadata.version -ne $Version) {
    throw "Nupkg version does not match '$Version'."
}
$nuspecNotes = Normalize-Markdown ([string]$nuspec.package.metadata.releaseNotes.InnerText)
if ($nuspecNotes -ne $notes) {
    throw "Nupkg release notes do not match '$notesPath'."
}

$portablePath = Join-Path $packageDirectory "CastoPet-win-Portable.zip"
$portable = [IO.Compression.ZipFile]::OpenRead($portablePath)
try {
    if (!($portable.Entries | Where-Object { $_.FullName -eq "CastoPet.exe" })) {
        throw "Portable package does not contain the root CastoPet.exe entry."
    }
}
finally {
    $portable.Dispose()
}

Write-Output "Release candidate verified: CastoPet $Version"
Write-Output "Source commit: $($metadata.sourceCommit)"
Write-Output "Package directory: $packageDirectory"
