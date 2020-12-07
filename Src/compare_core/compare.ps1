#!/usr/bin/env pwsh

param (
    [string]$SourceVersion = 'fw4.8',  # currently fixed to 4.8
    [string]$TargetVersion = '5',
    [string]$SourceDir = "..\..\..\..\results\scan\$SourceVersion",
    [string]$TargetDir = "..\..\..\..\results\compare\$TargetVersion",
    [bool]$ClearTargetDir = $True 
)


# compare the assembly API
function CompareAPI([System.IO.FileSystemInfo]$sourceFile) {
    Write-Host "comparing $($sourceFile.Name)"
    $targetFileName = [System.IO.Path]::ChangeExtension($sourceFile.Name, '.csv')
    dotnet compare.dll -i ($sourceFile.FullName) -o "$TargetDir\$targetFileName"
}

# prepare the target dir
if (Test-Path $TargetDir) {
    if ($ClearTargetDir) {
        Remove-Item "$TargetDir\*"
    }
} else {
    New-Item $TargetDir -ItemType Directory
}

# compare the assembly APIs
Get-ChildItem "$SourceDir\*.json" -File `
 | ForEach-Object { CompareAPI($_) }
