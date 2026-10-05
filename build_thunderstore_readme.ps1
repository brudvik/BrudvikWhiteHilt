<#
.SYNOPSIS
    Writes the Thunderstore README (BrudvikWhiteHilt/Package/README.md) from the GitHub README (README.MD).
.DESCRIPTION
    The GitHub README uses HTML (centred header, feature cards in tables, folding sections) that Thunderstore shows
    poorly. This turns it into plain markdown: one heading, image and line per feature card, folded sections opened up,
    and every relative link made absolute, so images and pages load from GitHub. publish.ps1 runs it for every Release
    build; edit README.MD, never the package README.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build_thunderstore_readme.ps1
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot 'README.MD'),
    [string]$Target = (Join-Path $PSScriptRoot 'BrudvikWhiteHilt\Package\README.md')
)

$ErrorActionPreference = 'Stop'
$repo = 'https://github.com/brudvik/BrudvikWhiteHilt'
$raw = 'https://raw.githubusercontent.com/brudvik/BrudvikWhiteHilt/master'

$text = [IO.File]::ReadAllText($Source, [Text.Encoding]::UTF8) -replace "`r`n", "`n"

# Feature cards: each table cell becomes a small section of its own.
$text = [regex]::Replace($text, '(?s)<td[^>]*>(.*?)</td>', {
    param($cell)
    $content = $cell.Groups[1].Value
    if ($content.Trim().Length -eq 0) { return '' }
    $title = [regex]::Match($content, '<b><a href="([^"]+)">([^<]+)</a></b>')
    $image = [regex]::Match($content, '<img src="([^"]+)"')
    $line = [regex]::Match($content, '(?s)<sub>(.*?)</sub>')
    "`n#### [$($title.Groups[2].Value)]($($title.Groups[1].Value))`n`n<img src=`"$($image.Groups[1].Value)`" alt=`"$($title.Groups[2].Value)`" height=`"120`">`n`n$($line.Groups[1].Value.Trim())`n"
})
$text = $text -replace '</?(table|tr)>', ''

# Folding sections open up under a heading of their own.
$text = $text -replace '(?s)<details>\s*<summary><b>(.*?)</b></summary>', '## $1'
$text = $text -replace '</details>', ''

# The rest of the HTML as markdown.
$text = [regex]::Replace($text, '<h1[^>]*>(.*?)</h1>', '# $1')
$text = [regex]::Replace($text, '<img src="([^"]+)" alt="([^"]*)"(?! height="120")[^>]*>', '![$2]($1)')
$text = [regex]::Replace($text, '(?s)<a href="([^"]+)">(.*?)</a>', '[$2]($1)')
$text = $text -replace '<b>(.*?)</b>', '**$1**'
$text = $text -replace '</?sub>', ''
$text = $text -replace '<br>\s*', "`n"
$text = $text -replace '</?p[^>]*>', ''

# Relative links point into the repository: images load raw, pages open on GitHub. Anchors stay as they are.
$text = [regex]::Replace($text, '\]\((?!https?:|#|mailto:)([^)]+)\)', {
    param($link)
    $path = $link.Groups[1].Value
    if ($path -match '\.(png|jpe?g|gif)$') { "]($raw/$path)" } else { "]($repo/blob/master/$path)" }
})

$text = [regex]::Replace($text, 'src="(?!https?:)([^"]+)"', { param($m) "src=`"$raw/$($m.Groups[1].Value)`"" })

# Lines that only held tags are left indented or empty.
$text = ($text -split "`n" | ForEach-Object { $_.TrimEnd() -replace '^\s+(?=[\[!#*])', '' }) -join "`n"
$text = [regex]::Replace($text, "`n{3,}", "`n`n").Trim()

$header = "<!-- Generated from README.MD by build_thunderstore_readme.ps1 when the package is built. Edit README.MD instead. -->"
[IO.File]::WriteAllText($Target, "$header`n`n$text`n", (New-Object Text.UTF8Encoding($false)))
Write-Host "Wrote $Target"
