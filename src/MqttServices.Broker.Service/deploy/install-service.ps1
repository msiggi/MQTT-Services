<#
.SYNOPSIS
    Registers the MQTT broker as a Windows service. Run once, elevated.

.DESCRIPTION
    Creates the service, the data directory and the firewall rule, and sets a recovery policy
    that restarts the service after a failure.

.EXAMPLE
    .\install-service.ps1 -BinaryPath 'C:\Services\MqttBroker\MqttServices.Broker.Service.exe'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $BinaryPath,

    [string] $ServiceName = 'MqttBroker',
    [string] $DisplayName = 'MQTT Broker',
    [string] $Description = 'MQTT broker (MQTTnet) for ThingsBridge, Argus and related services.',
    [string] $DataDirectory = 'C:\ProgramData\MqttBroker',
    [int]    $TlsPort = 8883
)

$ErrorActionPreference = 'Stop'

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
        ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this elevated: it creates a service, a firewall rule and a directory ACL.'
}

if (-not (Test-Path -LiteralPath $BinaryPath)) {
    throw "Binary not found: $BinaryPath"
}

# The data directory holds the certificate's private key and the real credentials, so take the
# inherited permissions away and leave SYSTEM and the administrators.
if (-not (Test-Path -LiteralPath $DataDirectory)) {
    New-Item -ItemType Directory -Path $DataDirectory | Out-Null
    Write-Host "Created $DataDirectory"
}

$acl = Get-Acl -LiteralPath $DataDirectory
$acl.SetAccessRuleProtection($true, $false)
$acl.Access | ForEach-Object { $acl.RemoveAccessRule($_) | Out-Null }
foreach ($identity in @('NT AUTHORITY\SYSTEM', 'BUILTIN\Administrators')) {
    $acl.AddAccessRule((New-Object System.Security.AccessControl.FileSystemAccessRule(
        $identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
}
Set-Acl -LiteralPath $DataDirectory -AclObject $acl
Write-Host "Restricted $DataDirectory to SYSTEM and Administrators"

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Service $ServiceName already exists; leaving it alone."
}
else {
    New-Service -Name $ServiceName `
                -BinaryPathName "`"$BinaryPath`"" `
                -DisplayName $DisplayName `
                -Description $Description `
                -StartupType Automatic | Out-Null
    Write-Host "Created service $ServiceName"
}

# Restart after a failure. failureflag=1 is the part that is easy to miss: without it Windows
# only reacts to a crash, and the watchdog stops the service with a clean exit code.
& sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/15000/restart/60000 | Out-Null
& sc.exe failureflag $ServiceName 1 | Out-Null
Write-Host 'Recovery policy set (restart after 5s, 15s, 60s; also on a clean non-zero exit)'

$ruleName = "MQTT Broker TLS ($TlsPort)"
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName `
                        -Direction Inbound -Action Allow -Protocol TCP -LocalPort $TlsPort | Out-Null
    Write-Host "Created firewall rule '$ruleName'"
}

Write-Host ''
Write-Host "Next: put the real configuration in $DataDirectory\appsettings.Production.json,"
Write-Host "then start it with: Start-Service $ServiceName"
