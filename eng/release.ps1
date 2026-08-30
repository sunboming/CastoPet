[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$NotesFile,

    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$Repository = "sunboming/CastoPet"
$Remote = "origin"
$Tag = "v$Version"

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

function Invoke-Captured {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = @(& $FilePath @Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }

    return ($output -join [Environment]::NewLine).Trim()
}

function Get-LocalTagCommit {
    param([Parameter(Mandatory)][string]$Name)

    $output = @(& git rev-parse --verify "refs/tags/$Name^{commit}" 2>$null)
    if ($LASTEXITCODE -eq 0) {
        return ($output -join [Environment]::NewLine).Trim()
    }

    return $null
}

Push-Location $RepositoryRoot
try {
    foreach ($command in @("git", "gh", "pwsh")) {
        if (!(Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Required command '$command' is not available."
        }
    }

    [xml]$sharedProperties = Get-Content -LiteralPath "Directory.Build.props" -Raw
    $repositoryVersion = [string]$sharedProperties.Project.PropertyGroup.VersionPrefix
    if ($repositoryVersion -ne $Version) {
        throw "Release version '$Version' must match Directory.Build.props version '$repositoryVersion'."
    }

    $resolvedNotesFile = $null
    if (![string]::IsNullOrWhiteSpace($NotesFile)) {
        if (!(Test-Path -LiteralPath $NotesFile -PathType Leaf)) {
            throw "Release notes file '$NotesFile' does not exist."
        }
        $resolvedNotesFile = (Resolve-Path -LiteralPath $NotesFile).Path
    }

    $gitStatus = @(& git status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect the Git working tree."
    }
    if ($gitStatus.Count -gt 0) {
        throw "Release creation requires a clean Git working tree. Commit the intended release inputs first."
    }

    $branch = Invoke-Captured -FilePath "git" -Arguments @("branch", "--show-current")
    if ([string]::IsNullOrWhiteSpace($branch)) {
        throw "Release creation requires a named branch, not a detached HEAD."
    }

    $headCommit = Invoke-Captured -FilePath "git" -Arguments @("rev-parse", "HEAD")
    $remoteUrl = Invoke-Captured -FilePath "git" -Arguments @("remote", "get-url", $Remote)
    if ($remoteUrl -notmatch 'github\.com[:/]sunboming/CastoPet(?:\.git)?$') {
        throw "Remote '$Remote' must point to github.com/sunboming/CastoPet before publishing."
    }

    Invoke-Checked -FilePath "gh" -Arguments @("auth", "status")

    $releaseView = @(& gh release view $Tag --repo $Repository --json isDraft,url 2>$null)
    $existingRelease = $null
    if ($LASTEXITCODE -eq 0) {
        $existingRelease = ($releaseView -join [Environment]::NewLine) | ConvertFrom-Json
        if (!$existingRelease.isDraft) {
            throw "Release '$Tag' is already published and will not be modified."
        }
    }

    $localTagCommit = Get-LocalTagCommit -Name $Tag
    if ($null -ne $localTagCommit -and $localTagCommit -ne $headCommit) {
        throw "Local tag '$Tag' points to $localTagCommit instead of current commit $headCommit."
    }

    $remoteTagLines = @(& git ls-remote --tags $Remote "refs/tags/$Tag" "refs/tags/$Tag^{}")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect remote tag '$Tag'."
    }
    $remoteTagCommits = @($remoteTagLines | ForEach-Object { ($_ -split '\s+')[0] })
    if ($remoteTagCommits.Count -gt 0 -and $headCommit -notin $remoteTagCommits) {
        throw "Remote tag '$Tag' does not point to current commit $headCommit."
    }

    $packageArguments = @("-NoProfile", "-File", (Join-Path $PSScriptRoot "package.ps1"), "-Version", $Version)
    if ($SkipTests) {
        $packageArguments += "-SkipTests"
    }
    Invoke-Checked -FilePath "pwsh" -Arguments $packageArguments

    $packageRoot = Join-Path $RepositoryRoot "artifacts\packages\$Version"
    $packageDirectory = Join-Path $packageRoot "packages"
    $metadataPath = Join-Path $packageRoot "build-metadata.json"
    $packageFiles = @(Get-ChildItem -LiteralPath $packageDirectory -File | Sort-Object Name)
    if ($packageFiles.Count -eq 0 -or !(Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Packaging did not produce the expected release assets for version $Version."
    }
    $assetPaths = @($packageFiles.FullName) + $metadataPath

    Invoke-Checked -FilePath "git" -Arguments @("push", $Remote, "HEAD:refs/heads/$branch")

    if ($null -eq $localTagCommit) {
        if ($remoteTagCommits.Count -gt 0) {
            Invoke-Checked -FilePath "git" -Arguments @("fetch", $Remote, "refs/tags/${Tag}:refs/tags/${Tag}")
        }
        else {
            Invoke-Checked -FilePath "git" -Arguments @("tag", "-a", $Tag, "-m", "CastoPet $Version")
        }
    }
    if ($remoteTagCommits.Count -eq 0) {
        Invoke-Checked -FilePath "git" -Arguments @("push", $Remote, "refs/tags/$Tag")
    }

    if ($null -ne $existingRelease) {
        $uploadArguments = @("release", "upload", $Tag, "--repo", $Repository, "--clobber") + $assetPaths
        Invoke-Checked -FilePath "gh" -Arguments $uploadArguments
        Write-Output "Draft release updated: $($existingRelease.url)"
        return
    }

    $createArguments = @(
        "release", "create", $Tag,
        "--repo", $Repository,
        "--verify-tag",
        "--draft",
        "--title", "CastoPet $Version"
    )
    if ($null -ne $resolvedNotesFile) {
        $createArguments += @("--notes-file", $resolvedNotesFile)
    }
    else {
        $createArguments += "--generate-notes"
    }
    $createArguments += $assetPaths

    Invoke-Checked -FilePath "gh" -Arguments $createArguments
    Write-Output "Draft release created. Review it before publishing:"
    Write-Output "https://github.com/$Repository/releases/tag/$Tag"
}
finally {
    Pop-Location
}
