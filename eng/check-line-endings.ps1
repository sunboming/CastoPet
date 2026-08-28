#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Join-Path $PSScriptRoot ".."),
    [switch]$Fix
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$RepositoryRoot = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($RepositoryRoot))

# NUL-delimited output preserves spaces, non-ASCII names, and embedded tabs.
$output = & git -C $RepositoryRoot ls-files --eol -z
if ($LASTEXITCODE -ne 0) { throw "Cannot enumerate tracked files in $RepositoryRoot." }
$records = ($output -join "`n").Split([char]0, [StringSplitOptions]::RemoveEmptyEntries)
$failures = [Collections.Generic.List[string]]::new()
$textCount = 0
$binaryCount = 0
$fixedCount = 0

foreach ($record in $records) {
    $parts = $record -split "`t", 2
    if ($parts.Count -ne 2 -or $parts[0] -notmatch '^i/(\S+)\s+w/(\S+)\s+attr/(.*)$') {
        throw "Unexpected git ls-files --eol output: $record"
    }
    $indexEol = $Matches[1]
    $workingEol = $Matches[2]
    $attributes = $Matches[3]
    $relativePath = $parts[1]
    if ($attributes -match '(^|\s)-text(\s|$)' -or
        ($workingEol -eq '-text' -and $attributes -match 'text=auto')) {
        $binaryCount++
        continue
    }

    $textCount++
    if ($attributes -notmatch '(^|\s)eol=(crlf|lf)(\s|$)') {
        $failures.Add("${relativePath}: no explicit eol attribute.")
        continue
    }
    $expected = $Matches[2]
    if ($indexEol -notin @('lf', 'none')) {
        $failures.Add("${relativePath}: Git index is $indexEol; expected lf (or an empty/single-line file).")
    }
    if ($workingEol -in @($expected, 'none')) { continue }
    if (!$Fix -or $workingEol -notin @('lf', 'crlf', 'mixed')) {
        $failures.Add("${relativePath}: working tree is $workingEol; expected $expected.")
        continue
    }

    $fullPath = [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $relativePath))
    $rootPrefix = $RepositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Tracked path escapes the repository: $relativePath"
    }
    $cursor = $fullPath
    while ($cursor -ne $RepositoryRoot) {
        if (([IO.File]::GetAttributes($cursor) -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing to rewrite a symbolic link or junction: $relativePath"
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }

    $bytes = [IO.File]::ReadAllBytes($fullPath)
    if ($bytes -contains 0) { throw "Refusing byte-level conversion of NUL-containing text: $relativePath" }
    # Latin1 is a reversible byte mapping here, not an encoding conversion.
    $raw = [Text.Encoding]::Latin1.GetString($bytes)
    if ([regex]::IsMatch($raw, "`r(?!`n)")) { throw "Standalone CR needs manual review: $relativePath" }
    $normalized = $raw.Replace("`r`n", "`n")
    if ($expected -eq 'crlf') { $normalized = $normalized.Replace("`n", "`r`n") }
    [IO.File]::WriteAllBytes($fullPath, [Text.Encoding]::Latin1.GetBytes($normalized))
    $fixedCount++
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Output "FAIL $_" }
    throw "Line-ending check failed for $($failures.Count) condition(s)."
}
Write-Output "Line endings OK: $textCount text, $binaryCount binary, $fixedCount normalized."
