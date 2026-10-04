param([string]$BlenderPath = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe')

$ErrorActionPreference = 'Stop'
if (-not $BlenderPath) {
    $BlenderPath = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
}
$repo = Split-Path $PSScriptRoot -Parent
$output = Join-Path $repo 'BrudvikWhiteHiltUnity\Assets\Foraging'
$log = Join-Path $env:TEMP 'whitehilt_skidbladnir_export.log'
$arguments = @('--background', '--python-exit-code', '1', '--python',
    (Join-Path $PSScriptRoot 'Preview\render_ship_layout.py'), '--',
    (Join-Path $PSScriptRoot 'Ships\skidbladnir.glb'), '--export', $output)
$process = Start-Process -FilePath $BlenderPath -ArgumentList $arguments -Wait -PassThru -RedirectStandardOutput $log -RedirectStandardError "$log.errors"
if ($process.ExitCode -ne 0) {
    throw "Skidbladnir export failed. See $log and $log.errors"
}
Copy-Item (Join-Path $output 'sailing_ship.assets.json') (Join-Path $PSScriptRoot 'Ships\sailing_ship.assets.json') -Force
Get-Content $log | Select-String 'SHIP EXPORT COMPLETE'