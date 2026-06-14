<#
  Build the release artifacts for Clarion Debugger:
    1. self-contained single-file ClarionDbg.exe (bundles .NET + WPF)  -> installer\portable\
    2. portable zip                                                    -> installer\output\
    3. Inno Setup installer                                            -> installer\output\
  Run from anywhere; paths are resolved relative to this script.
#>
param([string]$Version = "1.0.0")

$ErrorActionPreference = "Stop"
$here   = Split-Path -Parent $MyInvocation.MyCommand.Path
$root   = Split-Path -Parent $here
$proj   = Join-Path $root "src\ClarionDbg.App\ClarionDbg.App.csproj"
$portable = Join-Path $here "portable"
$output   = Join-Path $here "output"
$iscc   = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Get-Process ClarionDbg -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -ItemType Directory -Force -Path $portable, $output | Out-Null

Write-Host "==> publishing self-contained single-file (win-x86)..."
dotnet publish $proj -c Release -r win-x86 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o $portable -v quiet --nologo
Remove-Item (Join-Path $portable "*.pdb") -ErrorAction SilentlyContinue

Write-Host "==> zipping portable build..."
$zip = Join-Path $output "ClarionDebugger-$Version-portable-win-x86.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $portable "ClarionDbg.exe") -DestinationPath $zip

Write-Host "==> building installer with Inno Setup..."
& $iscc "/DAppVersion=$Version" (Join-Path $here "ClarionDbg.iss") | Out-Null

Write-Host "`n==> artifacts:"
Get-ChildItem $output | Select-Object Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}
