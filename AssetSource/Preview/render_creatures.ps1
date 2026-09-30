<#
.SYNOPSIS
    Renders a contact sheet of every animated creature (all animator states, front and side) without starting the game.
.DESCRIPTION
    Needs the creature prefabs that AssetSource\build_foraging_bundle.ps1 builds in the git-ignored BrudvikWhiteHiltUnity
    project. Writes <creature>_anim.png to BrudvikWhiteHiltUnity\Preview\out (or -Out). In the front view the creature's
    front (+z) faces the camera.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File AssetSource\Preview\render_creatures.ps1
#>
param(
    [string]$Out = '',
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
if (-not $Out) {
    $Out = Join-Path $project 'Preview\out'
}
$logFile = Join-Path $env:TEMP 'whitehilt_creature_preview.log'

if (-not (Test-Path (Join-Path $project 'Assets\Creatures'))) {
    throw "Run AssetSource\build_foraging_bundle.ps1 first; it builds the creature prefabs."
}

$editorScripts = Join-Path $project 'Assets\Editor'
Copy-Item (Join-Path $repoRoot 'AssetSource\Unity\CreaturePreview.cs') $editorScripts -Force

# Unity.exe is a GUI application, so the call operator would not wait for it. No -nographics: the camera must render.
$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'CreaturePreview.Render',
    '-previewOut', "`"$Out`"", '-logFile', "`"$logFile`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
Select-String -Path $logFile -Pattern '\[WhiteHilt\]' | ForEach-Object { Write-Host $_.Line }
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logFile"
}
Write-Host "Sheets in $Out"
