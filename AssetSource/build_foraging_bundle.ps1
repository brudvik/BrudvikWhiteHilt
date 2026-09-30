<#
.SYNOPSIS
    Builds BrudvikWhiteHilt/Assets/whitehilt_foraging from the models in AssetSource/Models and the sounds in AssetSource/Sounds.
.DESCRIPTION
    Converts the glTF models to OBJ/PNG, copies the .wav sounds, creates the Unity project in BrudvikWhiteHiltUnity (git-ignored) on first run,
    builds the asset bundle in batch mode and copies it into the mod's embedded assets.
    Unity must be the same version as Valheim (see valheim_Data/../UnityPlayer.dll).
#>
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
$logFile = Join-Path $env:TEMP 'whitehilt_unity_build.log'
$bundleTarget = Join-Path $repoRoot 'BrudvikWhiteHilt\Assets\whitehilt_foraging'

function Invoke-Unity([string[]]$Arguments) {
    # Unity.exe is a GUI application, so the call operator would not wait for it.
    $process = Start-Process -FilePath $UnityPath -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode). See $logFile"
    }
}

if (-not (Test-Path $UnityPath)) {
    throw "Unity not found at $UnityPath"
}

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    Write-Host "Creating Unity project in $project"
    Invoke-Unity @('-batchmode', '-quit', '-createProject', "`"$project`"", '-logFile', "`"$logFile`"")
}

$foragingAssets = Join-Path $project 'Assets\Foraging'
$editorScripts = Join-Path $project 'Assets\Editor'
if (Test-Path $foragingAssets) {
    Remove-Item (Join-Path $foragingAssets '*') -Recurse -Force
}
New-Item -ItemType Directory -Force $foragingAssets, $editorScripts | Out-Null

$models = Get-ChildItem (Join-Path $PSScriptRoot 'Models') -Filter *.glb
if ($models.Count -eq 0) {
    throw "No .glb files in AssetSource\Models"
}

foreach ($model in $models) {
    # The file name, in lower case, becomes the mesh name and the texture name (<name>_albedo).
    python (Join-Path $PSScriptRoot 'convert_glb.py') $model.FullName $foragingAssets $model.BaseName.ToLowerInvariant()
    if ($LASTEXITCODE -ne 0) {
        throw "Model conversion failed for $($model.Name)"
    }
}

# The file name, in lower case, becomes the AudioClip name.
$sounds = Join-Path $PSScriptRoot 'Sounds'
if (Test-Path $sounds) {
    foreach ($sound in Get-ChildItem $sounds -Filter *.wav) {
        Copy-Item $sound.FullName (Join-Path $foragingAssets $sound.Name.ToLowerInvariant()) -Force
    }
}

Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildForagingBundle.cs') $editorScripts -Force

Write-Host "Building asset bundle (log: $logFile)"
Invoke-Unity @('-batchmode', '-quit', '-nographics', '-projectPath', "`"$project`"", '-executeMethod', 'BuildForagingBundle.Build', '-logFile', "`"$logFile`"")

Copy-Item (Join-Path $project 'AssetBundles\whitehilt_foraging') $bundleTarget -Force
Select-String -Path $logFile -Pattern '\[WhiteHilt\]' | ForEach-Object { Write-Host $_.Line }
Write-Host "Copied bundle to $bundleTarget ($((Get-Item $bundleTarget).Length) bytes)"
