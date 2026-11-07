$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
$wpf = Join-Path $fw 'WPF'
$outDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$refs = @(
    (Join-Path $wpf 'PresentationFramework.dll'),
    (Join-Path $wpf 'PresentationCore.dll'),
    (Join-Path $wpf 'WindowsBase.dll'),
    (Join-Path $fw 'System.Xaml.dll'),
    (Join-Path $fw 'System.Windows.Forms.dll'),
    (Join-Path $fw 'System.Drawing.dll'),
    (Join-Path $fw 'System.Xml.dll'),
    (Join-Path $fw 'System.Xml.Linq.dll'),
    (Join-Path $fw 'System.Core.dll'),
    (Join-Path $fw 'System.Management.dll'),
    (Join-Path $fw 'System.Runtime.dll'),
    (Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Foundation.winmd'),
    (Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Graphics.winmd'),
    (Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Storage.winmd'),
    (Join-Path $env:WINDIR 'System32\WinMetadata\Windows.Media.winmd'),
    (Join-Path $fw 'System.Runtime.Serialization.dll')
)
$refArgs = $refs | ForEach-Object { '/r:' + $_ }
$sources = Get-ChildItem (Join-Path $root 'src\*.cs') | Sort-Object Name | ForEach-Object { $_.FullName }

$exePath = Join-Path $outDir 'HAVEGET-DynamicIsland.exe'
$prettyPath = Join-Path $outDir 'HAVEGET DynamicIsland.exe'

$argsList = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/debug-',
    '/langversion:5',
    '/utf8output',
    ('/out:' + $exePath),
    ('/win32icon:' + (Join-Path $root 'assets\app.ico')),
    ('/win32manifest:' + (Join-Path $root 'src\app.manifest'))
) + $refArgs + $sources

$OutputEncoding = [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
& $csc @argsList
if ($LASTEXITCODE -ne 0) { throw "csc failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath $exePath -Destination $prettyPath -Force
Write-Host ("Built: " + $exePath)
Write-Host ("Portable: " + $prettyPath)
Get-Item $exePath, $prettyPath | Select-Object Name, Length, LastWriteTime | Format-List
