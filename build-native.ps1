#!/usr/bin/env pwsh
# build-native.ps1
# This script builds the native `ghostty.dll` dependency for Windows using Zig.
# It should be run manually whenever the ghostty_src submodule is updated.

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$GhosttySrc = Join-Path $ProjectRoot "ghostty_src"
$NativeDir = Join-Path $ProjectRoot "Dotty\Native"

Write-Host "Initializing submodules..."
Set-Location $ProjectRoot
git submodule update --init

Write-Host "Building ghostty.dll for x86_64-windows..."
Set-Location $GhosttySrc
# We limit to 4 jobs to avoid OOM or Insufficient Handle Quota errors on large C/C++ dependencies
zig build -j4 -Dtarget=x86_64-windows -Dapp-runtime=none

Write-Host "Copying ghostty.dll to Dotty\Native..."
if (!(Test-Path $NativeDir)) {
    New-Item -ItemType Directory -Force -Path $NativeDir | Out-Null
}

$DllPath = Join-Path $GhosttySrc "zig-out\lib\ghostty.dll"
if (Test-Path $DllPath) {
    Copy-Item -Path $DllPath -Destination $NativeDir -Force
    Write-Host "Success! The windows native library has been built and copied." -ForegroundColor Green
} else {
    Write-Host "Error: Failed to find ghostty.dll in zig-out\lib" -ForegroundColor Red
    exit 1
}
