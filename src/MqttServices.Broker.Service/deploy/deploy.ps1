<#
.SYNOPSIS
    Deploys a new build of the MQTT broker service. Run elevated.

.DESCRIPTION
    Stops the service, replaces the binaries, starts it again and shows the tail of the log. The
    service has to be stopped first, otherwise its own DLLs are locked and the copy fails halfway.

.EXAMPLE
    dotnet publish -c Release -o C:\Temp\MqttBrokerPublish
    .\deploy.ps1 -Source 'C:\Temp\MqttBrokerPublish' -Target 'C:\Services\MqttBroker'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Source,

    [Parameter(Mandatory = $true)]
    [string] $Target,

    [string] $ServiceName = 'MqttBroker',
    [string] $DataDirectory = 'C:\ProgramData\MqttBroker',
    [int]    $StopTimeoutSeconds = 30
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Source)) {
    throw "Source not found: $Source"
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service -and $service.Status -ne 'Stopped') {
    Write-Host "Stopping $ServiceName ..."
    Stop-Service -Name $ServiceName
    $service.WaitForStatus('Stopped', [TimeSpan]::FromSeconds($StopTimeoutSeconds))
}

New-Item -ItemType Directory -Path $Target -Force | Out-Null

# The data directory is deliberately somewhere else, so nothing here can overwrite the
# certificate or the production configuration.
Copy-Item -Path (Join-Path $Source '*') -Destination $Target -Recurse -Force
Write-Host "Copied the build to $Target"

Start-Service -Name $ServiceName
Write-Host "Started $ServiceName"

Start-Sleep -Seconds 5
$log = Get-ChildItem -Path (Join-Path $DataDirectory 'logs') -Filter 'broker-*.log' -ErrorAction SilentlyContinue |
       Sort-Object LastWriteTime -Descending |
       Select-Object -First 1

if ($log) {
    Write-Host ''
    Write-Host "--- $($log.Name) ---"
    Get-Content -LiteralPath $log.FullName -Tail 20
}
