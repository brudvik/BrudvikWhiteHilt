<#
.SYNOPSIS
    Renders the White Hilt darkwood doors, walls, gable walls and double gate offline with the mod's own cutting code.
.DESCRIPTION
    Copies Pieces/Structure/Darkwood/MeshData.cs and GateJoinery.cs (rewritten to block namespaces, which Unity's C#
    needs) and AssetSource/Unity/DarkwoodPreview.cs into the git-ignored BrudvikWhiteHiltUnity project, which reads the
    vanilla darkwood gate straight from the game's asset bundles. A check sheet per piece (front, back, opened) lands in
    BrudvikWhiteHiltUnity/Preview/darkwood (or -Out). -Docs also writes the pictures for the documentation into
    docs/images/darkwood_*.png, cropped like render_showcase.py does.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File AssetSource\Preview\render_darkwood.ps1 -Docs
#>
param(
    [string]$Out = '',
    [switch]$Docs,
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\Valheim",
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
if (-not $Out) {
    $Out = Join-Path $project 'Preview\darkwood'
}
$logFile = Join-Path $env:TEMP 'whitehilt_darkwood.log'

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    throw "Run AssetSource\build_foraging_bundle.ps1 once first; it creates the Unity project."
}

$target = Join-Path $project 'Assets\Editor\Darkwood'
New-Item -ItemType Directory -Force $target | Out-Null
foreach ($file in 'MeshData.cs', 'GateJoinery.cs') {
    $source = Get-Content (Join-Path $repoRoot "BrudvikWhiteHilt\Pieces\Structure\Darkwood\$file") -Raw -Encoding UTF8
    $converted = [regex]::Replace($source, '(?m)^namespace ([\w\.]+);', 'namespace $1 {') + "`n}`n"
    Set-Content -Path (Join-Path $target $file) -Value $converted -Encoding UTF8
}
Copy-Item (Join-Path $repoRoot 'AssetSource\Unity\DarkwoodPreview.cs') $target -Force

$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'DarkwoodPreview.Render',
    '-dwGame', "`"$Game`"", '-dwOut', "`"$Out`"", '-logFile', "`"$logFile`"")
$docsRaw = Join-Path $Out 'docs'
if ($Docs) {
    New-Item -ItemType Directory -Force $docsRaw | Out-Null
    $arguments += @('-dwDocs', "`"$docsRaw`"")
}

# Unity.exe is a GUI application, so the call operator would not wait for it. No -nographics: the cameras must render.
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -NoNewWindow
Select-String -Path $logFile -Pattern '\[Darkwood\]|error CS|Exception' | ForEach-Object { Write-Host $_.Line }
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logFile"
}

if ($Docs) {
    python (Join-Path $PSScriptRoot 'crop_pictures.py') $docsRaw (Join-Path $repoRoot 'docs\images')
}
