param(
    [string]$GameRoot = 'C:\Game\EscapeFromTarkov\Client.0.16.9.0.40087',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet is not on PATH. Install .NET SDK 8.0 first: winget install Microsoft.DotNet.SDK.8, then reopen the terminal.'
}

$project = Join-Path $PSScriptRoot 'DamageReport.csproj'

Write-Host "GameRoot : $GameRoot"
Write-Host "Project  : $project"

dotnet build $project -c $Configuration "-p:GameRoot=$GameRoot"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE. If the game is running, close it first: the plugin DLL is locked and cannot be overwritten."
}

$dll = Join-Path $GameRoot 'BepInEx\plugins\DamageReport\DamageReport.dll'
Write-Host ''
if (Test-Path $dll) {
    Write-Host "Output: $dll"
} else {
    Write-Host "WARNING: output not found at $dll"
}
