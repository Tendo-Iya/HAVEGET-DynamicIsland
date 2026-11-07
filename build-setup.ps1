$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
$wpf = Join-Path $fw 'WPF'
$outDir = Join-Path $root 'dist'
$appExe = Join-Path $outDir 'HAVEGET-DynamicIsland.exe'
if (!(Test-Path $appExe)) { throw 'Build HAVEGET-DynamicIsland.exe first.' }

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
    (Join-Path $fw 'System.Runtime.Serialization.dll')
)
$refArgs = $refs | ForEach-Object { '/r:' + $_ }
$sources = @(
    (Join-Path $root 'installer\InstallerEngine.cs'),
    (Join-Path $root 'installer\SetupWindow.cs'),
    (Join-Path $root 'installer\SetupProgram.cs'),
    (Join-Path $root 'installer\SetupAssemblyInfo.cs'),
    (Join-Path $root 'src\Theme.cs'),
    (Join-Path $root 'src\Spring.cs')
)

$setupPath = Join-Path $outDir 'HAVEGET-DynamicIsland-Setup.exe'
$argsList = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/debug-',
    '/langversion:5',
    '/utf8output',
    ('/out:' + $setupPath),
    ('/win32icon:' + (Join-Path $root 'assets\setup.ico')),
    ('/win32manifest:' + (Join-Path $root 'installer\setup.manifest')),
    ('/resource:' + $appExe + ',HAVEGETDynamicIsland.exe')
) + $refArgs + $sources

$OutputEncoding = [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
& $csc @argsList
if ($LASTEXITCODE -ne 0) { throw "setup compiler failed with exit code $LASTEXITCODE" }
Write-Host ("Built: " + $setupPath)
Get-Item $setupPath | Select-Object Name, Length, LastWriteTime | Format-List
