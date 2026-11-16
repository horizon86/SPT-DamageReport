param(
    [string]$Version = '1.0.0',
    [string]$GameRoot = 'C:\Game\EscapeFromTarkov\Client.0.16.9.0.40087',
    [string]$Configuration = 'Release',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'DamageReport.csproj'
$pluginsDir = Join-Path $GameRoot 'BepInEx\plugins\DamageReport'
$dll = Join-Path $pluginsDir 'DamageReport.dll'

if (-not $SkipBuild) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'dotnet is not on PATH. Install .NET SDK 8.0 first.'
    }

    & dotnet build $project -c $Configuration "-p:GameRoot=$GameRoot"
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE. Close the game if the plugin DLL is locked."
    }
}

if (-not (Test-Path $dll)) {
    throw "Plugin DLL not found: $dll"
}

$stage = Join-Path $env:TEMP ('DamageReport-package-' + [Guid]::NewGuid().ToString('N'))
$stagePlugins = Join-Path $stage 'BepInEx\plugins\DamageReport'
New-Item -ItemType Directory -Path $stagePlugins -Force | Out-Null
Copy-Item $dll $stagePlugins

$outDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$zip = Join-Path $outDir ("DamageReport-v$Version.zip")
if (Test-Path $zip) {
    Remove-Item $zip -Force
}

$tarPath = Join-Path $env:SystemRoot 'System32\tar.exe'
if (-not (Test-Path $tarPath)) {
    $tarPath = $null
}

if ($tarPath) {
    & $tarPath -a -c -f $zip -C $stage BepInEx
    if ($LASTEXITCODE -ne 0) {
        throw "tar failed with exit code $LASTEXITCODE"
    }
} else {
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
}

Remove-Item $stage -Recurse -Force

Write-Host "Output: $zip"
if ($tarPath) {
    Write-Host 'Contents:'
    & $tarPath -tf $zip
}
