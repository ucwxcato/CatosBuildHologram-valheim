[CmdletBinding()]
param(
    [string]$AssemblyPath,
    [string]$BepInExCoreDirectory = 'C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosBuildHologram\BepInEx\core',
    [string[]]$TypeName,
    [string[]]$MethodName,
    [switch]$IncludeMethodBody,
    [switch]$MethodsOnly
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $AssemblyPath = Join-Path $PSScriptRoot '..\lib\assembly_valheim.dll'
}

$cecilPath = Join-Path $BepInExCoreDirectory 'Mono.Cecil.dll'
if (-not (Test-Path -LiteralPath $cecilPath -PathType Leaf)) {
    throw "Mono.Cecil.dll was not found at $cecilPath"
}
if (-not (Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) {
    throw "Valheim assembly was not found at $AssemblyPath"
}

[System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $cecilPath).Path) | Out-Null
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path -LiteralPath $AssemblyPath).Path)
$types = @($assembly.MainModule.GetTypes() | Sort-Object FullName)
$candidateNames = @(
    'Player', 'Piece', 'PieceTable', 'WearNTear', 'ZNet', 'ZNetView', 'ZDO',
    'Inventory', 'Hud', 'Game', 'SaveSystem', 'ObjectDB', 'Recipe', 'Requirement',
    'CraftingStation', 'PlacementStatus', 'RequirementMode'
)
$requestedTypeNames = @($TypeName | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$requestedMethodNames = @($MethodName | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$candidates = @($types | Where-Object {
    $_.Name -in $candidateNames -and
    (($requestedTypeNames.Count -eq 0) -or ($_.Name -in $requestedTypeNames))
})
$memberPattern = 'build|piece|place|snap|support|stabil|wear|health|inventory|item|recipe|require|cost|consume|resource|save|world|rpc|zdo|owner|player|hover|craft|station|ghost|preview|construct|destroy|remove|queue|network'

Write-Host '=== Assembly ==='
Write-Host $assembly.Name.FullName
Write-Host "Types: $($types.Count)"
Write-Host ''
Write-Host '=== Candidate native types ==='
foreach ($type in $candidates) {
    Write-Host $type.FullName
}

Write-Host ''
Write-Host '=== Relevant members ==='
foreach ($type in $candidates) {
    Write-Host "--- $($type.FullName) ---"

    if (-not $MethodsOnly) {
        foreach ($field in ($type.Fields | Sort-Object Name)) {
            if ($type.IsEnum -or $field.Name -match $memberPattern -or $field.Name -match 'amount|resItem|resources|name|prefab|hash|position|rotation') {
                Write-Host "FIELD  $($field.FieldType.FullName) $($field.Name)"
            }
        }
        foreach ($property in ($type.Properties | Sort-Object Name)) {
            if ($property.Name -match $memberPattern) {
                Write-Host "PROP   $($property.PropertyType.FullName) $($property.Name)"
            }
        }
    }
    foreach ($method in ($type.Methods | Sort-Object Name, FullName)) {
        if ($method.Name -match $memberPattern -and
            (($requestedMethodNames.Count -eq 0) -or ($method.Name -in $requestedMethodNames))) {
            $parameters = ($method.Parameters | ForEach-Object { "$($_.ParameterType.FullName) $($_.Name)" }) -join ', '
            Write-Host "METHOD $($method.ReturnType.FullName) $($method.Name)($parameters)"

            if ($IncludeMethodBody -and $requestedMethodNames.Count -gt 0 -and $method.Name -in $requestedMethodNames -and $method.HasBody) {
                foreach ($instruction in $method.Body.Instructions) {
                    $operand = if ($null -eq $instruction.Operand) { '' } else { " $($instruction.Operand)" }
                    Write-Host ("  IL {0}: {1}{2}" -f $instruction.Offset, $instruction.OpCode.Code, $operand)
                }
            }
        }
    }
}
