# Simple PowerShell script to easily build on Windows and verify functionality.
#
# Usage:
#   .\build.ps1                           # Build with defaults (win-x64, Release, output to bin/)
#   .\build.ps1 -Output dist              # Custom output directory
#   .\build.ps1 -Configuration Debug      # Debug build
#   .\build.ps1 -Runtime win-arm64        # Different runtime
#   .\build.ps1 -NoPortable               # Skip creating userdata/ folder (non-portable)
#   .\build.ps1 -SelfContained            # Produce a self-contained build
#   .\build.ps1 -NoSingleFile             # Disable single-file publishing

param(
    [Alias("o")]
    [string]$Output = "bin",

    [Alias("c")]
    [string]$Configuration = "Release",

    [Alias("r")]
    [string]$Runtime = "win-x64",

    [switch]$NoPortable,
    [switch]$SelfContained,
    [switch]$NoSingleFile,

    [Parameter(ValueFromRemainingArguments)]
    [string[]]$ExtraArgs
)

$ErrorActionPreference = "Stop"

$RepoRoot = $PSScriptRoot
$Framework = "net8.0"

$Projects = @(
    "OpenTabletDriver.Daemon",
    "OpenTabletDriver.Console"
)

$UIProjects = @(
    "OpenTabletDriver.UX.Wpf"
)

### Version suffix detection (ported from eng/bash/lib.sh)

$VersionSuffix = $env:VERSION_SUFFIX
if (-not $VersionSuffix) {
    try {
        $gitDescribe = git describe --long --tags --dirty 2>$null
        if ($LASTEXITCODE -eq 0 -and $gitDescribe) {
            $tagRegex = '^v(([0-9]+(\.[0-9]+)*)([^\-\r\n]*(-?(rc[0-9]+))?))-?((([0-9]+)-g([a-f0-9]{8}))(.*)?)$'
            if ($gitDescribe -match $tagRegex) {
                $commitDistance = $Matches[9]
                $describeSuffix = $Matches[7]
                $subVersion = $Matches[4]
                $remainder = $Matches[11]
                $dontSetDirty = $false

                if ([int]$commitDistance -gt 0) {
                    $VersionSuffix = "+$describeSuffix"
                    $dontSetDirty = $true
                }

                if ($subVersion) {
                    $VersionSuffix = "-$subVersion$VersionSuffix"
                }

                if (-not $dontSetDirty -and $remainder -match "dirty") {
                    $VersionSuffix = "$VersionSuffix-dirty"
                }

                if ($VersionSuffix) {
                    Write-Output "Autodetected version suffix: '$VersionSuffix'"
                }
            }
        }
    } catch {
        Write-Output "WARN: Could not detect version suffix from git"
    }
}

### Sanity check

if (-not (Test-Path "$RepoRoot/OpenTabletDriver")) {
    Write-Error "Could not find OpenTabletDriver folder!"
    exit 1
}

### Clean old outputs (preserve userdata/)

if (Test-Path $Output) {
    Write-Output "Cleaning old build outputs..."
    try {
        Get-ChildItem -Path $Output | ForEach-Object {
            if ($_.Name -ne "userdata") {
                Remove-Item -Path $_.FullName -Recurse -Force
            }
        }
    } catch {
        Write-Error "Could not clean old build dirs. Please manually remove contents of $Output folder."
        exit 1
    }
}

New-Item -ItemType Directory -Force -Path $Output > $null

### Portable mode

if (-not $NoPortable) {
    New-Item -ItemType Directory -Force -Path "$Output/userdata" > $null
}

### Build options

$Options = @(
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--self-contained", $(if ($SelfContained) { "true" } else { "false" }),
    "--output", $Output,
    "-p:PublishTrimmed=false",
    "-p:DebugType=embedded",
    "-p:SuppressNETCoreSdkPreviewMessage=true"
)

if ($VersionSuffix) {
    $Options += "-p:VersionSuffix=$VersionSuffix"
}

if (-not $NoSingleFile) {
    $Options += "-p:PublishSingleFile=true"
}

if ($ExtraArgs) {
    $Options += $ExtraArgs
}

### Restore & Build

Write-Output "Restoring packages..."
dotnet restore --runtime $Runtime --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Restore failed!"
    exit 1
}

foreach ($project in $Projects) {
    Write-Output "`nBuilding $project...`n"
    dotnet publish $project --framework $Framework $Options
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $project!"
        exit 1
    }
}

foreach ($project in $UIProjects) {
    Write-Output "`nBuilding $project...`n"
    dotnet publish $project --framework "$Framework-windows" $Options
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $project!"
        exit 1
    }
}

Write-Output "`nBuild finished! Binaries created in $Output"
