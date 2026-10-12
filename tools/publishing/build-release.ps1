# Rebuilds the solution in Release; publish.ps1 then writes BrudvikWhiteHilt\bin\Release\BrudvikWhiteHilt.dll.zip.
# MSBuild is not on the PATH, so it is found with vswhere.
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found' }

& $msbuild (Join-Path $root 'BrudvikWhiteHilt.sln') /t:Rebuild /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE" }
& python (Join-Path $PSScriptRoot 'check-package.py')
