<#
.SYNOPSIS
    Renders the White Hilt roofs offline with the mod's own mesh code, one image per covering.
.DESCRIPTION
    Copies Pieces/Roofs/RoofMeshBuilder.cs and RoofCoverings.cs (rewritten to block namespaces, which Unity's C# needs)
    and AssetSource/Unity/RoofPreview.cs into the git-ignored BrudvikWhiteHiltUnity project, exports the longship's
    dragon head on first run, and renders with the textures in AssetSource/Textures. Images land in
    BrudvikWhiteHiltUnity/Preview/roofs (or -Out). Run make_roof_textures.py first. -Docs also renders each covering's
    house on a transparent background into docs/images/roof_<covering>.png, cropped like render_showcase.py does.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File AssetSource\Preview\render_roofs.ps1 -Docs
#>
param(
    [string]$Out = '',
    [switch]$Docs,
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
$meshes = Join-Path $project 'Preview\vanilla'
if (-not $Out) {
    $Out = Join-Path $project 'Preview\roofs'
}
$logFile = Join-Path $env:TEMP 'whitehilt_roofs.log'
$wood = Join-Path $env:TEMP 'wh_roof_textures\dark_wooden_planks_Diffuse_1k.jpg'

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    throw "Run AssetSource\build_foraging_bundle.ps1 once first; it creates the Unity project."
}
if (-not (Test-Path $wood)) {
    throw "Run AssetSource\Tools\make_roof_textures.py first; it downloads the wood texture."
}

if (-not (Test-Path (Join-Path $meshes 'dragon_head.json'))) {
    New-Item -ItemType Directory -Force $meshes | Out-Null
    python (Join-Path $PSScriptRoot 'export_dragon_head.py') (Join-Path $meshes 'dragon_head.json')
}

$target = Join-Path $project 'Assets\Editor\Roofs'
New-Item -ItemType Directory -Force $target | Out-Null
foreach ($file in 'RoofMeshBuilder.cs', 'RoofCoverings.cs') {
    $source = Get-Content (Join-Path $repoRoot "BrudvikWhiteHilt\Pieces\Roofs\$file") -Raw
    $converted = [regex]::Replace($source, '(?m)^namespace ([\w\.]+);', 'namespace $1 {') + "`n}`n"
    Set-Content -Path (Join-Path $target $file) -Value $converted -Encoding UTF8
}
Copy-Item (Join-Path $repoRoot 'AssetSource\Unity\RoofPreview.cs') $target -Force

$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'RoofPreview.Render',
    '-roofTextures', "`"$(Join-Path $repoRoot 'AssetSource\Textures')`"", '-roofWood', "`"$wood`"",
    '-roofMeshes', "`"$meshes`"", '-roofOut', "`"$Out`"", '-logFile', "`"$logFile`"")
$docsRaw = Join-Path $Out 'docs'
if ($Docs) {
    New-Item -ItemType Directory -Force $docsRaw | Out-Null
    $arguments += @('-roofDocs', "`"$docsRaw`"")
}

# Unity.exe is a GUI application, so the call operator would not wait for it. No -nographics: the cameras must render.
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
Select-String -Path $logFile -Pattern '\[Roofs\]|error CS|Exception' | ForEach-Object { Write-Host $_.Line }
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logFile"
}

if ($Docs) {
    python (Join-Path $PSScriptRoot 'crop_pictures.py') $docsRaw (Join-Path $repoRoot 'docs\images')
}
