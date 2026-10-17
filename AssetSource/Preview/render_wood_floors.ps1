<#
.SYNOPSIS
    Renders the White Hilt wood floor triangles, angled ends and octagons offline with the mod's own cutting code.
.DESCRIPTION
    Copies Pieces/Structure/Darkwood/MeshData.cs and Pieces/Structure/WoodFloors/FloorCutting.cs (rewritten to block
    namespaces, which Unity's C# needs) and AssetSource/Unity/WoodFloorPreview.cs into the git-ignored
    BrudvikWhiteHiltUnity project (MeshData to Assets/Editor/Shared, where the darkwood preview puts it too), which reads the vanilla wood floor and wall straight from the game's asset bundles. A
    check sheet per floor (from above, the side and below) and a tower of vanilla walls on the octagon land in
    BrudvikWhiteHiltUnity/Preview/wood_floors (or -Out). -Docs also writes the pictures for the documentation into
    docs/images/wood_floor_*.png, cropped like render_showcase.py does.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File AssetSource\Preview\render_wood_floors.ps1 -Docs
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
    $Out = Join-Path $project 'Preview\wood_floors'
}
$logFile = Join-Path $env:TEMP 'whitehilt_wood_floors.log'

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    throw "Run AssetSource\build_foraging_bundle.ps1 once first; it creates the Unity project."
}

# MeshData goes to Assets\Editor\Shared, as the darkwood preview puts it: two copies in one project do not compile.
$target = Join-Path $project 'Assets\Editor\WoodFloors'
$shared = Join-Path $project 'Assets\Editor\Shared'
New-Item -ItemType Directory -Force $target, $shared | Out-Null
$sources = @{
    'BrudvikWhiteHilt\Pieces\Structure\Darkwood\MeshData.cs'       = $shared
    'BrudvikWhiteHilt\Pieces\Structure\WoodFloors\FloorCutting.cs' = $target
}
foreach ($file in $sources.Keys) {
    $source = Get-Content (Join-Path $repoRoot $file) -Raw -Encoding UTF8
    $converted = [regex]::Replace($source, '(?m)^namespace ([\w\.]+);', 'namespace $1 {') + "`n}`n"
    Set-Content -Path (Join-Path $sources[$file] (Split-Path $file -Leaf)) -Value $converted -Encoding UTF8
}
Copy-Item (Join-Path $repoRoot 'AssetSource\Unity\WoodFloorPreview.cs') $target -Force
Remove-Item (Join-Path $project 'Assets\Editor\Darkwood\MeshData.cs*') -ErrorAction SilentlyContinue

$arguments = @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'WoodFloorPreview.Render',
    '-wfGame', "`"$Game`"", '-wfOut', "`"$Out`"", '-logFile', "`"$logFile`"")
$docsRaw = Join-Path $Out 'docs'
if ($Docs) {
    New-Item -ItemType Directory -Force $docsRaw | Out-Null
    $arguments += @('-wfDocs', "`"$docsRaw`"")
}

# Unity.exe is a GUI application, so the call operator would not wait for it. No -nographics: the cameras must render.
# Wait for Unity itself only: -Wait also waits for helpers it leaves running, such as the licensing client.
# The handle is taken at once, or the exit code reads empty afterwards.
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -NoNewWindow
$null = $process.Handle
$process.WaitForExit()
Select-String -Path $logFile -Pattern '\[WoodFloor\]|error CS|Exception' | ForEach-Object { Write-Host $_.Line }
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See $logFile"
}

if ($Docs) {
    python (Join-Path $PSScriptRoot 'crop_pictures.py') $docsRaw (Join-Path $repoRoot 'docs\images')
}
