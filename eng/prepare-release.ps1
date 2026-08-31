[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Patch", "Minor", "Major")]
    [string]$Bump,

    [string]$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..")),

    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$propertiesPath = Join-Path $RepositoryRoot "Directory.Build.props"
$notesDirectory = Join-Path $RepositoryRoot "docs\release-notes"

if (!(Test-Path -LiteralPath $propertiesPath -PathType Leaf)) {
    throw "Version file '$propertiesPath' does not exist."
}

if (!$AllowDirty) {
    $status = @(& git -C $RepositoryRoot status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect the Git working tree at '$RepositoryRoot'."
    }
    if ($status.Count -gt 0) {
        throw "Release preparation requires a clean Git working tree."
    }
}

$properties = Get-Content -LiteralPath $propertiesPath -Raw
[xml]$parsedProperties = $properties
$versionElements = @($parsedProperties.SelectNodes("//*[local-name()='VersionPrefix']"))
if ($versionElements.Count -ne 1) {
    throw "Directory.Build.props must contain exactly one VersionPrefix element."
}

$currentVersion = [string]$versionElements[0].InnerText
$match = [regex]::Match($currentVersion, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$')
if (!$match.Success) {
    throw "Current version '$currentVersion' is not a three-part stable semantic version."
}

$major = [int]$match.Groups["major"].Value
$minor = [int]$match.Groups["minor"].Value
$patch = [int]$match.Groups["patch"].Value
switch ($Bump) {
    "Patch" { $patch++ }
    "Minor" { $minor++; $patch = 0 }
    "Major" { $major++; $minor = 0; $patch = 0 }
}
$nextVersion = "$major.$minor.$patch"
$notesPath = Join-Path $notesDirectory "$nextVersion.md"

if (Test-Path -LiteralPath $notesPath) {
    throw "Release notes '$notesPath' already exist. No files were changed."
}

$versionPattern = '(<VersionPrefix>)' + [regex]::Escape($currentVersion) + '(</VersionPrefix>)'
$updatedProperties = [regex]::Replace(
    $properties,
    $versionPattern,
    "`${1}$nextVersion`${2}",
    [Text.RegularExpressions.RegexOptions]::CultureInvariant)
if ($updatedProperties -eq $properties) {
    throw "VersionPrefix could not be updated safely."
}

New-Item -ItemType Directory -Force -Path $notesDirectory | Out-Null
$utf8WithoutBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($propertiesPath, $updatedProperties, $utf8WithoutBom)
[IO.File]::WriteAllText(
    $notesPath,
    "- 在此填写 $nextVersion 版本变更。`r`n",
    $utf8WithoutBom)

Write-Output "Prepared CastoPet $nextVersion ($Bump from $currentVersion)."
Write-Output "Release notes: $notesPath"
Write-Output "Review the notes, run Debug and Release verification, and commit before publishing."
