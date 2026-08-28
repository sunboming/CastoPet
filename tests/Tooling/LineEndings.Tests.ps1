#requires -Version 7.0
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$checker = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../../eng/check-line-endings.ps1"))
$root = Join-Path ([IO.Path]::GetTempPath()) ("castopet-eol-" + [Guid]::NewGuid().ToString('N'))
$utf8 = [Text.UTF8Encoding]::new($false)

function Assert-True([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw $Message }
}

function Invoke-Check([bool]$ShouldPass, [switch]$Fix) {
    $arguments = @('-NoProfile', '-File', $checker, '-RepositoryRoot', $root)
    if ($Fix) { $arguments += '-Fix' }
    $result = & pwsh @arguments 2>&1
    $succeeded = $LASTEXITCODE -eq 0
    Assert-True ($succeeded -eq $ShouldPass) ("Unexpected checker result: " + ($result -join "`n"))
}

try {
    New-Item -ItemType Directory -Path $root | Out-Null
    & git init --quiet $root
    if ($LASTEXITCODE -ne 0) { throw 'Fixture git init failed.' }
    $attributes = "* text=auto eol=crlf`r`n*.sh text eol=lf`r`n*.png -text -eol`r`n"
    [IO.File]::WriteAllText((Join-Path $root '.gitattributes'), $attributes, $utf8)
    $codePath = Join-Path $root ('sample ' + [char]0x4E2D + '.cs')
    $scriptPath = Join-Path $root 'run.sh'
    $assetPath = Join-Path $root 'image.png'
    $code = "// first`r`n// second`r`n"
    [IO.File]::WriteAllText($codePath, $code, [Text.UTF8Encoding]::new($true))
    [IO.File]::WriteAllText($scriptPath, "#!/bin/sh`necho ok`n", $utf8)
    [byte[]]$asset = @(137, 80, 78, 71, 0, 13, 10, 10, 13, 255)
    [IO.File]::WriteAllBytes($assetPath, $asset)
    [IO.File]::WriteAllText((Join-Path $root 'empty.md'), '', $utf8)
    [IO.File]::WriteAllText((Join-Path $root 'single.json'), '{}', $utf8)
    & git -C $root add --all
    if ($LASTEXITCODE -ne 0) { throw 'Fixture git add failed.' }
    [IO.File]::WriteAllText((Join-Path $root 'untracked.cs'), "leave`nthis`n", $utf8)
    Invoke-Check $true
    Write-Output 'PASS valid CRLF text, LF shell, empty files, binary and untracked exclusions'

    [IO.File]::WriteAllText($codePath, "// first`r`n// second`n", [Text.UTF8Encoding]::new($true))
    $before = [Convert]::ToHexString([IO.File]::ReadAllBytes($codePath))
    Invoke-Check $false
    Assert-True ($before -eq [Convert]::ToHexString([IO.File]::ReadAllBytes($codePath))) 'Check mode must not modify files.'
    Invoke-Check $true -Fix
    Invoke-Check $true
    [byte[]]$expected = [Text.UTF8Encoding]::new($true).GetPreamble() + $utf8.GetBytes($code)
    Assert-True ([Convert]::ToHexString($expected) -eq [Convert]::ToHexString([IO.File]::ReadAllBytes($codePath))) 'Fix must preserve BOM and all non-newline bytes.'
    Write-Output 'PASS mixed line endings rejected; fix preserves content and BOM'

    [IO.File]::WriteAllText($codePath, $code.Replace("`r`n", "`n"), $utf8)
    [IO.File]::WriteAllText($scriptPath, "#!/bin/sh`r`necho ok`r`n", $utf8)
    Invoke-Check $false
    Invoke-Check $true -Fix
    Invoke-Check $true
    Assert-True ([Convert]::ToHexString($asset) -eq [Convert]::ToHexString([IO.File]::ReadAllBytes($assetPath))) 'Binary assets must not change.'
    Assert-True ([IO.File]::ReadAllText((Join-Path $root 'untracked.cs')) -eq "leave`nthis`n") 'Untracked files must not change.'
    Write-Output 'PASS wrong pure line endings rejected; both directions normalize without touching unrelated files'
}
finally {
    $fullRoot = [IO.Path]::GetFullPath($root)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if ($fullRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        [IO.Path]::GetFileName($fullRoot) -like 'castopet-eol-*' -and (Test-Path -LiteralPath $fullRoot)) {
        Remove-Item -LiteralPath $fullRoot -Recurse -Force
    }
}
