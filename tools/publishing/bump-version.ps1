# Sets the version in the three places that must agree and dates the "Unreleased" changelog heading.
# Usage: .\tools\publishing\bump-version.ps1 -Version 0.125.0 [-Date 2026-10-12]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Date = (Get-Date -Format 'yyyy-MM-dd')
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$project = Join-Path $root 'BrudvikWhiteHilt'

$pluginFile = Join-Path $project 'BrudvikWhiteHilt.cs'
$current = [regex]::Match([IO.File]::ReadAllText($pluginFile), 'PluginVersion = "([0-9.]+)"').Groups[1].Value
if (-not $current) { throw 'PluginVersion not found in BrudvikWhiteHilt.cs' }
if ($current -eq $Version) { throw "Already at $Version" }

# AssemblyInfo.cs is Latin-1 encoded and carries four-part versions.
$latin1 = [Text.Encoding]::GetEncoding(28591)
$assemblyInfo = Join-Path $project 'Properties\AssemblyInfo.cs'
$text = [IO.File]::ReadAllText($assemblyInfo, $latin1).Replace("`"$current.0`"", "`"$Version.0`"")
[IO.File]::WriteAllText($assemblyInfo, $text, $latin1)

# The other files keep the BOM they already have.
$replacements = @{
    $pluginFile                                 = @("PluginVersion = `"$current`"", "PluginVersion = `"$Version`"")
    (Join-Path $project 'Package\manifest.json') = @("`"version_number`": `"$current`"", "`"version_number`": `"$Version`"")
    (Join-Path $root 'CHANGELOG.md')            = @('## Unreleased', "## v$Version - $Date")
}
foreach ($file in $replacements.Keys) {
    $bytes = [IO.File]::ReadAllBytes($file)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $content = [IO.File]::ReadAllText($file).Replace($replacements[$file][0], $replacements[$file][1])
    [IO.File]::WriteAllText($file, $content, (New-Object Text.UTF8Encoding $bom))
}

Write-Output "Version $current -> $Version"
Select-String -Path $assemblyInfo -Pattern 'Version\("0' | ForEach-Object { $_.Line }
Select-String -Path $pluginFile -Pattern 'PluginVersion =' | ForEach-Object { $_.Line }
Select-String -Path (Join-Path $project 'Package\manifest.json') -Pattern 'version_number' | ForEach-Object { $_.Line }
Select-String -Path (Join-Path $root 'CHANGELOG.md') -Pattern '^## ' | Select-Object -First 2 | ForEach-Object { $_.Line }
