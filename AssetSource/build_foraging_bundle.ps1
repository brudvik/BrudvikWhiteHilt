<#
.SYNOPSIS
    Builds BrudvikWhiteHilt/Assets/whitehilt_foraging from the models in AssetSource/Models, the animated creatures in
    AssetSource/Creatures and the sounds in AssetSource/Sounds.
.DESCRIPTION
    Converts the glTF models to OBJ/PNG, exports each creature with Blender to FBX, copies the .wav sounds, creates the
    Unity project in BrudvikWhiteHiltUnity (git-ignored) on first run, builds the asset bundle in batch mode and copies it
    to BrudvikWhiteHilt/Assets, from where the build puts it next to the DLL. Unity must be the same version as Valheim
    (see valheim_Data/../UnityPlayer.dll).
    -UnityOnly skips the model, sound and creature conversion and rebuilds the bundle from what is already in the Unity
    project, e.g. after changing only AssetSource/Unity/*.cs.
    The Decor Hammer's models (AssetSource/Decor/Models, made by AssetSource/Decor/prepare_decor.py) go into a second
    bundle, BrudvikWhiteHilt/Assets/whitehilt_decor, shipped as a file next to the DLL. -DecorOnly builds only that one.
    Every sound goes into a third, BrudvikWhiteHilt/Assets/whitehilt_audio: the effect sounds in AssetSource/Sounds and the
    voices, effects, ambience and music made by AssetSource/Audio/generate_audio.py. -AudioOnly builds only that one.
#>
param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.75f1\Editor\Unity.exe",
    [string]$BlenderPath = "",
    [switch]$UnityOnly,
    [switch]$DecorOnly,
    [switch]$AudioOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'BrudvikWhiteHiltUnity'
$logFile = Join-Path $env:TEMP 'whitehilt_unity_build.log'
$bundleTarget = Join-Path $repoRoot 'BrudvikWhiteHilt\Assets\whitehilt_foraging'
$foragingAssets = Join-Path $project 'Assets\Foraging'
$decorAssets = Join-Path $project 'Assets\Decor'
$decorTarget = Join-Path $repoRoot 'BrudvikWhiteHilt\Assets\whitehilt_decor'
$audioAssets = Join-Path $project 'Assets\Audio'
$audioTarget = Join-Path $repoRoot 'BrudvikWhiteHilt\Assets\whitehilt_audio'
$creatureAssets = Join-Path $project 'Assets\Creatures'
$editorScripts = Join-Path $project 'Assets\Editor'

function Invoke-Unity([string[]]$Arguments) {
    # Unity.exe is a GUI application, so the call operator would not wait for it.
    $process = Start-Process -FilePath $UnityPath -ArgumentList $Arguments -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode). See $logFile"
    }
}

function Convert-Models {
    if (Test-Path $foragingAssets) {
        Remove-Item (Join-Path $foragingAssets '*') -Recurse -Force
    }
    New-Item -ItemType Directory -Force $foragingAssets | Out-Null

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

    # Tiling textures made by Tools/make_roof_textures.py (<name>_albedo / <name>_normal).
    $textures = Join-Path $PSScriptRoot 'Textures'
    if (Test-Path $textures) {
        foreach ($texture in Get-ChildItem $textures -Include *.jpg, *.png -File -Recurse) {
            Copy-Item $texture.FullName (Join-Path $foragingAssets $texture.Name.ToLowerInvariant()) -Force
        }
    }
}

# Every sound, in its own bundle; the file name, in lower case, becomes the AudioClip name.
function Copy-Audio {
    if (Test-Path $audioAssets) {
        Remove-Item (Join-Path $audioAssets '*') -Recurse -Force
    }
    New-Item -ItemType Directory -Force $audioAssets | Out-Null
    $folders = @((Join-Path $PSScriptRoot 'Sounds'), (Join-Path $PSScriptRoot 'Audio'))
    foreach ($folder in $folders | Where-Object { Test-Path $_ }) {
        foreach ($sound in Get-ChildItem $folder -Include *.wav, *.mp3 -File -Recurse) {
            Copy-Item $sound.FullName (Join-Path $audioAssets $sound.Name.ToLowerInvariant()) -Force
        }
    }
}

function Build-Audio {
    Write-Host "Building audio bundle (log: $logFile)"
    Invoke-Unity @('-batchmode', '-quit', '-nographics', '-projectPath', "`"$project`"", '-executeMethod', 'BuildAudioBundle.Build', '-logFile', "`"$logFile`"")
    Copy-Item (Join-Path $project 'AssetBundlesAudio\whitehilt_audio') $audioTarget -Force
    Select-String -Path $logFile -Pattern '\[WhiteHilt\] Built' | ForEach-Object { Write-Host $_.Line }
    Write-Host "Copied audio bundle to $audioTarget ($((Get-Item $audioTarget).Length) bytes)"
}

# The decor models are named decor_<id>, so they cannot clash with the main bundle's.
function Convert-Decor {
    if (Test-Path $decorAssets) {
        Remove-Item (Join-Path $decorAssets '*') -Recurse -Force
    }
    New-Item -ItemType Directory -Force $decorAssets | Out-Null
    foreach ($model in Get-ChildItem (Join-Path $PSScriptRoot 'Decor\Models') -Filter *.glb) {
        python (Join-Path $PSScriptRoot 'convert_glb.py') $model.FullName $decorAssets "decor_$($model.BaseName.ToLowerInvariant())"
        if ($LASTEXITCODE -ne 0) {
            throw "Model conversion failed for $($model.Name)"
        }
    }
}

function Build-Decor {
    Write-Host "Building decor bundle (log: $logFile)"
    Invoke-Unity @('-batchmode', '-quit', '-nographics', '-projectPath', "`"$project`"", '-executeMethod', 'BuildDecorBundle.Build', '-logFile', "`"$logFile`"")
    Copy-Item (Join-Path $project 'AssetBundlesDecor\whitehilt_decor') $decorTarget -Force
    Select-String -Path $logFile -Pattern '\[WhiteHilt\]' | ForEach-Object { Write-Host $_.Line }
    Write-Host "Copied decor bundle to $decorTarget ($((Get-Item $decorTarget).Length) bytes)"
}

# Blender turns each rigged glTF into an FBX with its textures; Unity builds the prefab (BuildCreatures.cs).
function Export-Creatures {
    if (Test-Path $creatureAssets) {
        Remove-Item (Join-Path $creatureAssets '*') -Recurse -Force
    }

    $creatures = @(Get-ChildItem (Join-Path $PSScriptRoot 'Creatures') -Filter *.glb -ErrorAction SilentlyContinue)
    if ($creatures.Count -eq 0) {
        return
    }

    $blender = $BlenderPath
    if (-not $blender) {
        $blender = Get-ChildItem 'C:\Program Files\Blender Foundation' -Filter blender.exe -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName | Select-Object -Last 1 -ExpandProperty FullName
    }
    if (-not $blender -or -not (Test-Path $blender)) {
        throw "Blender not found; pass -BlenderPath"
    }

    New-Item -ItemType Directory -Force $creatureAssets | Out-Null
    foreach ($creature in $creatures) {
        $name = $creature.BaseName.ToLowerInvariant()
        $spec = Join-Path $creature.DirectoryName "$($creature.BaseName).creature.json"
        & $blender -b --factory-startup --python-exit-code 1 --python (Join-Path $PSScriptRoot 'Tools\export_creature.py') -- $creature.FullName $spec $creatureAssets $name |
            Select-String '\[WhiteHilt\]|Error' | ForEach-Object { Write-Host $_.Line }
        if ($LASTEXITCODE -ne 0) {
            throw "Creature export failed for $($creature.Name)"
        }
        Copy-Item $spec (Join-Path $creatureAssets "$name.creature.json") -Force
    }
}

if (-not (Test-Path $UnityPath)) {
    throw "Unity not found at $UnityPath"
}

if (-not (Test-Path (Join-Path $project 'Assets'))) {
    Write-Host "Creating Unity project in $project"
    Invoke-Unity @('-batchmode', '-quit', '-createProject', "`"$project`"", '-logFile', "`"$logFile`"")
}

New-Item -ItemType Directory -Force $editorScripts | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildForagingBundle.cs') $editorScripts -Force
Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildCreatures.cs') $editorScripts -Force
Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildDecorBundle.cs') $editorScripts -Force
Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildAttackClips.cs') $editorScripts -Force
Copy-Item (Join-Path $PSScriptRoot 'Unity\BuildAudioBundle.cs') $editorScripts -Force

if ($AudioOnly) {
    Copy-Audio
    Build-Audio
    return
}

if ($DecorOnly) {
    if (-not $UnityOnly) {
        Convert-Decor
    }
    Build-Decor
    return
}

if (-not $UnityOnly) {
    Convert-Models
    Copy-Audio
    Convert-Decor
    Export-Creatures
    & (Join-Path $PSScriptRoot 'build_skidbladnir.ps1') -BlenderPath $BlenderPath
}

# The Rune Sword's attack clips are checked against the game's own player animator, so they need the game installed;
# without it the clips already in the project are used. Not -nographics: it renders preview strips too.
$game = if ($env:VALHEIM_INSTALL) { $env:VALHEIM_INSTALL } else { 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' }
if (Test-Path (Join-Path $game 'valheim_Data')) {
    Write-Host "Building attack clips (log: $logFile)"
    Invoke-Unity @('-batchmode', '-quit', '-projectPath', "`"$project`"", '-executeMethod', 'BuildAttackClips.Build', '-logFile', "`"$logFile`"")
    Select-String -Path $logFile -Pattern '\[WhiteHilt\]' | ForEach-Object { Write-Host $_.Line }
}
else {
    Write-Warning "Valheim not found at $game (set VALHEIM_INSTALL); keeping the attack clips already in the project."
}

Write-Host "Building asset bundle (log: $logFile)"
Invoke-Unity @('-batchmode', '-quit', '-nographics', '-projectPath', "`"$project`"", '-executeMethod', 'BuildForagingBundle.Build', '-logFile', "`"$logFile`"")

Copy-Item (Join-Path $project 'AssetBundles\whitehilt_foraging') $bundleTarget -Force
Select-String -Path $logFile -Pattern '\[WhiteHilt\]' | ForEach-Object { Write-Host $_.Line }
Write-Host "Copied bundle to $bundleTarget ($((Get-Item $bundleTarget).Length) bytes)"

Build-Decor

Build-Audio
