#!/usr/bin/env pwsh

param (
    [string[]]$AssemblyListFiles = @("..\..\AssemblyList.txt"),
    [string]$FrameworkVersion = 'fw4.8',  # currently fixed to 4.8
    [string]$TargetDir = "..\..\..\results\scan\$FrameworkVersion",
    [bool]$ClearTargetDir = $True 
)


# scan the given assembly
function ScanAssembly([string]$assemblyName) {
    $name = ($assemblyName -split ',')[0]
    Write-Host "scanning $assemblyName"
    .\scan.exe -of json -o "$TargetDir\${name}.json" "$assemblyName"
}

# process a line in the assembly list file
function ProcessLine([string]$line) {
    $line = $line.Trim()

    # skip comment lines
    if (-not $line.StartsWith('#')) {
        ScanAssembly($line)
    }
}

# prepare the target dir
if (Test-Path $TargetDir) {
    if ($ClearTargetDir) {
        Remove-Item "$TargetDir\*"
    }
} else {
    New-Item $TargetDir -ItemType Directory
}

# scan the assemblies listed in the assembly list file
Get-Content $AssemblyListFiles `
| ForEach-Object { ProcessLine($_) } 
