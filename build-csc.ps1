# Fallback build: no .NET SDK required, uses the legacy C# compiler shipped with Windows (C# 5).
# Only for emergency preview builds. Use build.ps1 + .NET SDK for the real build.
param(
    [string]$GameRoot = 'C:\Game\EscapeFromTarkov\Client.0.16.9.0.40087',
    [string]$OutDir = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrEmpty($OutDir)) {
    $OutDir = Join-Path $PSScriptRoot 'dist'
}

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "Legacy C# compiler not found: $csc"
}

$man = Join-Path $GameRoot 'EscapeFromTarkov_Data\Managed'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$references = @(
    (Join-Path $man 'netstandard.dll')
    (Join-Path $man 'Sirenix.Serialization.dll')
    (Join-Path $man 'DissonanceVoip.dll')
    (Join-Path $man 'ItemComponent.Types.dll')
    (Join-Path $man 'ItemTemplate.Types.dll')
    (Join-Path $man 'Assembly-CSharp.dll')
    (Join-Path $man 'Comfort.dll')
    (Join-Path $man 'UnityEngine.dll')
    (Join-Path $man 'UnityEngine.CoreModule.dll')
    (Join-Path $man 'UnityEngine.UI.dll')
    (Join-Path $man 'UnityEngine.UIModule.dll')
    (Join-Path $man 'UnityEngine.TextRenderingModule.dll')
    (Join-Path $man 'Unity.TextMeshPro.dll')
    (Join-Path $GameRoot 'BepInEx\core\BepInEx.dll')
    (Join-Path $GameRoot 'BepInEx\core\0Harmony.dll')
    (Join-Path $GameRoot 'BepInEx\plugins\spt\spt-reflection.dll')
)

$sources = Get-ChildItem $PSScriptRoot -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
$output = Join-Path $OutDir 'DamageReport.dll'

$arguments = @('/nologo', '/target:library', '/langversion:5', "/out:$output")
$referenceArguments = $references | ForEach-Object { "/reference:$_" }
$arguments += $referenceArguments
$arguments += $sources

& $csc $arguments
if ($LASTEXITCODE -ne 0) {
    throw "csc build failed, exit code $LASTEXITCODE"
}

Write-Host "Output: $output"
