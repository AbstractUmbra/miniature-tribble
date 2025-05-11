<#
.SYNOPSIS
    A small script to edit the Dalamud config file with the new beta testing keys.

.DESCRIPTION
    This script intends to make it easier to update your dalamud config settings with new release types and beta keys.
    Running this script with no arguments removes the beta key and changes the release type back to 'release', which is the default behaviour.

.PARAMETER BetaKey
    The value of the new beta key.

.PARAMETER BetaKind
    The value of the new beta kind.

.PARAMETER WhatIf
    This switch will print the altered contents of the config file, but not overwrite them with changes.
    This should be used for testing or ensuring bad content is not created first.

.PARAMETER NoBackup
    This switch will disable the script's backup creation process.
    By default we create a backup of the config with today's date and time, before editing with our changes.

.EXAMPLE
    .\\dalamud.ps1 -BetaKey "FunkyBetaKey" -BetaKind "stg"
    Runs the script to edit the config with the given key and kind.

.EXAMPLE
    .\\dalamud.ps1
    Runs the script to reset the default values for dalamud, release with no beta key

.EXAMPLE
    .\\dalamud.ps1 -BetaKey FunkyBetaKey -BetaKind stg -WhatIf
    Runs the script but will print the new value of the file rather than creating it, if you want to view the potential file changes.
    The output is rather long.

.EXAMPLE
    .\\dalamud.ps1 -NoBackup
    Runs the script, resetting to default values, but will NOT create a backup of today's date in the process.
#>

param(
    [Parameter(Mandatory = $false)]
    [String]
    $BetaKey = "",

    [Parameter(Mandatory = $false)]
    [String]
    $BetaKind = "release",

    [Parameter(Mandatory = $false)]
    [switch]
    $WhatIf,

    [Parameter(Mandatory = $false)]
    [switch]
    $NoBackup
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$configFileName = "dalamudConfig.json"
$configFilePath = "$env:APPDATA/XIVLauncher/${configFileName}"

if (-Not (Test-Path -Path $configFilePath)) {
    Write-Output "No XIVLauncher config folder/file has been found, will create it now!"
    # Create a dummy json file with a default empty object
    New-Item -ItemType File -Value '{}' -Force -Path $configFilePath
}

$configFileContent = Get-Content $configFilePath | ConvertFrom-JSON
$configFileContent.DalamudBetaKey = $BetaKey
$configFileContent.DalamudBetaKind = $BetaKind

if ($WhatIf) {
    $configFileContent | ConvertTo-JSON -Depth 32
}
else {
    if (-Not $NoBackup) {
        $backupFilePath = $configFilePath -replace ".json$", (".backup-{0:dd-MM-yyyy--HH-mm}.json" -f (Get-Date))
        Copy-Item -Path $configFilePath -Destination $backupFilePath -Force
    }
    $configFileContent | ConvertTo-JSON -Depth 32 | Set-Content $configFilePath
}
