$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $root 'build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'App build failed.' }
& (Join-Path $root 'build-setup.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Setup build failed.' }
Get-ChildItem (Join-Path $root 'dist') | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
