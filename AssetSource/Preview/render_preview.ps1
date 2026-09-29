<#
.SYNOPSIS
    Renders preview images of pieces put together from vanilla meshes, without starting the game.
.DESCRIPTION
    Exports the vanilla meshes named in the layout (first run, or with -Export) with export_vanilla.py into the
    git-ignored BrudvikWhiteHiltUnity project, then renders every piece in the layout with Unity in batch mode.
    One PNG per piece lands in BrudvikWhiteHiltUnity/Preview/out.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File AssetSource\Preview\render_preview.ps1 -Only skansevegg
#>
param(
    [string]$Layout = (Join-Path $PSScriptRoot 'defenses.json'),
    [string]$Only = '',
    [string[]]$Export = @(),
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
$meshes = Join-Path $project 'Preview\vanilla'
$out = Join-Path $project 'Preview\out'
$logFile = Join-Path $env:TEMP 'whitehilt_preview.log'

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    throw "Run AssetSource\build_foraging_bundle.ps1 once first; it creates the Unity project."
}

# Export any mesh the layout uses that is not exported yet, plus the ones asked for.
$used = (Get-Content $Layout -Raw | ConvertFrom-Json).pieces.parts | Where-Object { $_.mesh } | ForEach-Object { $_.mesh } | Sort-Object -Unique
$missing = @($used | Where-Object { -not (Test-Path (Join-Path $meshes "$_.json")) }) + $Export
if ($missing.Count -gt 0) {
    python (Join-Path $PSScriptRoot 'export_vanilla.py') $meshes @missing
    if ($LASTEXITCODE -ne 0) {
        throw 'Exporting vanilla meshes failed'
    }
}

$editorScripts = Join-Path $project 'Assets\Editor'
New-Item -ItemType Directory -Force $editorScripts | Out-Null
Copy-Item (Join-Path $repoRoot 'AssetSource\Unity\PreviewRender.cs') $editorScripts -Force

$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'PreviewRender.Render',
    '-previewLayout', "`"$Layout`"", '-previewMeshes', "`"$meshes`"", '-previewOut', "`"$out`"", '-logFile', "`"$logFile`"")
if ($Only) {
    $arguments += @('-previewOnly', $Only)
}

# Unity.exe is a GUI application, so the call operator would not wait for it. No -nographics: the cameras must render.
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
Select-String -Path $logFile -Pattern '\[Preview\]|error CS|Exception' | ForEach-Object { Write-Host $_.Line }
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logFile"
}
