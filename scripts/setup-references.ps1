[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameManagedDirectory,

    [Parameter(Mandatory = $true)]
    [string]$BepInExCoreDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$destination = Join-Path $repoRoot 'lib'
$resolvedGame = (Resolve-Path -LiteralPath $GameManagedDirectory).Path
$resolvedBepInEx = (Resolve-Path -LiteralPath $BepInExCoreDirectory).Path

New-Item -ItemType Directory -Path $destination -Force | Out-Null

$gameReferences = @(
    'UnityEngine.dll',
    'UnityEngine.CoreModule.dll',
    'UnityEngine.UI.dll',
    'UnityEngine.PhysicsModule.dll',
    'UnityEngine.InputLegacyModule.dll',
    'UnityEngine.IMGUIModule.dll',
    'Unity.TextMeshPro.dll',
    'assembly_valheim.dll',
    'assembly_utils.dll'
)
$bepInExReferences = @('BepInEx.dll', '0Harmony20.dll')

foreach ($name in $gameReferences) {
    $source = Join-Path $resolvedGame $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing required game reference: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name) -Force
}

foreach ($name in $bepInExReferences) {
    $source = Join-Path $resolvedBepInEx $name
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing required BepInEx reference: $source"
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $destination $name) -Force
}

Write-Host "Copied $($gameReferences.Count + $bepInExReferences.Count) references into $destination"
