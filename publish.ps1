param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$ProjectPath,
    
    [System.String]$DeployPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file, for debuggers that need Mono's format; only when pdb2mdb.exe has been put in libraries\Debug.
$pdb = "$TargetPath\$name.pdb"
$pdb2mdb = "$(Get-Location)\libraries\Debug\pdb2mdb.exe"
if ((Test-Path -Path "$pdb") -and (Test-Path -Path "$pdb2mdb")) {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$pdb2mdb`" `"$TargetPath\$TargetAssembly`""
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) {
    if ($DeployPath.Equals("")){
      $DeployPath = "$ValheimPath\BepInEx\plugins"
    }
    
    $plug = New-Item -Type Directory -Path "$DeployPath\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$plug" -Force
    if (Test-Path -Path "$TargetPath\$name.dll.mdb") {
        Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
    }
}

if($Target.Equals("Release")) {
    Write-Host "Packaging for ThunderStore..."
    $Package="Package"
    $PackagePath="$ProjectPath\$Package"

    Write-Host "$PackagePath\$TargetAssembly"
    New-Item -Type Directory -Path "$PackagePath\plugins" -Force
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$PackagePath\plugins\$TargetAssembly" -Force
    # Thunderstore shows the GitHub README's HTML poorly, so the package gets a plain markdown version of it.
    & "$ProjectPath\..\build_thunderstore_readme.ps1" -Source "$ProjectPath\..\README.MD" -Target "$PackagePath\README.md"
    Copy-Item -Path "$ProjectPath\..\CHANGELOG.md" -Destination "$PackagePath\CHANGELOG.md" -Force -ErrorAction Stop

    # Compress-Archive in PS 5.1 writes backslash entry names, which Thunderstore rejects
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $zipPath = "$TargetPath\$TargetAssembly.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $root = (Resolve-Path $PackagePath).Path.TrimEnd('\') + '\'
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
    try {
        Get-ChildItem -Path $PackagePath -Recurse -File | % {
            $entry = $_.FullName.Substring($root.Length).Replace('\', '/')
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entry) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }
}

# Pop Location
Pop-Location