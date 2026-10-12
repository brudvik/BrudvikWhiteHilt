# Creates the GitHub release for a tag, with the CHANGELOG section as notes and the package zip attached.
# The token comes from Git's credential manager and is never printed.
# Usage: .\tools\publishing\github-release.ps1 -Tag v0.125.0
param([Parameter(Mandatory = $true)][string]$Tag)
$ErrorActionPreference = 'Continue'
$repo = 'brudvik/BrudvikWhiteHilt'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')

$psi = New-Object System.Diagnostics.ProcessStartInfo 'git', 'credential fill'
$psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.UseShellExecute = $false
$p = [System.Diagnostics.Process]::Start($psi)
$p.StandardInput.Write("protocol=https`nhost=github.com`n`n"); $p.StandardInput.Close()
$out = $p.StandardOutput.ReadToEnd(); $p.WaitForExit()
$token = ($out -split "`n" | Where-Object { $_ -like 'password=*' } | Select-Object -First 1)
if (-not $token) { Write-Output 'NO TOKEN'; exit 1 }
$token = $token.Substring(9).Trim()
$headers = @{ Authorization = "Bearer $token"; Accept = 'application/vnd.github+json'; 'User-Agent' = 'BrudvikWhiteHilt-release' }

$text = [IO.File]::ReadAllText("$root\CHANGELOG.md", [Text.Encoding]::UTF8) -replace "`r`n", "`n"
$version = $Tag.TrimStart('v')
$start = $text.IndexOf("## v$version ")
if ($start -lt 0) { Write-Output "No CHANGELOG section for $Tag"; exit 1 }
$bodyStart = $text.IndexOf("`n", $start) + 1
$next = $text.IndexOf("`n## v", $bodyStart)
$notes = $text.Substring($bodyStart, $next - $bodyStart).Trim()

# Pushing a tag does not make a GitHub release, so a repeated run stops here instead of creating a second one.
try {
    $existing = Invoke-RestMethod -UseBasicParsing -Headers $headers "https://api.github.com/repos/$repo/releases/tags/$Tag"
    Write-Output "Release already exists: $($existing.html_url)"; exit 0
} catch { }

$payload = @{ tag_name = $Tag; name = $Tag; body = $notes; draft = $false; prerelease = $false } | ConvertTo-Json
try {
    $release = Invoke-RestMethod -UseBasicParsing -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' `
        -Body ([Text.Encoding]::UTF8.GetBytes($payload)) "https://api.github.com/repos/$repo/releases"
} catch {
    Write-Output "Create failed: HTTP $($_.Exception.Response.StatusCode.value__)"; exit 1
}
Write-Output "Created: $($release.html_url)"

$zip = "$root\BrudvikWhiteHilt\bin\Release\BrudvikWhiteHilt.dll.zip"
$upload = $release.upload_url -replace '\{.*\}', ''
try {
    $asset = Invoke-RestMethod -UseBasicParsing -Method Post -Headers $headers -ContentType 'application/zip' `
        -InFile $zip "$($upload)?name=BrudvikWhiteHilt.dll.zip"
    Write-Output "Attached: $($asset.name) ($($asset.size) bytes)"
} catch {
    Write-Output "Upload failed: HTTP $($_.Exception.Response.StatusCode.value__)"; exit 1
}
