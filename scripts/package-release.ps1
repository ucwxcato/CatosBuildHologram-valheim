[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\CatosBuildHologram-$Configuration"
}

if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Refusing to overwrite an existing package directory: $OutputDirectory"
}

$clientOutput = Join-Path $repoRoot "src\CatosBuildHologram.Client\bin\$Configuration\net48\net48"
$serverOutput = Join-Path $repoRoot "src\CatosBuildHologram.Server\bin\$Configuration\net48\net48"
$sharedOutput = Join-Path $repoRoot "src\CatosBuildHologram.Shared\bin\$Configuration\net48\net48"
$required = @(
    (Join-Path $clientOutput 'CatosBuildHologram.Client.dll'),
    (Join-Path $serverOutput 'CatosBuildHologram.Server.dll'),
    (Join-Path $sharedOutput 'CatosBuildContracts.dll')
)
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Missing release input. Run scripts/build-release.ps1 first: $file"
    }
}

$clientPackage = Join-Path $OutputDirectory 'client'
$serverPackage = Join-Path $OutputDirectory 'server'
New-Item -ItemType Directory -Path $clientPackage, $serverPackage -Force | Out-Null
Copy-Item -LiteralPath $required[0] -Destination $clientPackage
Copy-Item -LiteralPath $required[2] -Destination $clientPackage
Copy-Item -LiteralPath $required[1] -Destination $serverPackage
Copy-Item -LiteralPath $required[2] -Destination $serverPackage

Write-Host "Created client package: $clientPackage"
Write-Host "Created server package: $serverPackage"
Write-Host 'Package excludes lib, bin, obj, logs, worlds, credentials, and BepInEx runtime state.'
