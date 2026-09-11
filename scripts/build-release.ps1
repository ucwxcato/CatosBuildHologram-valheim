[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$GameManagedDirectory = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed',
    [string]$BepInExCoreDirectory = 'C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosBuildHologram\BepInEx\core'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$setupScript = Join-Path $repoRoot 'scripts\setup-references.ps1'
$projects = @(
    (Join-Path $repoRoot 'src\CatosBuildHologram.Shared\CatosBuildHologram.Shared.csproj'),
    (Join-Path $repoRoot 'src\CatosBuildHologram.Client\CatosBuildHologram.Client.csproj'),
    (Join-Path $repoRoot 'src\CatosBuildHologram.Server\CatosBuildHologram.Server.csproj')
)
$outputs = @(
    (Join-Path $repoRoot "src\CatosBuildHologram.Shared\bin\$Configuration\net48\net48\CatosBuildContracts.dll"),
    (Join-Path $repoRoot "src\CatosBuildHologram.Client\bin\$Configuration\net48\net48\CatosBuildHologram.Client.dll"),
    (Join-Path $repoRoot "src\CatosBuildHologram.Server\bin\$Configuration\net48\net48\CatosBuildHologram.Server.dll")
)

& $setupScript -GameManagedDirectory $GameManagedDirectory -BepInExCoreDirectory $BepInExCoreDirectory
if (-not $?) {
    throw 'Reference setup failed.'
}

foreach ($project in $projects) {
    Write-Host "Building $project"
    & dotnet build $project -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed for $project with exit code $LASTEXITCODE"
    }
}

foreach ($output in $outputs) {
    if (-not (Test-Path -LiteralPath $output -PathType Leaf)) {
        throw "Expected build output was not produced: $output"
    }
    Write-Host "Verified $output"
}

Write-Host "Release artifacts built successfully."
